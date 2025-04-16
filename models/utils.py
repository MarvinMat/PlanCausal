import numpy as np
import logging
from modules.logger import Logger
from sklearn.metrics import precision_score, recall_score, f1_score


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
    Compares the learned structure to the true structure using Structural Hamming Distance (SHD),
    Precision, Recall, and F1-score.

    :param truth_model: The ground truth Bayesian Network model.
    :param learned_model: The learned Bayesian Network model.
    :return: A dictionary with SHD, Precision, Recall, and F1-score.
    """  
    truth_edges = set(truth_model.edges())
    learned_edges = set(learned_model.edges())
    truth_nodes = set(truth_model.nodes())
    learned_nodes = set(learned_model.nodes())

    logger = Logger.get_global_logger(category="Model", level=logging.DEBUG, log_to_file=True, log_filename="output/logs/app.log")

    # Calculate Structural Hamming Distance (SHD)
    false_negatives = truth_edges - learned_edges  # Edges in truth but not in learned
    false_positives = learned_edges - truth_edges  # Edges in learned but not in truth
    shd = len(false_negatives) + len(false_positives)

    # Prepare binary vectors for Precision, Recall, and F1-score
    all_possible_edges = truth_edges | learned_edges  # Union of all observed edges
    y_true = [1 if edge in truth_edges else 0 for edge in all_possible_edges]
    y_pred = [1 if edge in learned_edges else 0 for edge in all_possible_edges]

    precision = precision_score(y_true, y_pred, zero_division=0)
    recall = recall_score(y_true, y_pred, zero_division=0)
    f1 = f1_score(y_true, y_pred, zero_division=0)

    logger.debug(f"SHD: {shd}, Precision: {precision:.4f}, Recall: {recall:.4f}, F1-score: {f1:.4f}")

    return {
        "SHD": shd,
        "Precision": precision,
        "Recall": recall,
        "F1-Score": f1
    }