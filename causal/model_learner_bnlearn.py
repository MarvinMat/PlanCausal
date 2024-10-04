import pandas as pd
import numpy as np
import bnlearn as bn
from factory.Operation import Operation


class ModelLearnerBNLearn:
    def __init__(self, csv_file=None):
        # Initialisierung des gelernten Modells basierend auf CSV-Daten
        csv_file = 'data/NonCausalVsCausal_CausalPlan.xlsx'  # Hier den Pfad zur CSV-Datei angeben
        self.pc_did_run = False
        self.data = self.read_from_csv(csv_file)        
        self.truth_model = self.create_truth_model(self.data)       
        self.true_adj_matrix = self.get_adjacency_matrix(self.truth_model.edges(), self.data.columns)
        successful_combinations = self.test_algorithms(self.data)
        print("successful_combinations", len(successful_combinations))
        self.learned_model = self.learn_model_from_data(self.data, algorithm=successful_combinations[0][0], score_type=successful_combinations[0][1]) if self.data is not None else None

    def read_from_csv(self, csv_file):
        # CSV-Datei einlesen
        #data = pd.read_csv(csv_file, sep=';')
        
        data = pd.read_excel(csv_file)
        data.drop(columns=data.columns[0], axis=1, inplace=True)
        # # Spaltennamen anpassen und kategorische Daten in numerische Werte umwandeln
        # data = data.rename(columns={
        #     'Maschinenstatus': 'machine_status',
        #     'Verzögerung': 'delay',
        #     'vorher Maschinenpause': 'previous_machine_pause',
        #     'Vorverarbeitung': 'pre_processing'
        # })

        # # Kategorische Daten in numerische Werte umwandeln
        # data['machine_status'] = data['machine_status'].map({'Schlecht': 0, 'Gut': 1})
        # data['delay'] = data['delay'].map({'Ja': 1, 'Nein': 0})
        # data['previous_machine_pause'] = data['previous_machine_pause'].map({'Ja': 1, 'Nein': 0})
        # data['pre_processing'] = data['pre_processing'].map({'Ja': 1, 'nein': 0})

        return data