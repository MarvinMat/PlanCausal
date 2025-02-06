import numpy as np
from models.abstract.distribution import DistributionModel

class NormalDistributionModel(DistributionModel):
    """
    A distribution model assuming normal distribution for durations.
    """

    def fit(self, data):
        """
        Fits a normal distribution to the data and returns mean and std.
        """
        return np.mean(data), np.std(data)

    def sample(self, params):
        """
        Samples from the fitted normal distribution.
        """
        mu, sigma = params
        return np.random.normal(mu, sigma)
