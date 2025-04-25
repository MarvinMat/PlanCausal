import uuid
import numpy as np
from models.abstract.distribution import DistributionModel
import scipy.stats as stats
import matplotlib.pyplot as plt
import os

class HistoDistributionModel(DistributionModel):
    """
    A distribution model assuming log-normal distribution for durations.
    """

    def fit(self, data):
        """
        Fits a log-normal distribution by computing the mean and standard deviation
        of the logarithm of the data.
        """
        #self.check_log_normality(data)
        #self.check_distribution(data)
        
        hist = np.histogram(data, bins=len(data), density=True)
        empirical_dist = stats.rv_histogram(hist)
        
        return (empirical_dist)  # Store as a tuple

    def sample(self, empirical_dist):
        """
        Samples from the fitted log-normal distribution.
        """
        sample = empirical_dist.rvs()
        
        return sample