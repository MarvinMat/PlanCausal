import pandas as pd
import os
import numpy as np
import networkx as nx
from pgmpy.models import DiscreteBayesianNetwork
from pgmpy.estimators import BayesianEstimator
from castle.algorithms import PC, GES, CORL, DAG_GNN, Notears
from pgmpy.estimators import HillClimbSearch, ExhaustiveSearch
from pgmpy.base import DAG
from models.abstract.pgmpy import PGMPYModel
from models.utils import compare_structures
from modules.simulation import Operation
from sklearn.mixture import GaussianMixture

class CausalContinousSmallLogLearnModel(PGMPYModel):    
    def __init__(self, csv_file, seed = 0, truth_model=None, structure_learning_lib = 'pgmpy', structure_learning_method='HillClimbSearch', estimator='K2', **kwargs):        
        super().__init__()
        #self.seed = seed
        self.csv_file = csv_file
        self.truth_model = truth_model
        self.edges = []
        self.structure_learning_lib = structure_learning_lib
        self.structure_learning_method = structure_learning_method  # Can be 'PC', 'GES', or a custom function
        self.estimator = estimator  # Bayesian Estimation method
        self.distributions = {}
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
        
        #self.use_distributions()
        self.learn_distributions(self.model.edges)
        #self.logger.debug(f"Learned distributions: {self.distributions}")
        
        super().initialize()
        
    def learn_distributions(self, edges):
        # Filter data to only include the target variable and its parent variables
        target_variable = 'relative_processing_time_deviation'
        parent_variables = [edge[0] for edge in edges if edge[1] == target_variable]
        relevant_columns = parent_variables + [target_variable]
        filtered_data = self.data[relevant_columns].dropna()

        # Iterate over all unique combinations of parent variable values
        parent_combinations = filtered_data[parent_variables].drop_duplicates()
        for _, combination in parent_combinations.iterrows():
            # Filter data for the current combination of parent variable values
            condition = (filtered_data[parent_variables] == combination.values).all(axis=1)
            subset = filtered_data[condition][[target_variable]]

            # Fit a Gaussian Mixture Model to estimate the distribution
            gmm = GaussianMixture(n_components=1, random_state=42)
            gmm.fit(subset)

            # Extract the mean and variance of the Gaussian distribution
            mean = gmm.means_.flatten()[0]
            variance = gmm.covariances_.flatten()[0]

            # Store the distribution parameters with variable names and parent values
            self.distributions[(target_variable, combination.values[0])] = {'mean': mean, 'variance': variance}
        
        print(f"{self.distributions}")
    
    def learn_distributions_old(self, edges):
        # Filter data to only include the target variable and its parent variables
        target_variable = 'relative_processing_time_deviation'
        parent_variables = [edge[0] for edge in edges if edge[1] == target_variable]
        relevant_columns = parent_variables + [target_variable]
        filtered_data = self.data[relevant_columns].dropna()

        # Iterate over all unique combinations of parent variable values
        parent_combinations = filtered_data[parent_variables].drop_duplicates()
        for _, combination in parent_combinations.iterrows():
            # Filter data for the current combination of parent variable values
            condition = (filtered_data[parent_variables] == combination.values).all(axis=1)
            subset = filtered_data[condition][[target_variable]].values.flatten()
            if len(subset) > 0:
                log_data = np.log(subset)
                mu = np.mean(log_data)  # Mean of log-transformed data
                sigma = np.std(log_data)  # Standard deviation of log-transformed data
                self.distributions[(target_variable, combination.values[0])] = {
                    'mu': mu,
                    'sigma': sigma
                }
            else:
                self.logger.warning(f"No data available for combination: {combination.values}")
                
        print(f"{self.distributions}")
            
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
        """ Wrapper for gCastle's structure learning algorithms with proper dtype and fallback. """
        colum_names = self.data.columns.tolist()
        data_numeric = self.data.copy()

        # Convert bools to int to avoid float-only assumptions
        for col in data_numeric.select_dtypes(include='bool').columns:
            data_numeric[col] = data_numeric[col].astype(int)

        data_array = data_numeric.to_numpy(dtype=float)

        algorithms = {
            'PC': PC,
            'GES': lambda **kw: GES(**kw),
            'CORL': lambda **kw: CORL(device_type="gpu", iteration=1000, **kw),
            'DAG_GNN': lambda **kw: DAG_GNN(device_type="gpu", **kw),
            'Notears': Notears
        }

        if method not in algorithms:
            raise ValueError(f"Unsupported method at gcastle: {method}")

        try:
            model = algorithms[method](**kwargs)
            model.learn(data_array)
        except Exception as e:
            print(f"[gCastle] Learning failed for method '{method}': {e}")
            return []

        return [(colum_names[i], colum_names[j])
                for i in range(len(colum_names))
                for j in range(len(colum_names))
                if model.causal_matrix[i, j] != 0]
    
    
    def gcastle_structure_learning_old(self, method='PC', **kwargs):
        """ Wrapper for gCastle's structure learning algorithms. """
        colum_names = self.data.columns.tolist()
        data_array = np.array(self.data, dtype=float)

        algorithms = {
            'PC': PC,
            'GES':  lambda **kw: GES(criterion='bic', method='r2', k=1.0, N=5000),
            'CORL': lambda **kw: CORL(device_type="gpu", iteration=1000, **kw),
            'DAG_GNN': lambda **kw: DAG_GNN(device_type="gpu", **kw),
            'Notears': lambda **kw: Notears(**kw),
        }

        if method not in algorithms:
            raise ValueError(f"Unsupported method at gcastle: {method}")

        model = algorithms[method](**kwargs)
        model.learn(data_array)

        return [(colum_names[i], colum_names[j]) 
                for i in range(len(colum_names)) 
                for j in range(len(colum_names)) if model.causal_matrix[i, j] != 0]
    
    def pgmpy_structure_learning(self, method='HillClimbSearch', **kwargs):
        """ Wrapper for gCastle's structure learning algorithms. """
        if method == 'HillClimbSearch':
            est = HillClimbSearch(self.data)
            edges = list(est.estimate(scoring_method='bic-d').edges())
            #k2score, bdeuscore, bdsscore, bicscore, aicscore
            
        elif method == 'ExhaustiveSearch':
            est = ExhaustiveSearch(self.data)
            edges = list(est.estimate().edges())
        elif method == 'GES':
            discrete_vars = ['last_tool_change']  # define discrete vars
            score = BicCGScore(self.data, discrete_vars=discrete_vars)

            hc = HillClimbSearch(self.data)
            model = hc.estimate(scoring_method=score)

            print(model.edges())
            #est = GES(self.data)
            #edges = list(est.estimate(scoring_method="bic-cg").edges())
        else:
            raise ValueError(f"Unsupported method at pgmpy: {method}")
        
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
            self.edges = learned_structure
        elif isinstance(learned_structure, pd.DataFrame):
            # If it's a DataFrame (adjacency matrix), convert it to an edge list
            graph = nx.from_pandas_adjacency(learned_structure, create_using=nx.DiGraph)
            self.edges = graph.edges()  # Get the edges as a list of tuples
        elif isinstance(learned_structure, nx.DiGraph):
            # If it's already a DiGraph, get the edges directly
            self.edges = learned_structure.edges()
        else:
            raise ValueError("Unsupported learned structure format")

        # Now create the Bayesian Network with the edge list
        model = DiscreteBayesianNetwork()
        model.add_edges_from(self.edges)
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
            
        return round(operation.duration * inferenced_variables['relative_processing_time_deviation'], 0), inferenced_variables

    
    def inference_old(self, operation: Operation, current_tool, do_calculus) -> tuple[int, list[tuple]]:
        last_tool_change = operation.tool != current_tool
            
        evidence = {
            'last_tool_change': last_tool_change
        }
        
        # Inferenz durchführen
        result = self.sample(evidence=evidence)
        
        # Sampling for the relative_processing_time_deviation variable
        if 'relative_processing_time_deviation' in result:
            relative_processing_time_deviation_values = result['relative_processing_time_deviation'].values
            relative_processing_time_deviation_probabilities = relative_processing_time_deviation_values / relative_processing_time_deviation_values.sum()  # Normalize

            # Three possible states: 0.9, 1.0, 1.2
            relative_processing_time_deviation = np.random.choice([0.9, 1.0, 1.2], p=relative_processing_time_deviation_probabilities)
        # Use the learned distributions for sampling
        # Extract the parent variable values from the evidence
        
        # Create parent_values using machine_state and cleaning variables
        parent_values = last_tool_change
        # Check if the parent combination exists in the learned distributions
        key = ('relative_processing_time_deviation', parent_values)
        if key in self.distributions:
            params = self.distributions[key]
            mu = params['mu']
            sigma = params['sigma']
            relative_processing_time_deviation = np.random.lognormal(mean=mu, sigma=sigma)
        else:
            self.logger.error(f"No distribution found for parent values: {parent_values}. Using default mean and variance.")

        inferenced_variables = {
            'last_tool_change': last_tool_change,
            'relative_processing_time_deviation': relative_processing_time_deviation
        }
            
        return round(operation.duration * inferenced_variables['relative_processing_time_deviation'], 0), inferenced_variables
