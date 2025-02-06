import numpy as np
from models.abstract.distribution import DistributionModel

class ExponentialDistributionModel(DistributionModel):
    """
    A distribution model assuming exponential distribution for durations.
    """

    def fit(self, data):
        """
        Fits an exponential distribution (lambda = 1/mean).
        """
        lambda_ = 1 / np.mean(data)
        return (lambda_,)  # Store as a tuple

    def sample(self, params):
        """
        Samples from the fitted exponential distribution.
        """
        lambda_ = params[0]
        return np.random.exponential(1 / lambda_)
