import uuid
import numpy as np
from models.abstract.distribution import DistributionModel
import scipy.stats as stats
import matplotlib.pyplot as plt
import logging
import os

class LogNormalDistributionModel(DistributionModel):
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

        # Create a logs directory if it doesn't exist
        log_dir = "./output/logs"
        os.makedirs(log_dir, exist_ok=True)
        
        distribution_dir = "./output/plots/distribution"
        os.makedirs(distribution_dir, exist_ok=True)

        # Histogram of original and log-transformed data
        fig, axes = plt.subplots(1, 2, figsize=(12, 5))

        axes[0].hist(data, bins=30, alpha=0.7, color="blue", edgecolor="black")
        axes[0].set_title("Histogram of Original Data")

        axes[1].hist(log_data, bins=30, alpha=0.7, color="green", edgecolor="black")
        axes[1].set_title("Histogram of Log-Transformed Data")

        # Save the histogram plot
        unique_id = str(uuid.uuid4())[:8]  # Generate a unique ID for the plot
        histogram_path = os.path.join(distribution_dir, f"data_histograms_{unique_id}.png")
        plt.savefig(histogram_path)
        plt.close(fig)

        # Q-Q Plot for log-transformed data
        fig = plt.figure(figsize=(6, 6))
        stats.probplot(log_data, dist="norm", plot=plt)
        plt.title("Q-Q Plot of Log-Transformed Data")

        # Save the Q-Q plot
        qq_plot_path = os.path.join(distribution_dir, f"qq_plot_log_transformed_{unique_id}.png")
        plt.savefig(qq_plot_path)
        plt.close(fig)

        # Normality test on log-transformed data
        shapiro_test = stats.shapiro(log_data)
        p_value = shapiro_test.pvalue

        # Log the results
        log_file_path = os.path.join(log_dir, "distribution.log")
        with open(log_file_path, "a") as log_file:
            log_file.write("Log-Normality Check:\n")
            log_file.write(f"Shapiro-Wilk Test p-value: {p_value:.5f}\n")
            if p_value > 0.05:
                log_file.write("Log-transformed data appears to be normally distributed (supports log-normal assumption).\n")
            else:
                log_file.write("Log-transformed data is not normally distributed (log-normal assumption may not hold).\n")
            log_file.write(f"Histogram saved at: {histogram_path}\n")
            log_file.write(f"Q-Q Plot saved at: {qq_plot_path}\n")
            log_file.write("-" * 50 + "\n")
    
    def check_distribution(self, data):
        """
        Checks the distribution of the data and identifies the closest matching distribution
        along with its parameters.
        """
        log_data = np.log(data)

        # Define candidate distributions to test
        candidate_distributions = {
            "normal": stats.norm,
            "lognormal": stats.lognorm,
            "exponential": stats.expon,
            "gamma": stats.gamma,
            "weibull": stats.weibull_min
        }

        best_fit = None
        best_params = None
        best_ks_stat = float("inf")

        # Test each distribution
        for name, distribution in candidate_distributions.items():
            try:
                # Fit the distribution to the data
                params = distribution.fit(data)#log_data if name == "lognormal" else data)
                
                # Perform the Kolmogorov-Smirnov test
                ks_stat, _ = stats.kstest(data, distribution.cdf, args=params)
                
                # Keep track of the best fit
                if ks_stat < best_ks_stat:
                    best_ks_stat = ks_stat
                    best_fit = name
                    best_params = params
            except Exception as e:
                # Skip distributions that fail to fit
                print(f"Failed to fit {name}: {e}")
        print(f"Best fit distribution: {best_fit} with parameters: {best_params}")        
        return best_fit, best_params