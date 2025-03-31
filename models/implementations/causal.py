import pandas as pd
import os
import numpy as np
import networkx as nx
from pgmpy.models import BayesianNetwork
from pgmpy.estimators import BayesianEstimator
from castle.algorithms import PC, GES, CORL, DAG_GNN
from models.abstract.pgmpy import PGMPYModel
from models.utils import compare_structures

class CausalModel(PGMPYModel):    
    def __init__(self, csv_file, truth_model=None, structure_learning_method='CORL', estimator='BDeu', **kwargs):        
        super().__init__()
        self.csv_file = csv_file
        self.truth_model = truth_model
        self.structure_learning_method = structure_learning_method  # Can be 'PC', 'GES', or a custom function
        self.estimator = estimator  # Bayesian Estimation method
        self.model = None
        self.kwargs = kwargs  # Additional arguments for learning algorithms

    def initialize(self):
        self.data = self.read_from_csv(self.csv_file)
        
        if self.truth_model:
            successful_models = self.learn_truth_causal_model()
            self.logger.debug(f"Number of successful learned models: {len(successful_models)}")
            self.model = successful_models[0] if successful_models else self.truth_model.model
        else:
            self.model = self.learn_causal_model()
        
        super().initialize()

    def gcastle_structure_learning(self, method='PC', **kwargs):
        """ Wrapper for gCastle's structure learning algorithms. """
        colum_names = self.data.columns.tolist()
        data_array = np.array(self.data, dtype=float)

        if method == 'PC':
            model = PC(**kwargs)
        elif method == 'GES':
            model = GES(**kwargs)
        elif method == 'CORL':
            model = CORL(device_type="gpu", iteration=1000)
        elif method == 'DAG_GNN':
            model = DAG_GNN(**kwargs)
        else:
            raise ValueError(f"Unsupported method: {method}")

        model.learn(data_array)
        edges = [(colum_names[i], colum_names[j]) for i in range(len(colum_names)) 
                 for j in range(len(colum_names)) if model.causal_matrix[i, j] != 0]
        return edges

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

    def learn_causal_model(self):
        """ Learn a causal model dynamically based on the chosen method. """
        method = self.structure_learning_method

        learned_structure = self.gcastle_structure_learning(method=method, **self.kwargs)
        
        #elif callable(method):  
        #    learned_structure = method(self.data, **self.kwargs)  # Custom function
        #else:
        #    raise ValueError(f"Unsupported structure learning method: {method}")

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
            if compare_structures(truth_model=self.truth_model.model, learned_model=learned_model):
                successful_models.append(learned_model)
                self.logger.debug(f"Model successfully matched the truth model.")
            else:
                self.logger.debug("Failed to match the truth model.")

        except Exception as e:
            self.logger.debug(f"Error learning model: {e}")

        return successful_models
    
    def inference(self, operation, evidence_variable='last_tool_change', do_variable='cleaning', target_variable='relative_processing_time_deviation'):
        """ Perform inference with configurable variables. """
        last_tool_change = operation.tool != operation.machine.current_tool if operation.machine else True
        evidence = {evidence_variable: last_tool_change}

        # Do-Intervention with True and False
        result_do_true = self.sample(evidence=evidence, do={do_variable: True})
        result_do_false = self.sample(evidence=evidence, do={do_variable: False})

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
            inferenced_variables[var] = relative_processing_time_deviation_mapping[np.random.choice(factor.state_names[var], p=factor.values / factor.values.sum())]

        # Compute new duration
        return round(operation.duration * inferenced_variables[target_variable], 0), inferenced_variables
