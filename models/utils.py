import numpy as np

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

def compare_structures(truth_graph, learned_graph, nodes):
    """
    Compares the learned structure to the true structure using the Hamming distance.

    :param learned_model: The learned Bayesian Network model.
    :return: True if the structures match, False otherwise.
    """
    # Get adjacency matrix of the learned model
    learned_adj_matrix = get_adjacency_matrix(learned_graph.edges(), learned_graph.nodes())

    # Calculate Hamming distance (number of different entries)
    distance =  hamming_distance(truth_graph.adjacency, learned_adj_matrix)
    return distance == 0
