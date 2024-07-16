import pandas as pd
from pgmpy.models import BayesianNetwork
from pgmpy.factors.discrete import TabularCPD, DiscreteFactor
from pgmpy.estimators import HillClimbSearch, BicScore, BayesianEstimator
from pgmpy.inference import VariableElimination
import numpy as np
from scipy.stats import norm

class CausalModel:
    def __init__(self, csv_file=None):
        self.predefined_model = self.create_predefined_model()
        self.learned_model = self.learn_model_from_csv(csv_file) if csv_file is not None else None

    def create_predefined_model(self):
        model = BayesianNetwork([
            ('has_lots_operations', 'is_shorter_than_15'), 
            ('is_shorter_than_15', 'duration'), 
            ('one_working', 'duration')
        ])

        cpd_has_lots_operations = TabularCPD(variable='has_lots_operations', variable_card=2, values=[[0.5], [0.5]], state_names={'has_lots_operations': [False, True]})

        cpd_is_shorter_than_15 = TabularCPD(variable='is_shorter_than_15', variable_card=2,
                                            values=[[0.7, 0.2], [0.3, 0.8]],
                                            evidence=['has_lots_operations'],
                                            evidence_card=[2],
                                            state_names={'is_shorter_than_15': [False, True], 'has_lots_operations': [False, True]})

        cpd_one_working = TabularCPD(variable='one_working', variable_card=2, values=[[0.5], [0.5]], state_names={'one_working': [False, True]})

        duration_values = np.zeros((6, 4))
        def calculate_duration_factor(is_shorter_than_15, one_working):
            is_shorter_than_15_factor = 0.9 if is_shorter_than_15 else 1.1
            one_working_factor = 1.1 if one_working else 1.0

            duration_factor = is_shorter_than_15_factor * one_working_factor
            possible_duration_factors = [0.8, 0.9, 1.0, 1.1, 1.2, 1.3]

            result = []
            prev_value = 0
            for possible_duration_factor in possible_duration_factors:
                value = norm.cdf(possible_duration_factor + 0.05, loc=duration_factor, scale=0.04)
                result.append(value - prev_value)
                prev_value = value

            result[5] += 1 - sum(result)
            return result

        cur_col = 0
        for is_shorter in [False, True]:
            for working in [False, True]:
                duration_values[:,cur_col] = calculate_duration_factor(is_shorter, working)
                cur_col += 1

        cpd_duration = TabularCPD(variable='duration', variable_card=6,
                                  values=duration_values,
                                  evidence=['is_shorter_than_15', 'one_working'],
                                  evidence_card=[2, 2],
                                  state_names={'duration': ['0.8', '0.9', '1.0', '1.1', '1.2', '1.3'],
                                               'is_shorter_than_15': [False, True],
                                               'one_working': [False, True]})

        model.add_cpds(cpd_has_lots_operations, cpd_is_shorter_than_15, cpd_one_working, cpd_duration)
        assert model.check_model()
        return model
    
    def learn_model_from_csv(self, csv_file):
        data = pd.read_csv(csv_file)
        return self.learn_model_from_data(data)

    def learn_model_from_data(self, data):
        # Specify the number of times to repeat each row
        times = 100
        hc = HillClimbSearch(data)
        best_model = hc.estimate(scoring_method=BicScore(data))
        model = BayesianNetwork(best_model.edges())
        model.fit(data, estimator=BayesianEstimator, prior_type="BDeu")
        assert model.check_model()
        return model

    def infer_duration(self, use_predefined, has_lots_operations, is_shorter_than_15, one_working):
        model = self.predefined_model if use_predefined else self.learned_model
        inference = VariableElimination(model)
        evidence = {
            'has_lots_operations': has_lots_operations,
            'is_shorter_than_15': is_shorter_than_15,
            'one_working': one_working
        }
        result = inference.query(['duration'], evidence=evidence)
        return float(result.sample(1)["duration"][0])

# Example usage
csv_file = 'data/observed_data.csv'  # Path to your CSV file

model = CausalModel(csv_file=csv_file)

has_lots_operations = len([1, 2]) > 1
is_shorter_than_15 = 10 < 15
one_working = False

# Decide which model to use for inference
use_predefined = True  # Set this to False to use the learned model

duration = model.infer_duration(use_predefined, has_lots_operations, is_shorter_than_15, one_working)
print("Inferred duration factor:", duration)
