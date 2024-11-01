import os
import sys
sys.path.insert(0, os.getcwd())

import pandas as pd
from factory.Operation import Operation
from simulation.Simulator import Simulator
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
            ('operation_pres_count', 'operation_duration'), 
            ('operation_req_machine', 'operation_duration')
        ])

        cpd_operation_pres_count = TabularCPD(variable='operation_pres_count', variable_card=4, values=[[0.25], [0.25], [0.25], [0.25]], state_names={'operation_pres_count': [0, 1, 2, 3]})

        cpd_operation_req_machine = TabularCPD(variable='operation_req_machine', variable_card=4, values=[[0.25], [0.25], [0.25], [0.25]], state_names={'operation_req_machine': ['a1', 'a2', 'a3', 'a4']})

        duration_values = np.zeros((6, 16))
        def calculate_duration_factor(operation_pres_count, operation_req_machine):
            pres_count_factor = 1.0 + 0.1 * operation_pres_count
            req_machine_factor = 1.0
            
            if operation_req_machine == 'a1':
                req_machine_factor = 0.9
            elif operation_req_machine == 'a2':
                req_machine_factor = 1.0
            elif operation_req_machine == 'a3':
                req_machine_factor = 1.1
            elif operation_req_machine == 'a4':
                req_machine_factor = 1.2

            duration_factor = pres_count_factor * req_machine_factor
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
        for pres_count in range(4):
            for req_machine in ['a1', 'a2', 'a3', 'a4']:
                duration_values[:,cur_col] = calculate_duration_factor(pres_count, req_machine)
                cur_col += 1

        cpd_operation_duration = TabularCPD(variable='operation_duration', variable_card=6,
                                            values=duration_values,
                                            evidence=['operation_pres_count', 'operation_req_machine'],
                                            evidence_card=[4, 4],
                                            state_names={'operation_duration': ['0.8', '0.9', '1.0', '1.1', '1.2', '1.3'],
                                                         'operation_pres_count': [0, 1, 2, 3],
                                                         'operation_req_machine': ['a1', 'a2', 'a3', 'a4']})

        model.add_cpds(cpd_operation_pres_count, cpd_operation_req_machine, cpd_operation_duration)
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

    def infer_duration(self, use_predefined, operation:Operation, tool):
        model = self.predefined_model if use_predefined else self.learned_model
        inference = VariableElimination(model)
        evidence = {
            'operation_pres_count': len(operation.predecessor_operations),
            #'operation_duration': operation.duration,
            'operation_req_machine': operation.req_machine_group_id
        }
        result = inference.query(['operation_duration'], evidence=evidence)
        return float(result.sample(1)["operation_duration"][0])