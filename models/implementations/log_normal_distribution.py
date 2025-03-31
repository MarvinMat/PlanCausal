import numpy as np
from models.abstract.distribution import DistributionModel
import scipy.stats as stats
import matplotlib.pyplot as plt

class LogNormalDistributionModel(DistributionModel):
    """
    A distribution model assuming log-normal distribution for durations.
    """

    def fit(self, data):
        """
        Fits a log-normal distribution by computing the mean and standard deviation
        of the logarithm of the data.
        """
        self.check_log_normality(data)
        log_data = np.log(data)
        mu = np.mean(log_data)  # Mean of log-transformed data
        sigma = np.std(log_data)  # Standard deviation of log-transformed data
        return (mu, sigma)  # Store as a tuple

    def sample(self, params):
        """
        Samples from the fitted log-normal distribution.
        """
        mu, sigma = params
        return np.random.lognormal(mu, sigma)

    def check_log_normality(self, data):
        log_data = np.log(data)

        # Histogram of original and log-transformed data
        fig, axes = plt.subplots(1, 2, figsize=(12, 5))

        axes[0].hist(data, bins=30, alpha=0.7, color="blue", edgecolor="black")
        axes[0].set_title("Histogram of Original Data")

        axes[1].hist(log_data, bins=30, alpha=0.7, color="green", edgecolor="black")
        axes[1].set_title("Histogram of Log-Transformed Data")

        plt.show()

        # Q-Q Plot
        #stats.probplot(log_data, dist="norm", plot=plt)
        plt.title("Q-Q Plot of Log-Transformed Data")
        plt.savefig("test.png")

        # Normality test on log-transformed data
        shapiro_test = stats.shapiro(log_data)
        #print(f"Shapiro-Wilk Test for Log-Transformed Data: p-value = {shapiro_test.pvalue:.5f}")

        #if shapiro_test.pvalue > 0.05:
            #print("Log-transformed data appears to be normally distributed (supports log-normal assumption).")
        #else:
            #print("Log-transformed data is not normally distributed (log-normal assumption may not hold).")