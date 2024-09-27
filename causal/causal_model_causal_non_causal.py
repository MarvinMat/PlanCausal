import pandas as pd
from pgmpy.models import BayesianNetwork
from pgmpy.estimators import HillClimbSearch, BicScore, BayesianEstimator
from pgmpy.inference import VariableElimination, CausalInference

class CausalModel:
    def __init__(self, csv_file=None):
        # Initialisierung des gelernten Modells basierend auf CSV-Daten
        self.learned_model = self.learn_model_from_csv(csv_file) if csv_file is not None else None

    def learn_model_from_csv(self, csv_file):
        # CSV-Datei einlesen
        data = pd.read_csv(csv_file, sep=';')
        
        # Spaltennamen anpassen und kategorische Daten in numerische Werte umwandeln
        data = data.rename(columns={
            'Maschinenstatus': 'machine_status',
            'Verzögerung': 'delay',
            'vorher Maschinenpause': 'previous_machine_pause',
            'Vorverarbeitung': 'pre_processing'
        })

        # Kategorische Daten in numerische Werte umwandeln
        data['machine_status'] = data['machine_status'].map({'Schlecht': 0, 'Gut': 1})
        data['delay'] = data['delay'].map({'Ja': 1, 'Nein': 0})
        data['previous_machine_pause'] = data['previous_machine_pause'].map({'Ja': 1, 'Nein': 0})
        data['pre_processing'] = data['pre_processing'].map({'Ja': 1, 'nein': 0})

        return self.set_truth_edges(data)

    def set_truth_edges(self, data):
        print("Set edges by user")
            # Defining the Bayesian network structure manually based on your specified edges
        model = BayesianNetwork([
            ('previous_machine_pause', 'machine_status'),
            ('machine_status', 'delay'),
            ('machine_status', 'pre_processing'),
            ('pre_processing', 'delay')
        ])
        
        # Learn the parameters (CPDs) of the Bayesian network from the data
        model.fit(data, estimator=BayesianEstimator, prior_type="BDeu")
        
        # Verify the model structure and parameters
        assert model.check_model(), "The model is not valid!"

        self.safe_model(model)
        
        # Save and return the learned model
        return model

    def learn_model_from_data(self, data):
        print('Learn edges from data')
        # Bayesian Network lernen mittels HillClimbSearch und BicScore
        hc = HillClimbSearch(data)
        best_model = hc.estimate(scoring_method=BicScore(data))
        model = BayesianNetwork(best_model.edges())

        # Anpassung der CPDs für das Modell basierend auf den Daten
        model.fit(data, estimator=BayesianEstimator, prior_type="BDeu")
        
        # Modellüberprüfung
        assert model.check_model()
        self.safe_model(model)
        return model
    
    def safe_model(self, model):
        model_graphviz = model.to_graphviz()
        model_graphviz.draw("causal/causal_model.png", prog="dot")
        return model_graphviz

    def infer(self, variable={}, evidence={}, do={}):
        """
        Führt eine Inferenz auf dem gelernten Modell durch.

        :param evidence: Ein Dictionary mit den Beweisen für die Inferenz, z.B. {'machine_status': 1, 'delay': 0}
        :param do_intervention: Wenn True, führt eine "do"-Intervention auf die `pre_processing`-Variable durch
        :return: Wahrscheinlichkeiten für alle Variablen
        """
        if not self.learned_model:
            raise ValueError("Es wurde kein Modell gelernt. Bitte laden Sie ein CSV und erstellen Sie ein Modell.")
        
        if not variable:
            all_model_variables = self.learned_model.nodes()
        else:
            all_model_variables = variable
        
        # VariableElimination für reguläre Inferenz
        inference = VariableElimination(self.learned_model)

        # CausalInference-Objekt für kausale Abfragen (do-Operator)
        causal_inference = CausalInference(self.learned_model)

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



# Beispielaufruf mit CSV-Datei (Dateipfad anpassen)
csv_file = 'data/NonCausalVsCausal.csv'  # Hier den Pfad zur CSV-Datei angeben
model = CausalModel(csv_file)

# Beispielhafte Inferenz durchführen mit Beweisen (ohne do-Operator)
#evidence = {'pre_processing': 1}  # Beispielhafte Evidenz
# Assuming inference is the model you've already set up

# Non-Causal Inference: Conditional probability of delay given pre_processing
non_causal_delay_given_preprocessing_0 = model.infer(evidence={'pre_processing': 0})
non_causal_delay_given_preprocessing_1 = model.infer(evidence={'pre_processing': 1})

print("Non-Causal Probability of delay given pre_processing=0:", non_causal_delay_given_preprocessing_0['delay'])
print("Non-Causal Probability of delay given pre_processing=1:", non_causal_delay_given_preprocessing_1['delay'])

# Causal Inference: Do-Intervention on pre_processing
causal_delay_given_preprocessing_0 = model.infer(variable={'delay'}, do={'pre_processing': 0})
causal_delay_given_preprocessing_1 = model.infer(variable={'delay'}, do={'pre_processing': 1})

print("Causal Probability of delay after do(pre_processing=0):", causal_delay_given_preprocessing_0['delay'])
print("Causal Probability of delay after do(pre_processing=1):", causal_delay_given_preprocessing_1['delay'])