
from pgmpy.models import BayesianNetwork
from pgmpy.factors.discrete import TabularCPD
from models.core.model import Model
import numpy as np

class HybridModel(Model):
    def __init__(self, csv_file=None):
        pre_csv_file = 'data/NonCausalVsCausal_CausalPlan.xlsx'
        self.data = self.read_from_pre_xlsx(pre_csv_file)

        # Load observed data or fallback to pre-existing data
        self.observed_data = self.read_from_observed_csv(csv_file) or self.data
        self.avg_duration = self.get_avg_duration_from_df(self.observed_data)

        # Create the truth model
        self.truth_model = self.create_truth_model(self.data)
        self.true_adj_matrix = self.get_adjacency_matrix(self.truth_model.edges(), self.data.columns)

        # Test algorithms and select a successful model
        successful_combinations = self.test_algorithms(self.data)
        if successful_combinations:
            self.learned_model = successful_combinations[0][2]
            print(f"Selected model from: Algorithm={successful_combinations[0][0]}, Score={successful_combinations[0][1]}")
        else:
            self.learned_model = None
            print("No successful models were found.")

    def test_algorithms(self, data, algorithms=None, scores=None):
        """
        Test various algorithm and scoring combinations to find successful models.
        :param data: Input data as Pandas DataFrame.
        :param algorithms: List of algorithms to test.
        :param scores: List of scoring methods to test.
        :return: List of successful (algorithm, score, model) tuples.
        """
        successful_combinations = []
        algorithms = algorithms or ['hill_climb', 'tree_search', 'exhaustive', 'pc']
        scores = scores or ['BDeu', 'Bic', 'K2']

        for algorithm in algorithms:
            for score in scores:
                try:
                    print(f"Testing combination: Algorithm={algorithm}, Score={score}")
                    learned_model = self.learn_model_from_data(data, algorithm=algorithm, score_type=score)
                    if self.compare_structures(learned_model):
                        successful_combinations.append((algorithm, score, learned_model))
                except Exception as e:
                    print(f"Error testing Algorithm={algorithm}, Score={score}: {e}")
        return successful_combinations


    def test_algorithms(self, data, algorithms=None, scores=None):
        # 1. Netzwerkstruktur definieren
        model = BayesianNetwork([
            ('Letzter_Werkzeugwechsel', 'Arbeitsgangdauer'),
            ('Maschinenstatus', 'Arbeitsgangdauer'),
            ('Maschinenreinigung', 'Arbeitsgangdauer')
        ])

        # 2. Diskrete CPDs definieren (für diskrete Knoten)
        cpd_werkzeugwechsel = TabularCPD(variable='Letzter_Werkzeugwechsel', variable_card=2, 
                                        values=[[0.8, 0.2]])  # P(>30 min) = 80%, P(<=30 min) = 20%
        cpd_maschinenstatus = TabularCPD(variable='Maschinenstatus', variable_card=2, 
                                        values=[[0.65, 0.35]])  # P(schlecht) = 65%, P(gut) = 35%
        cpd_maschinenreinigung = TabularCPD(variable='Maschinenreinigung', variable_card=2, 
                                            values=[[0.675, 0.325]])  # P(nein) = 67.5%, P(ja) = 32.5%

        # 3. CPDs zum Modell hinzufügen
        model.add_cpds(cpd_werkzeugwechsel, cpd_maschinenstatus, cpd_maschinenreinigung)

        # 4. Kontinuierliche Arbeitsgangdauer berechnen
    def calculate_continuous_delay(werkzeugwechsel, status, reinigung):
        # Beispiel CPT für mean/variance
        cpt = {
            ('>30 Minuten', 'schlecht', 'nein'): {'mean': 1.5, 'variance': 0.3},
            ('>30 Minuten', 'schlecht', 'ja'): {'mean': 1.2, 'variance': 0.2},
            ('>30 Minuten', 'gut', 'nein'): {'mean': 1.1, 'variance': 0.15},
            ('<=30 Minuten', 'gut', 'ja'): {'mean': 0.9, 'variance': 0.1},
        }
        key = (werkzeugwechsel, status, reinigung)
        if key in cpt:
            mean = cpt[key]['mean']
            variance = cpt[key]['variance']
            # Gauß'sche Zufallsvariable basierend auf mean und variance
            delay = np.random.normal(mean, np.sqrt(variance))
            return delay
        else:
            raise ValueError(f"Unbekannte Kombination: {key}")

    def test_inference(self):
        # Test mit einer spezifischen Kombination
        result = calculate_continuous_delay('>30 Minuten', 'schlecht', 'nein')
        print(f"Simulierte Arbeitsgangdauer: {result:.2f}")
