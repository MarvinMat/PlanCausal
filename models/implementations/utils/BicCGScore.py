from pgmpy.estimators import DecomposableScore
import numpy as np
import pandas as pd
from itertools import product

class BicCGScore(DecomposableScore):
    """
    BIC Score for Conditional Gaussian Bayesian Networks (discrete + continuous variables).
    Assumes continuous child + discrete parents.
    """

    def __init__(self, data, discrete_vars=None, **kwargs):
        super().__init__(data, **kwargs)
        self.discrete_vars = set(discrete_vars or [])

    def local_score(self, variable, parents):
        var_type = 'discrete' if variable in self.discrete_vars else 'continuous'
        parent_types = ['discrete' if p in self.discrete_vars else 'continuous' for p in parents]

        if var_type == 'discrete':
            return self._score_discrete(variable, parents)
        else:
            return self._score_continuous(variable, parents, parent_types)

    def _score_discrete(self, var, parents):
        """
        Standard BIC score for discrete variable.
        """
        data = self.data
        N = len(data)
        value_counts = data.groupby([*parents, var]).size()
        parent_counts = data.groupby(parents).size()
        log_likelihood = 0
        k = 0

        for parent_cfg in parent_counts.index:
            parent_cfg = parent_cfg if isinstance(parent_cfg, tuple) else (parent_cfg,)
            parent_count = parent_counts[parent_cfg]

            for val in data[var].unique():
                count = value_counts.get(parent_cfg + (val,), 0)
                if count > 0:
                    log_likelihood += count * np.log(count / parent_count)

            k += len(data[var].unique()) - 1

        bic = log_likelihood - (k / 2) * np.log(N)
        return bic

    def _score_continuous(self, var, parents, parent_types):
        """
        BIC score for continuous variable with possibly discrete parents.
        """
        data = self.data
        N = len(data)

        # Separate discrete and continuous parents
        discrete_parents = [p for p, t in zip(parents, parent_types) if t == 'discrete']
        continuous_parents = [p for p, t in zip(parents, parent_types) if t == 'continuous']

        if not discrete_parents:
            # Just a linear regression
            X = data[continuous_parents] if continuous_parents else np.ones((N, 1))
            y = data[var]
            return self._bic_linear(y, X)
        else:
            # Conditional on discrete parent configs
            group = data.groupby(discrete_parents)
            log_likelihood = 0
            k_total = 0
            for _, subdata in group:
                if len(subdata) < 2:
                    continue
                X = subdata[continuous_parents] if continuous_parents else np.ones((len(subdata), 1))
                y = subdata[var]
                ll, k = self._bic_linear(y, X, return_k=True)
                log_likelihood += ll
                k_total += k
            bic = log_likelihood - (k_total / 2) * np.log(N)
            return bic

    def _bic_linear(self, y, X, return_k=False):
        """
        BIC log-likelihood for linear regression.
        """
        from sklearn.linear_model import LinearRegression

        reg = LinearRegression().fit(X, y)
        y_pred = reg.predict(X)
        resid = y - y_pred
        sse = np.sum(resid**2)
        n = len(y)
        sigma2 = sse / n
        log_likelihood = -0.5 * n * (np.log(2 * np.pi) + np.log(sigma2) + 1)
        k = X.shape[1] + 1  # parameters = weights + variance
        bic = log_likelihood
        return (bic, k) if return_k else bic
