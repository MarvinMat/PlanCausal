import pandas as pd
import numpy as np
from models.abstract.pgmpy import PGMPYModel
from models.abstract.model import Model
from modules.factory.Operation import Operation
from pgmpy.models import BayesianNetwork
from pgmpy.factors.discrete import TabularCPD

class TruthModel(PGMPYModel):
    def __init__(self, seed = None, lognormal_shape_modifier = False):    
        super().__init__(seed=seed) 
        self.model = None
        self.variable_elemination = None
        self.belief_propagation = None
        self.causal_inference = None
        self.lognormal_shape_modifier = lognormal_shape_modifier
        self.seed = seed
        
        
    def initialize(self):
        self.model = self.create_model()
        super().initialize()
            
    def read_from_pre_xlsx(self, file):
        data = pd.read_excel(file)
        data.drop(columns=data.columns[0], axis=1, inplace=True)
        return data
    
    def create_model(self):
        # Defining the Bayesian network structure manually
        model = BayesianNetwork([
            ('last_tool_change', 'machine_state'),
            ('machine_state', 'relative_processing_time_deviation'),
            ('machine_state', 'cleaning'),
            ('cleaning', 'relative_processing_time_deviation')
        ])
        
        # Define CPDs
        # CPD for 'last_tool_change' (root node, no parents)
        cpd_last_tool_change = TabularCPD(
            variable='last_tool_change', 
            variable_card=2,  # 2 states: 0 and 1
            values=[[0.6], [0.4]]  # P(last_tool_change=0)=0.8, P(last_tool_change=1)=0.2
        )
        
        # CPD for 'machine_state' (dependent on 'last_tool_change')
        cpd_machine_state = TabularCPD(
            variable='machine_state', 
            variable_card=2,  # 2 states: 0 and 1
            values=[
                [0.9, 0.4],  # P(machine_state=0 | last_tool_change)
                [0.1, 0.6]   # P(machine_state=1 | last_tool_change)
            ],
            evidence=['last_tool_change'],  # Parent node
            evidence_card=[2]  # Number of states of parent
        )
        
        # CPD for 'cleaning' (dependent on 'machine_state')
        cpd_cleaning = TabularCPD(
            variable='cleaning', 
            variable_card=2,  # 2 states: 0 and 1
            values=[
                [0.95, 0.15],  # P(cleaning=0 | machine_state)
                [0.05, 0.85]   # P(cleaning=1 | machine_state)
            ],
            evidence=['machine_state'],  # Parent node
            evidence_card=[2]  # Number of states of parent
        )
        
        # CPD for 'relative_processing_time_deviation' (dependent on 'machine_state' and 'cleaning')
        cpd_relative_processing_time_deviation = TabularCPD(
            variable='relative_processing_time_deviation', 
            variable_card=3,  # 2 states: 0.9, 1.0 and 1.2
            values=[
                [0.08, 0.10, 0.02, 0.04],  # P(relative_processing_time_deviation=0.9 | machine_state, cleaning)
                [0.60, 0.85, 0.31, 0.40],  # P(relative_processing_time_deviation=1.0 | machine_state, cleaning)
                [0.32, 0.05, 0.67, 0.56]   # P(relative_processing_time_deviation=1.2 | machine_state, cleaning)
            ],
            evidence=['machine_state', 'cleaning'],  # Parent nodes
            evidence_card=[2, 2]  # Number of states for each parent
        )
        
        # Add CPDs to the model
        model.add_cpds(
            cpd_last_tool_change, 
            cpd_machine_state, 
            cpd_cleaning, 
            cpd_relative_processing_time_deviation
        )
        
        # Validate the model (ensures the structure and CPDs are consistent)
        model.check_model()
        
        return model
    
    def sample_network(self, evidence: dict) -> dict:
        """
        Samples the Bayesian network given the provided evidence.

        Args:
            evidence (dict): A dictionary where keys are variable names and values are their observed states.

        Returns:
            dict: A dictionary containing the sampled values for all variables in the network.
        """
        from pgmpy.inference import VariableElimination

        # Initialize variable elimination for inference
        if self.variable_elemination is None:
            self.variable_elemination = VariableElimination(self.model)

        # Perform inference to get the probability distributions for all variables
        sampled_result = {}
        for variable in self.model.nodes():
            if variable not in evidence:
                # Query the probability distribution for the variable
                distribution = self.variable_elemination.query(
                    variables=[variable],
                    evidence=evidence
                )
                sampled_result[variable] = distribution.values

        return sampled_result
    
    def sample_without_evidence(self, num_samples: int) -> list[dict]:
        """
        Samples the Bayesian network multiple times without providing any evidence.

        Args:
            num_samples (int): The number of samples to generate.

        Returns:
            list[dict]: A list of dictionaries, where each dictionary contains the sampled values for all variables in the network.
        """
        from pgmpy.sampling import BayesianModelSampling

        # Initialize the sampler
        sampler = BayesianModelSampling(self.model)

        # Generate samples
        samples = sampler.forward_sample(size=num_samples)
        
        samples.to_excel("./output/samples.xlsx", index=False)

        # Convert the DataFrame to a list of dictionaries
        sampled_results = samples.to_dict(orient='records')

        return sampled_results
    
    def get_new_duration(self, operation: Operation, inferenced_variables) -> int:
        base_duration = operation.duration * inferenced_variables['relative_processing_time_deviation']

        # Generate log-normal noise around base duration
        if self.lognormal_shape_modifier:
            log_normal_factor = np.random.lognormal(mean=0, sigma=0.08)  # Small deviation
            new_duration = base_duration * log_normal_factor  # Introduce log-normal variation
        else: 
            new_duration = base_duration
        
        return round(new_duration, 0)
    
    def inference(self, operation: Operation, current_tool) -> tuple[int, list[tuple]]:      
        
        last_tool_change =  operation.tool != current_tool
        
        evidence = {
            'last_tool_change': last_tool_change
            # Weitere Evidenzen können hier hinzugefügt werden, falls nötig
        }
                     
        result = self.sample(evidence=evidence)

        # Variablen für relative_processing_time_deviation, machine_state und cleaning initialisieren
        machine_state = None
        cleaning = None

        # Sampling for the relative_processing_time_deviation variable
        if 'relative_processing_time_deviation' in result:
            relative_processing_time_deviation_values = result['relative_processing_time_deviation'].values
            relative_processing_time_deviation_probabilities = relative_processing_time_deviation_values / relative_processing_time_deviation_values.sum()  # Normalize

            # Three possible states: 0.9, 1.0, 1.2
            relative_processing_time_deviation = np.random.choice([0.9, 1.0, 1.2], p=relative_processing_time_deviation_probabilities)
        
        # Sampling für die machine_state-Variable
        if 'machine_state' in result:
            machine_state_values = result['machine_state'].values
            machine_state_probabilities = machine_state_values / machine_state_values.sum()  # Normalisieren
            machine_state = np.random.choice([0, 1], p=machine_state_probabilities)
        
        # Sampling für die cleaning-Variable
        if 'cleaning' in result:
            cleaning_values = result['cleaning'].values
            cleaning_probabilities = cleaning_values / cleaning_values.sum()  # Normalisieren
            cleaning = np.random.choice([0, 1], p=cleaning_probabilities)
        

        inferenced_variables = {
            'last_tool_change': last_tool_change,
            'relative_processing_time_deviation': relative_processing_time_deviation,
            'machine_state': machine_state,
            'cleaning': cleaning
        }
            
        return self.get_new_duration(operation=operation, inferenced_variables=inferenced_variables), inferenced_variables

