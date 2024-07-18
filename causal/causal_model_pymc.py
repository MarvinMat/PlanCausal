import pandas as pd
import numpy as np
from factory.Operation import Operation
import pymc as pm

class CausalModelPyMC:
    def __init__(self, csv_file=None):
        if csv_file is not None:
            self.data = pd.read_csv(csv_file)
            self.model, self.trace = self.learn_model_from_data()
        else:
            self.model = self.trace = None

    def learn_model_from_data(self):
        with pm.Model() as model:
            # Priors for unknown model parameters
            intercept = pm.Normal('intercept', mu=0, sigma=1)
            pres_count_coef = pm.Normal('pres_count_coef', mu=0, sigma=1)
            machine_coef = pm.Normal('machine_coef', mu=0, sigma=1)
            sigma = pm.HalfCauchy('sigma', beta=1)  # Standard deviation of duration distribution

            # Linear combination of predictors
            mu = intercept + pres_count_coef * self.data['operation_pres_count'] + machine_coef * self.data['operation_req_machine']

            # Likelihood of observed data
            duration_obs = pm.Normal('duration', mu=mu, sigma=sigma, observed=self.data['operation_duration'])

            # Inference
            trace = pm.sample(500, return_inferencedata=False)

        return model, trace

    def infer_duration(self,  use_predefined, operation:Operation):
        if self.model is not None:
            with self.model:
                # Set new data for prediction
                pm.set_data({
                    'operation_pres_count': [len(operation.predecessor_operations)],
                    'operation_req_machine': [operation.req_machine_group_id]
                })

                # Predictive posterior sampling
                post_pred = pm.sample_posterior_predictive(self.trace)
                return np.mean(post_pred['duration'])
        else:
            # If no model is learned, use a fallback or error out
            raise ValueError("Model has not been initialized with data.")

