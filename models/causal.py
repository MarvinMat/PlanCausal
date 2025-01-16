import pandas as pd
import numpy as np
import os
from factory.Operation import Operation
from pgmpy.models import BayesianNetwork
from pgmpy.estimators import HillClimbSearch, BicScore, BayesianEstimator, BDeuScore, TreeSearch, ExhaustiveSearch, K2Score, StructureScore, BDsScore, AICScore
from pgmpy.inference import VariableElimination, CausalInference

class CausalModel:
    def __init__(self, csv_file=None):
        # Initialisierung des gelernten Modells basierend auf CSV-Daten
        pre_csv_file = 'data/NonCausalVsCausal_CausalPlan.xlsx'  # Hier den Pfad zur CSV-Datei angeben
        csv_file = csv_file

        # Load pre-existing data
        self.data = self.read_from_pre_xlsx(pre_csv_file)
         
        self.observed_data = self.read_from_observed_csv(csv_file) 
        # Check if observed_data is empty or None
        if self.observed_data is None or self.observed_data.empty:
            print("No observed data found, using pre-existing data.")
            self.observed_data = self.data  # Use the pre-existing data instead

        self.avg_duration = self.get_avg_duration_from_df(self.observed_data)       
              
        self.truth_model = self.create_truth_model(self.data)       
        # Load observed data if provided
        self.observed_data = self.read_from_observed_csv(csv_file)

        # Use observed_data if available, otherwise fall back to self.data
        self.observed_data = self.observed_data if self.observed_data is not None and not self.observed_data.empty else self.data
        
        self.avg_duration = self.get_avg_duration_from_df(self.observed_data)

        # Create truth model based on pre-existing data
        self.truth_model = self.create_truth_model(self.data)
        self.true_adj_matrix = self.get_adjacency_matrix(self.truth_model.edges(), self.data.columns)
        successful_combinations = self.test_algorithms(self.data)
        print("Anzahl der erfolgreich gelernten Modelle: ", len(successful_combinations))
        # Nimm die erste erfolgreiche Kombination
        self.learned_model = successful_combinations[0][2]
        #self.learned_model = self.learn_model_from_data(self.observed_data, algorithm=successful_combinations[0][0], score_type=successful_combinations[0][1]) if self.observed_data is not None else None

        # Test algorithms and select the first successful combination
        successful_combinations = self.test_algorithms(self.data)
        print("Number of successful learned models: ", len(successful_combinations))
        
        if successful_combinations:
            # Use the first successful model
            #self.learned_model = self.learn_model_from_data(self.observed_data, algorithm=successful_combinations[0][0], score_type=successful_combinations[0][1]) if self.observed_data is not None else None
            self.learned_model = successful_combinations[0][2]
        else:
            self.learned_model = None
            print("No successful models were found.")
            

    def read_from_pre_xlsx(self, file):
        data = pd.read_excel(file)
        data.drop(columns=data.columns[0], axis=1, inplace=True)
        return data
    
    def read_from_observed_csv(self, file): 
        # Check if file path is provided and file exists
        if not file or not os.path.exists(file):
            print(f"File not found: {file}. Using truth data.")
            return None

        try:
            # Attempt to read the CSV file
            data = pd.read_csv(file)
            data.drop(columns=data.columns[0], axis=1, inplace=True)
            return data
        except Exception as e:
            print(f"Error reading file {file}: {e}")
            return None
    
    def get_avg_duration_from_df(self, data):
        return data['delay'].mean()
    
    def hamming_distance(self, matrix1, matrix2):
        """
        Calculate the Hamming distance between two adjacency matrices.

        :param matrix1: First adjacency matrix (numpy array).
        :param matrix2: Second adjacency matrix (numpy array).
        :return: Hamming distance (int).
        """
        # Check that both matrices have the same shape
        assert matrix1.shape == matrix2.shape, "Matrices must have the same shape"
        return np.sum(matrix1 != matrix2)  # Count number of differing elements
    
    def get_adjacency_matrix(self, edges, nodes):
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

    def compare_structures(self, learned_model):
        """
        Compares the learned structure to the true structure using the Hamming distance.

        :param learned_model: The learned Bayesian Network model.
        :return: True if the structures match, False otherwise.
        """
        # Get adjacency matrix of the learned model
        learned_adj_matrix = self.get_adjacency_matrix(learned_model.edges(), self.data.columns)

        # Calculate Hamming distance (number of different entries)
        distance =  self.hamming_distance(self.true_adj_matrix, learned_adj_matrix)
        return distance == 0

    def create_truth_model(self, data):
        print("Set edges by user")
            # Defining the Bayesian network structure manually based on your specified edges
        model = BayesianNetwork([
            ('previous_machine_pause', 'machine_status'),
            ('machine_status', 'delay'),
            ('machine_status', 'pre_processing'),
            ('pre_processing', 'delay')
        ])
        
        model.name = "Predefined_Causal_Model"  # Add model name attribute

        # Learn the parameters (CPDs) of the Bayesian network from the data
        model.fit(data, estimator=BayesianEstimator, prior_type="BDeu")
        
        # Verify the model structure and parameters
        assert model.check_model(), "The model is not valid!"

        self.safe_model(model)
        
        # Save and return the learned model
        return model

    def learn_model_from_data(self, data, algorithm='hill_climb', score_type='BDeu', equivalent_sample_size=5):
        """
        Lerne ein kausales Modell aus den gegebenen Daten mit verschiedenen Algorithmen und Score-Funktionen.

        :param data: Eingabedaten als Pandas DataFrame.
        :param algorithm: Algorithmus zur Strukturfindung ('hill_climb', 'tree_search', 'exhaustive', 'pc').
        :param score_type: Bewertungsmethode für das Modell ('BDeu', 'Bic', 'K2').
        :param equivalent_sample_size: Äquivalente Stichprobengröße für BDeu-Score (nur relevant für 'BDeu').
        :return: Gelerntes BayesianNetwork-Modell.
        """
        
        print(f"Lerne Modell mit {algorithm}-Algorithmus und {score_type}-Score")

        # Wähle die Scoring-Methode basierend auf den Parametern
        if score_type == 'BDeu':
            scoring_method = BDeuScore(data, equivalent_sample_size=equivalent_sample_size)
        elif score_type == 'Bic':
            scoring_method = BicScore(data)
        elif score_type == 'K2':
            scoring_method = K2Score(data)
        elif score_type == 'StructureScore':
            scoring_method = StructureScore(data)
        elif score_type == 'BDsScore':
            scoring_method = BDsScore(data)
        elif score_type == 'AICScore':
            scoring_method = AICScore(data)    
        else:
            raise ValueError(f"Unbekannter Score-Typ: {score_type}")

        # Algorithmusauswahl
        if algorithm == 'hill_climb':
            search_alg = HillClimbSearch(data, use_cache=False)
            best_model = search_alg.estimate(scoring_method=scoring_method)
        elif algorithm == 'tree_search':
            search_alg = TreeSearch(data, root_node='previous_machine_pause')  # Beispiel für TreeSearch (root_node definieren)
            best_model = search_alg.estimate()
        elif algorithm == 'exhaustive':
            search_alg = ExhaustiveSearch(data, scoring_method=scoring_method, use_cache=False)
            best_model = search_alg.estimate()
        elif algorithm == 'pc':
            from pgmpy.estimators import PC
            search_alg = PC(data)
            search_alg.max_cond_vars = 3  # Maximale Anzahl bedingter Variablen (einstellbar)
            best_model = search_alg.estimate(significance_level=0.05)
            #return BayesianNetwork(best_model.edges())
        else:
            raise ValueError(f"Unbekannter Algorithmus: {algorithm}")

        # Struktur mit der gewählten Suchmethode lernen
              
        model = BayesianNetwork(best_model.edges())

        model.name = f"Learned_Model_{algorithm}_{score_type}"  # Setzt den Modellnamen für die spätere Speicherung
        
        # Anpassung der CPDs für das Modell basierend auf den Daten
        model.fit(data, estimator=BayesianEstimator, prior_type="BDeu")
        
        # Modellüberprüfung
        assert model.check_model()
        
        # compare with truth graph
        model_check = self.compare_structures(learned_model=model)
        
        if not model_check:
            print('Learned model does not represent truth model')
        
        self.safe_model(model)
        return model
    
    def test_algorithms(self, data):
        """
        Testet verschiedene Algorithmus- und Scoring-Kombinationen und gibt diejenigen zurück,
        die das 'truth model' korrekt nachbilden.

        :param data: Eingabedaten als Pandas DataFrame.
        :return: Liste der erfolgreichen (Algorithmus, Score)-Kombinationen.
        """
        successful_combinations = []

        # Definiere mögliche Algorithmen und Scores
        # algorithms = ['hill_climb', 'tree_search', 'exhaustive', 'pc']
        # scores = ['BDeu', 'Bic', 'K2', 'StructureScore', 'BDsScore', 'AICScore']

        # exhaustive Search einziges Modell, bei dem zuverlässig der truth Graph gefunden wird

        algorithms = ['exhaustive']
        scores = ['K2']

        # Iteriere über alle Algorithmus- und Score-Kombinationen
        for algorithm in algorithms:
            for score in scores:
                try:
                    print(f"Testing combination: Algorithm={algorithm}, Score={score}")

                    # Versuche, das Modell mit der aktuellen Kombination zu lernen
                    learned_model = self.learn_model_from_data(data, algorithm=algorithm, score_type=score)
                    
                    # Überprüfe, ob das gelernte Modell der Wahrheit entspricht
                    model_check = self.compare_structures(learned_model=learned_model)

                    if model_check:
                        print(f"Successful combination: Algorithm={algorithm}, Score={score}")
                        successful_combinations.append((algorithm, score, learned_model))
                    else:
                        print(f"Failed combination: Algorithm={algorithm}, Score={score}")

                except Exception as e:
                    print(f"Error with combination Algorithm={algorithm}, Score={score}: {e}")

        return successful_combinations
    
    def safe_model(self, model):
        model_filename = f"causal/{model.name}.png" if hasattr(model, 'name') else "causal/causal_model.png"
        model_graphviz = model.to_graphviz()
        model_graphviz.draw(model_filename, prog="dot")
        return model_graphviz

    def infer(self, model, variable={}, evidence={}, do={}):
        """
        Führt eine Inferenz auf dem gelernten Modell durch.
        """
        if not model:
            raise ValueError("Kein Modell zur Inference vorhanden.")
        
        if not variable:
            all_model_variables = self.learned_model.nodes()
        else:
            all_model_variables = variable
        
        # VariableElimination für reguläre Inferenz
        inference = VariableElimination(model)

        # CausalInference-Objekt für kausale Abfragen (do-Operator)
        causal_inference = CausalInference(model)

        result = {}     

        # Reguläre Inferenz ohne "do"-Intervention
        if not any(do):
            for variable in all_model_variables:
                if variable not in evidence:
                    # Nur Variablen abfragen, die nicht in der Evidenz enthalten sind
                    query_result = inference.query(variables=[variable], evidence=evidence, joint=True)
                    result[variable] = query_result

        # Kausale Inferenz (do-Intervention)
                
        else:
            #print(f"Durchführung einer 'do'-Intervention: Setze 'pre_processing' auf {do}")
            for variable in all_model_variables:
                if variable not in evidence:
                    do_result = causal_inference.query(variables=[variable], do=do, joint=True)
                    result[variable] = do_result

        return result

    def infer_duration(self, use_truth_model, operation: Operation, tool):
        # Beispielaufruf mit CSV-Datei (Dateipfad anpassen)
        model = self.truth_model if use_truth_model else self.learned_model
        
        previous_machine_pause =  operation.tool != tool
        evidence = {
            'previous_machine_pause': previous_machine_pause
            # Weitere Evidenzen können hier hinzugefügt werden, falls nötig
        }

        # Inferenz durchführen
        result = self.infer(model, evidence=evidence)

        # Variablen für delay, machine_status und pre_processing initialisieren
        has_delay = False
        machine_status = None
        pre_processing = None

        # Sampling für die delay-Variable
        if 'delay' in result:
            delay_values = result['delay'].values
            if len(delay_values) == 2:
                # Wahrscheinlichkeiten extrahieren
                delay_probabilities = delay_values / delay_values.sum()  # Normalisieren
                # Zustand für delay basierend auf den Wahrscheinlichkeiten würfeln
                has_delay = np.random.choice([0, 1], p=delay_probabilities)
        
        # Sampling für die machine_status-Variable
        if 'machine_status' in result:
            machine_status_values = result['machine_status'].values
            machine_status_probabilities = machine_status_values / machine_status_values.sum()  # Normalisieren
            machine_status = np.random.choice([0, 1], p=machine_status_probabilities)
        
        # Sampling für die pre_processing-Variable
        if 'pre_processing' in result:
            pre_processing_values = result['pre_processing'].values
            pre_processing_probabilities = pre_processing_values / pre_processing_values.sum()  # Normalisieren
            pre_processing = np.random.choice([0, 1], p=pre_processing_probabilities)
        
        # Berechnung des Multiplikators
        delay = 1.2 if has_delay else 1.0

        # Rückgabe eines Dictionaries mit allen relevanten Informationen
        return {
            'previous_machine_pause': previous_machine_pause,
            'delay': delay,
            'machine_status': machine_status,
            'pre_processing': pre_processing
        }
