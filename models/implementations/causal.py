import pandas as pd
import os
import numpy as np
import networkx as nx
from pgmpy.models import BayesianNetwork
from pgmpy.estimators import BayesianEstimator
from castle.algorithms import PC, GES, CORL, DAG_GNN, Notears
from pgmpy.estimators import HillClimbSearch, ExhaustiveSearch
from models.abstract.pgmpy import PGMPYModel
from models.utils import compare_structures
from modules.simulation import Operation

class CausalModel(PGMPYModel):    
    def __init__(self, csv_file, truth_model=None, structure_learning_lib = 'pgmpy', structure_learning_method='HillClimbSearch', estimator='BDeu', **kwargs):        
        super().__init__()
        self.csv_file = csv_file
        self.truth_model = truth_model
        self.structure_learning_lib = structure_learning_lib
        self.structure_learning_method = structure_learning_method  # Can be 'PC', 'GES', or a custom function
        self.estimator = estimator  # Bayesian Estimation method
        self.model = None
        self.kwargs = kwargs  # Additional arguments for learning algorithms
        
         #TODO Use Feedback data and merge with oberseved data

    def initialize(self):
        self.data = self.read_from_csv(self.csv_file)
        
        if self.truth_model:
            successful_models = self.learn_truth_causal_model()
            self.logger.debug(f"Number of successful learned models: {len(successful_models)}")
            self.model = successful_models[0] if successful_models else self.truth_model.model
        else:
            self.model = self.learn_causal_model()
        
        super().initialize()
    
    def read_from_csv(self, file): 
        """ Read dataset from CSV, handling errors gracefully. """
        if not file or not os.path.exists(file):
            raise FileExistsError(f"File not found: {file}.")

        try:
            data = pd.read_csv(file)
            data.drop(columns=data.columns[0], axis=1, inplace=True)
            return data
        except Exception as e:
            raise ImportError(f"Error reading file {file}: {e}")
        
    def gcastle_structure_learning(self, method='PC', **kwargs):
        """ Wrapper for gCastle's structure learning algorithms. """
        colum_names = self.data.columns.tolist()
        data_array = np.array(self.data, dtype=float)

        algorithms = {
            'PC': PC,
            'GES': GES,
            'CORL': lambda **kw: CORL(device_type="gpu", iteration=1000, **kw),
            'DAG_GNN': lambda **kw: DAG_GNN(device_type="gpu", **kw),
            'Notears': Notears
        }

        if method not in algorithms:
            raise ValueError(f"Unsupported method: {method}")

        model = algorithms[method](**kwargs)
        model.learn(data_array)

        return [(colum_names[i], colum_names[j]) 
                for i in range(len(colum_names)) 
                for j in range(len(colum_names)) if model.causal_matrix[i, j] != 0]
    
    def pgmpy_structure_learning(self, method='HillClimbSearch', **kwargs):
        """ Wrapper for gCastle's structure learning algorithms. """
        if method == 'HillClimbSearch':
            est = HillClimbSearch(self.data)
            edges = list(est.estimate(scoring_method='bicscore').edges())
        elif method == 'ExhaustiveSearch':
            est = ExhaustiveSearch(self.data)
            edges = list(est.estimate().edges())
        else:
            raise ValueError(f"Unsupported method: {method}")
        
        return edges

    def learn_causal_model(self):
        """ Learn a causal model dynamically based on the chosen method. """
        method_name = f"{self.structure_learning_lib}_structure_learning"

        if not hasattr(self, method_name):
            raise ValueError(f"Unsupported structure learning library: {self.structure_learning_lib}")

        # Dynamically call the method
        structure_learning_function = getattr(self, method_name)
        learned_structure = structure_learning_function(self.structure_learning_method, **self.kwargs)

        # Check if learned_structure is a list, a pandas DataFrame, or a NetworkX graph
        if isinstance(learned_structure, list):
            # If it's a list of edges, use it directly
            edges = learned_structure
        elif isinstance(learned_structure, pd.DataFrame):
            # If it's a DataFrame (adjacency matrix), convert it to an edge list
            graph = nx.from_pandas_adjacency(learned_structure, create_using=nx.DiGraph)
            edges = graph.edges()  # Get the edges as a list of tuples
        elif isinstance(learned_structure, nx.DiGraph):
            # If it's already a DiGraph, get the edges directly
            edges = learned_structure.edges()
        else:
            raise ValueError("Unsupported learned structure format")

        # Now create the Bayesian Network with the edge list
        model = BayesianNetwork(edges)
        model.fit(self.data, estimator=BayesianEstimator, prior_type=self.estimator)

        if not model.check_model():
            raise ValueError("Invalid learned model.")

        return model

    def learn_truth_causal_model(self):
        """ Test various algorithms and retain the best one. """
        successful_models = []

        try:
            learned_model = self.learn_causal_model()
            metrics = compare_structures(truth_model=self.truth_model.model, learned_model=learned_model)
            if metrics['SHD'] == 0:
                successful_models.append(learned_model)
                self.logger.debug(f"Model successfully matched the truth model.")
            else:
                self.logger.debug("Failed to match the truth model.")

        except Exception as e:
            self.logger.debug(f"Error learning model: {e}")

        return successful_models
    
    def inference(self, operation: Operation, current_tool, do_calculus) -> tuple[int, list[tuple]]:      
        
        if do_calculus:
            return self.inference_do_calculus(operation, current_tool)
        
        last_tool_change =  operation.tool != current_tool
            
        evidence = {
            'last_tool_change': last_tool_change
            # Weitere Evidenzen können hier hinzugefügt werden, falls nötig
        }

        # Inferenz durchführen
        result = self.sample(evidence=evidence)

        # Variablen für relative_processing_time_deviation, machine_status und cleaning initialisieren
        relative_processing_time_deviation = None
        machine_status = None
        cleaning = None

        
        # Sampling für die machine_status-Variable
        if 'machine_state' in result:
            machine_state_values = result['machine_state'].values
            machine_state_probabilities = machine_state_values / machine_state_values.sum()  # Normalisieren
            machine_state = np.random.choice([0, 1], p=machine_state_probabilities)
        
        # Sampling für die cleaning-Variable
        if 'cleaning' in result:
            cleaning_values = result['cleaning'].values
            cleaning_probabilities = cleaning_values / cleaning_values.sum()  # Normalisieren
            cleaning = np.random.choice([0, 1], p=cleaning_probabilities)

        evidence = {
            'last_tool_change': last_tool_change,
            'cleaning': cleaning,
            'machine_state': machine_state
        }
        # Inferenz durchführen
        #result = self.sample(evidence=evidence)
        
        # Sampling für die relative_processing_time_deviation-Variable
        if 'relative_processing_time_deviation' in result:
            relative_processing_time_deviation_values = result['relative_processing_time_deviation'].values
            if len(relative_processing_time_deviation_values) == 3:
                # Wahrscheinlichkeiten extrahieren
                relative_processing_time_deviation_probabilities = relative_processing_time_deviation_values / relative_processing_time_deviation_values.sum()  # Normalisieren
                # Zustand für relative_processing_time_deviation basierend auf den Wahrscheinlichkeiten würfeln
                relative_processing_time_deviation = np.random.choice([0.9, 1.0, 1.2], p=relative_processing_time_deviation_probabilities)
        

        inferenced_variables = {
            'last_tool_change': last_tool_change,
            'relative_processing_time_deviation': relative_processing_time_deviation,
            'machine_state': machine_status,
            'cleaning': cleaning
        }
            
        return round(operation.duration * inferenced_variables['relative_processing_time_deviation'], 0), inferenced_variables
    
    def inference_do_calculus(self, operation, current_tool, evidence_variable='last_tool_change', do_variable='cleaning', target_variable='relative_processing_time_deviation'):
        """ Perform inference with configurable variables. """
        
        last_tool_change =  operation.tool != current_tool
        evidence = {
            evidence_variable: last_tool_change
        }

        # Do-Intervention with True and False
        result_do_true = self.sample(evidence=evidence, do={do_variable: 1})
        result_do_false = self.sample(evidence=evidence, do={do_variable: 0})

        # Extract probabilities
        factor_do_true = result_do_true[target_variable]
        factor_do_false = result_do_false[target_variable]
        prob_do_true, prob_do_false = factor_do_true.values[1], factor_do_false.values[1]

        # Select the best intervention
        cleaning = prob_do_true > prob_do_false
        selected_result = result_do_true if cleaning else result_do_false

        # Define mapping for states
        # Pgmpy can only have int states
        relative_processing_time_deviation_mapping = {0: 0.9, 1: 1.0, 2: 1.2}
        
        # Sample dynamically
        inferenced_variables = {evidence_variable: last_tool_change, do_variable: cleaning}
        for var, factor in selected_result.items():
            inferenced_variables[var] = np.random.choice(factor.state_names[var], p=factor.values / factor.values.sum())
            if var == target_variable:
                inferenced_variables[var] = relative_processing_time_deviation_mapping[inferenced_variables[var]]
                
        # Compute new duration
        return round(operation.duration * inferenced_variables[target_variable], 0), inferenced_variables
    
