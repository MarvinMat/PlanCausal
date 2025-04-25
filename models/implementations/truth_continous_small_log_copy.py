import pandas as pd
import numpy as np
from models.abstract.pgmpy import PGMPYModel
from models.abstract.model import Model
from modules.factory.Operation import Operation
from pgmpy.models import BayesianNetwork
from pgmpy.factors.discrete import TabularCPD

class TruthContinousSmallLogCopyModel(PGMPYModel):
    def __init__(self, seed = None):    
        super().__init__(seed=seed) 
        self.model = None
        self.variable_elemination = None
        self.belief_propagation = None
        self.causal_inference = None
        self.seed = seed
        self.distributions = {}      
        
    def initialize(self):
        self.model = self.create_model()
        self.distributions = self.create_distributions()
        super().initialize()
            
    def read_from_pre_xlsx(self, file):
        data = pd.read_excel(file)
        data.drop(columns=data.columns[0], axis=1, inplace=True)
        return data
    
    def create_distributions(self):
        # Initialize distributions dictionary
        distributions = {}

        # Define mean and variance for each combination of 'machine_state' and 'cleaning'
        combinations = [
            (True, {'mean': 0.7, 'variance': 0.01}),
            (False, {'mean': 1.3, 'variance': 0.01})
        ]

        # Populate the distributions dictionary
        for last_tool_change, stats in combinations:
            distributions[('relative_processing_time_deviation', last_tool_change)] = stats
            
        return distributions
    
    def create_model(self):
        # Defining the Bayesian network structure manually
        model = BayesianNetwork([
            ('last_tool_change', 'relative_processing_time_deviation')
        ])
        
        # Define CPDs
        # CPD for 'last_tool_change' (root node, no parents)
        cpd_last_tool_change = TabularCPD(
            variable='last_tool_change', 
            variable_card=2,  # 2 states: 0 and 1
            values=[[0.6], [0.4]]  # P(last_tool_change=0)=0.6, P(last_tool_change=1)=0.4
        )
        
        # CPD for 'relative_processing_time_deviation' (dependent only on 'last_tool_change')
        cpd_relative_processing_time_deviation = TabularCPD(
            variable='relative_processing_time_deviation', 
            variable_card=3,  # 3 states: 0.9, 1.0, and 1.2
            values=[
                [0.1, 0.3],  # P(relative_processing_time_deviation=0.9 | last_tool_change)
                [0.7, 0.5],  # P(relative_processing_time_deviation=1.0 | last_tool_change)
                [0.2, 0.2]   # P(relative_processing_time_deviation=1.2 | last_tool_change)
            ],
            evidence=['last_tool_change'],  # Parent node
            evidence_card=[2]  # Number of states for the parent
        )
        
        # Add CPDs to the model
        model.add_cpds(
            cpd_last_tool_change,
            cpd_relative_processing_time_deviation
        )
        
        # Validate the model (ensures the structure and CPDs are consistent)
        model.check_model()
        
        return model

    
    def inference(self, operation: Operation, current_tool, do_calculus) -> tuple[int, list[tuple]]:      
        
        last_tool_change =  operation.tool != current_tool
        
        evidence = {
            'last_tool_change': last_tool_change
            # Weitere Evidenzen können hier hinzugefügt werden, falls nötig
        }
                     
        result = self.sample(evidence=evidence)

        
        # Sampling for the relative_processing_time_deviation variable
        if 'relative_processing_time_deviation' in result:
            relative_processing_time_deviation_values = result['relative_processing_time_deviation'].values
            relative_processing_time_deviation_probabilities = relative_processing_time_deviation_values / relative_processing_time_deviation_values.sum()  # Normalize

            # Three possible states: 0.9, 1.0, 1.2
            relative_processing_time_deviation = np.random.choice([0.9, 1.0, 1.2], p=relative_processing_time_deviation_probabilities)
        
        # Create parent_values using machine_state and cleaning variables
        parent_values = last_tool_change
        # Check if the parent combination exists in the learned distributions
        key = ('relative_processing_time_deviation', parent_values)
        if key in self.distributions:
            params = self.distributions[key]
            mean = params['mean']
            variance = params['variance']

            # Convert lognormal mean/variance to mu/sigma for underlying normal
            sigma_squared = np.log(1 + (variance / (mean ** 2)))
            sigma = np.sqrt(sigma_squared)
            mu = np.log(mean) - (sigma_squared / 2)

            # Sample from lognormal distribution
            relative_processing_time_deviation = np.random.lognormal(mean=mu, sigma=sigma)
        else:
            self.logger.error(f"No distribution found for parent values: {parent_values}. Using default mean and variance.")

        

        inferenced_variables = {
            'last_tool_change': last_tool_change,
            'relative_processing_time_deviation': relative_processing_time_deviation
        }
            
        return max(1, round(operation.duration * inferenced_variables['relative_processing_time_deviation'], 0)), inferenced_variables

