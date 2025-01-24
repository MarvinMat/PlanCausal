import pandas as pd
import numpy as np
import os
from modules.factory.Operation import Operation
from pgmpy.models import BayesianNetwork
from models.abstract.model import Model
from pgmpy.factors.discrete import TabularCPD
from pgmpy.estimators import HillClimbSearch, BicScore, BayesianEstimator, BDeuScore, TreeSearch, ExhaustiveSearch, K2Score, StructureScore, BDsScore, AICScore
from models.abstract.pgmpy import PGMPYModel

from models.utils import compare_structures

class CausalModel(PGMPYModel):
    def __init__(self, csv_file, truth_model=None):        
        self.data = self.read_from_csv(csv_file)
        self.truth_model = truth_model
        
        if self.truth_model is not None: 
            # Test algorithms and select the first successful combination
            successful_combinations = self.learn_truth_causal_model()
            print("Number of successful learned models: ", len(successful_combinations))
            
            if successful_combinations:
                self.model = successful_combinations[0][2]
            else:
                raise ValueError("No successful models were found.")
        else:
            self.model = self.learn_causal_model()
        super().__init__()
        
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

    def learn_causal_model(self, algorithm='exhaustive', score_type='K2', equivalent_sample_size=5):
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
            scoring_method = BDeuScore(self.data, equivalent_sample_size=equivalent_sample_size)
        elif score_type == 'Bic':
            scoring_method = BicScore(self.data)
        elif score_type == 'K2':
            scoring_method = K2Score(self.data)
        elif score_type == 'StructureScore':
            scoring_method = StructureScore(self.data)
        elif score_type == 'BDsScore':
            scoring_method = BDsScore(self.data)
        elif score_type == 'AICScore':
            scoring_method = AICScore(self.data)    
        else:
            raise ValueError(f"Unbekannter Score-Typ: {score_type}")

        # Algorithmusauswahl
        if algorithm == 'hill_climb':
            search_alg = HillClimbSearch(self.data, use_cache=False)
            best_model = search_alg.estimate(scoring_method=scoring_method)
        elif algorithm == 'tree_search':
            search_alg = TreeSearch(self.data, root_node='previous_machine_pause')  # Beispiel für TreeSearch (root_node definieren)
            best_model = search_alg.estimate()
        elif algorithm == 'exhaustive':
            search_alg = ExhaustiveSearch(self.data, scoring_method=scoring_method, use_cache=False)
            best_model = search_alg.estimate()
        elif algorithm == 'pc':
            from pgmpy.estimators import PC
            search_alg = PC(self.data)
            search_alg.max_cond_vars = 3  # Maximale Anzahl bedingter Variablen (einstellbar)
            best_model = search_alg.estimate(significance_level=0.05)
            #return BayesianNetwork(best_model.edges())
        else:
            raise ValueError(f"Unbekannter Algorithmus: {algorithm}")

        # Struktur mit der gewählten Suchmethode lernen
              
        model = BayesianNetwork(best_model.edges())

        model.name = f"Learned_Model_{algorithm}_{score_type}"  # Setzt den Modellnamen für die spätere Speicherung
        
        # Anpassung der CPDs für das Modell basierend auf den Daten
        model.fit(self.data, estimator=BayesianEstimator, prior_type="BDeu")
        
        # Modellüberprüfung
        assert model.check_model()
        
        self.safe_model(model)
        return model
    
    def learn_truth_causal_model(self):
        """
        Testet verschiedene Algorithmus- und Scoring-Kombinationen und gibt diejenigen zurück,
        die das 'truth model' korrekt nachbilden.

        :param data: Eingabedaten als Pandas DataFrame.
        :return: Liste der erfolgreichen (Algorithmus, Score)-Kombinationen.
        """
        successful_combinations = []

        # Definiere mögliche Algorithmen und Scores
        algorithms = ['hill_climb', 'tree_search', 'exhaustive', 'pc']
        scores = ['BDeu', 'Bic', 'K2', 'StructureScore', 'BDsScore', 'AICScore']

        # exhaustive Search einziges Modell, bei dem zuverlässig der truth Graph gefunden wird

        

        # Iteriere über alle Algorithmus- und Score-Kombinationen
        for algorithm in algorithms:
            for score in scores:
                try:
                    print(f"Testing combination: Algorithm={algorithm}, Score={score}")

                    # Versuche, das Modell mit der aktuellen Kombination zu lernen
                    learned_model = self.learn_causal_model(self.data, algorithm=algorithm, score_type=score)
                    
                    # Überprüfe, ob das gelernte Modell der Wahrheit entspricht
                    model_check = compare_structures(truth_graph=self.truth_model ,learned_model=learned_model)

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
    
    def get_new_duration(self, operation, inferenced_variables) -> int:
        new_duration = round(operation.duration * inferenced_variables['delay'],0)
        return new_duration
    
    def inference(self, operation: Operation) -> tuple[int, list[tuple]]:      
        
        if operation.machine is not None:
            previous_machine_pause =  operation.tool != operation.machine.current_tool
        else:
            previous_machine_pause = True
            
        evidence = {
            'previous_machine_pause': previous_machine_pause
            # Weitere Evidenzen können hier hinzugefügt werden, falls nötig
        }

        # Inferenz durchführen
        result = self.sample(evidence=evidence)

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

        inferenced_variables = {
            'previous_machine_pause': previous_machine_pause,
            'delay': delay,
            'machine_status': machine_status,
            'pre_processing': pre_processing
        }
            
        return self.get_new_duration(operation=operation, inferenced_variables=inferenced_variables), inferenced_variables
