import numpy as np
import logging
from modules.logger import Logger


def hamming_distance(matrix1, matrix2):
    """
    Calculate the Hamming distance between two adjacency matrices.

    :param matrix1: First adjacency matrix (numpy array).
    :param matrix2: Second adjacency matrix (numpy array).
    :return: Hamming distance (int).
    """
    # Check that both matrices have the same shape
    assert matrix1.shape == matrix2.shape, "Matrices must have the same shape"
    return np.sum(matrix1 != matrix2)  # Count number of differing elements

def get_adjacency_matrix(edges, nodes):
    """
    Converts an edge list to an adjacency matrix.

    :param edges: List of tuples representing the edges in the graph.
    :param nodes: List of nodes in the graph.
    :return: Adjacency matrix as a numpy array.
    """
    adj_matrix = np.zeros((len(nodes), len(nodes)), dtype=int)
    node_index = {node: i for i, node in enumerate(nodes)}
    
    for parent, child in edges:
        adj_matrix[node_index[parent], node_index[child]] = 1

    return adj_matrix

def compare_structures(truth_model, learned_model):
    """
    Compares the learned structure to the true structure using the Hamming distance.

    :param learned_model: The learned Bayesian Network model.
    :return: True if the structures match, False otherwise.
    """  
    truth_edges = set(truth_model.edges())
    learned_edges = set(learned_model.edges())
    truth_nodes = set(learned_model.nodes())
    learned_nodes = set(learned_model.nodes())
    
    logger = Logger.get_global_logger(category="Model", level=logging.DEBUG, log_to_file=True, log_filename="output/logs/app.log")

    if truth_edges == learned_edges:
        logger.debug(f"The models have the same structure.")
    else:
        logger.debug(f"The models are different.")
        logger.debug(f"Edges only in truth model: {truth_edges - learned_edges}")
        logger.debug(f"Edges only in learned model: {learned_edges}")
        logger.debug(f"Edges only in truth model: {learned_edges}")
        logger.debug(f"Nodes only in model1: {truth_nodes - learned_nodes}")
    
    # Calculate Hamming distance (number of different entries)
    distance =  hamming_distance(get_adjacency_matrix(truth_edges,truth_nodes), get_adjacency_matrix(learned_edges, learned_nodes))
    return distance == 0