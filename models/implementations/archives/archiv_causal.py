import pandas as pd
import os
import numpy as np
from modules.factory.Operation import Operation
from pgmpy.models import BayesianNetwork
from castle.algorithms import PC, GES  # Import gCastle algorithms
from pgmpy.estimators import BayesianEstimator
from models.abstract.pgmpy import PGMPYModel
import networkx as nx

from models.utils import compare_structures

class CausalModel(PGMPYModel):    
    def __init__(self, csv_file, truth_model=None):        
        super().__init__()
        self.csv_file = csv_file
        self.truth_model = truth_model
        self.model = None
        self.variable_elemination = None
        self.belief_propagation = None
        self.causal_inference = None
        
    def initialize(self):
        self.data = self.read_from_csv(self.csv_file)
        
        if self.truth_model is not None: 
            # Test algorithms and select the first successful combination
            successful_combinations = self.learn_truth_causal_model()
            self.logger.debug(f"Number of successful learned models: {len(successful_combinations)}")
            
            if successful_combinations:
                self.model = successful_combinations[0][2]
            else:
                #Logger
                self.logger.debug("No truth model discovered - Fallback to truth model for inferencing")
                self.model = self.truth_model.model
        else:
            self.model = self.learn_causal_model()
        
        super().initialize()

    def adjacency_to_edges(adj_matrix, variable_names):
        """Convert adjacency matrix to a list of edges."""
        
    
    # Example wrapper for a gCastle structure learning algorithm
    def gcastle_structure_learning(self, method='PC', **kwargs):
        """
        Wrapper for gCastle's structure learning algorithms.

        :param data: Input data as a Pandas DataFrame.
        :param method: The gCastle method to use ('PC', 'GES', etc.).
        :param kwargs: Additional arguments for the gCastle method.
        :return: Learned structure as an adjacency matrix.
        """

        # Convert DataFrame to numpy array
        colum_names = self.data.columns.tolist()
        data_array = np.array(self.data, dtype=float)

        # Initialize and apply the selected algorithm
        if method == 'PC':
            model = PC(**kwargs)
        elif method == 'GES':
            model = GES(**kwargs)
        else:
            raise ValueError(f"Unsupported gCastle method: {method}")

        model.learn(data_array)

        edges = []
        num_nodes = len(colum_names)
        for i in range(num_nodes):
            for j in range(num_nodes):
                if model.causal_matrix[i, j] != 0:
                    edges.append((colum_names[i], colum_names[j]))
        return edges
        
    def read_from_csv(self, file): 
        # Check if file path is provided and file exists
        if not file or not os.path.exists(file):
            raise FileExistsError(f"File not found: {file}.")

        try:
            # Attempt to read the CSV file
            data = pd.read_csv(file)
            data.drop(columns=data.columns[0], axis=1, inplace=True)
            return data
        except Exception as e:
            raise ImportError(f"Error reading file {file}: {e}")
        
    def learn_causal_model(self, structure_learning_func, **kwargs):
        """
        Learn a causal model using a specified structure learning function.

        :param structure_learning_func: Function that learns the structure and returns an adjacency matrix or edge list.
        :param kwargs: Additional arguments for the structure learning function.
        :return: Learned BayesianNetwork model.
        """
        # Learn the structure using the provided function
        learned_structure = structure_learning_func(**kwargs)

        # Convert the learned structure to a NetworkX graph
        if isinstance(learned_structure, nx.DiGraph):
            graph = learned_structure
        elif isinstance(learned_structure, pd.DataFrame):
            graph = nx.from_pandas_adjacency(learned_structure, create_using=nx.DiGraph)
        elif isinstance(learned_structure, list):
            graph = nx.DiGraph(learned_structure)
        else:
            raise ValueError("Unsupported structure format returned by the learning function.")

        # Create a Bayesian Network model from the learned structure
        model = BayesianNetwork(graph.edges())

        # Fit the model with the data using Bayesian Estimator
        model.fit(self.data, estimator=BayesianEstimator, prior_type="BDeu")

        # Validate the model
        if not model.check_model():
            raise ValueError("The learned model is not valid.")

        return model

    def learn_truth_causal_model(self):
        """
        Test various algorithm and scoring combinations to identify those
        that accurately replicate the 'truth model'.

        :return: List of successful (algorithm, score) combinations.
        """
        successful_combinations = []

        try:
            learned_model = self.learn_causal_model(
                structure_learning_func=self.gcastle_structure_learning,
                method='PC' 
            )

            # Verify if the learned model matches the truth model
            model_check = compare_structures(truth_model=self.truth_model.model, learned_model=learned_model)

            if model_check:
                self.logger.debug(f"Successful combination")
                successful_combinations.append(learned_model)
            else:
                self.logger.debug(f"Failed combination")

        except Exception as e:
            self.logger.debug(f"Error with combination {e}")

        return successful_combinations
    
    def safe_model(self, model):
        model_filename = f"causal/{model.name}.png" if hasattr(model, 'name') else "causal/causal_model.png"
        model_graphviz = model.to_graphviz()
        model_graphviz.draw(model_filename, prog="dot")
        return model_graphviz
    
    def get_new_duration(self, operation, inferenced_variables) -> int:
        new_duration = round(operation.duration * inferenced_variables['relative_processing_time_deviation'],0)
        return new_duration
        
    def inference(self, operation: Operation) -> tuple[int, dict]:
        
        evidence_variable = 'last_tool_change'
        do_variable = 'cleaning'
        target_variable = 'relative_processing_time_deviation'
                
        if operation.machine is not None:
            last_tool_change = operation.tool != operation.machine.current_tool
        else:
            last_tool_change = True
        
        evidence = {evidence_variable: last_tool_change}
        
        # Case 1: Do-Intervention with cleaning = True
        result_do_true = self.sample(evidence=evidence, do={do_variable: True})

        # Case 2: Do-Intervention with cleaning = False
        result_do_false = self.sample(evidence=evidence, do={do_variable: False})

        # Retrieve the DiscreteFactor for 'relative_processing_time_deviation'
        factor_do_true = result_do_true[target_variable]
        factor_do_false = result_do_false[target_variable]

        # Identify the index of the state corresponding to a deviation of 1.0
        # Assuming state '1' corresponds to a deviation of 1.0
        state_index = 1

        # Extract the probability of the identified state
        prob_do_true = factor_do_true.values[state_index]
        prob_do_false = factor_do_false.values[state_index]

        # Determine which intervention has the higher probability for the state
        if prob_do_true > prob_do_false:
            selected_result = result_do_true
            cleaning = True
        else:
            selected_result = result_do_false
            cleaning = False

        # Sample values dynamically
        inferenced_variables = {}
        inferenced_variables[evidence_variable] = last_tool_change
        inferenced_variables[do_variable] = cleaning
        for var, factor in selected_result.items():
            values = factor.values
            states = factor.state_names[var]  # Get possible state labels
            probabilities = values / values.sum()  # Normalize

            inferenced_variables[var] = np.random.choice(states, p=probabilities)

        # Compute new duration based on inferred variables
        return self.get_new_duration(operation=operation, inferenced_variables=inferenced_variables), inferenced_variables

