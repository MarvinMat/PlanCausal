from sklearn.neighbors import KernelDensity
import numpy as np
from models.abstract.distribution import DistributionModel
from modules.factory.Operation import Operation
from models.abstract.model import Model 

class KDEDistributionModel(DistributionModel):
    """
    A distribution model assuming Kernel Density Estimation (KDE) for durations.
    """
    def __init__(self, csv_file, seed, bandwidth=0.2):
        super().__init__(csv_file, seed=seed)
        self.bandwidth = bandwidth
        self.kde_models = {}  # Store KDE models per (product_type, operation_id)

    def initialize(self):
        """
        Create the model for each unique operation type.
        """
        super(DistributionModel, self).initialize()
        self.data = self.read_from_csv()

        unique_operations = self.data.groupby(['product_type', 'operation_id'])

        for (product_type, operation_id), group in unique_operations:
            values = group['duration'].values  # Extract durations

            if len(values) > 1:  # Avoid fitting on single data points
                kde = KernelDensity(bandwidth=self.bandwidth)
                kde.fit(values[:, None])  # Fit KDE
                self.kde_models[(product_type, operation_id)] = kde  # Store model
            else:
                self.kde_models[(product_type, operation_id)] = None  # Not enough data

    def inference(self, operation: Operation):
        """
        Samples from the fitted KDE distribution for a given operation.
        """
        key = (operation.product_type, operation.operation_id)

        if key in self.kde_models and self.kde_models[key] is not None:
            kde = self.kde_models[key]
            samples = kde.sample(1)  # Generate a sample
            return samples[0, 0], key  # Return a single value
        else:
            return None  # Handle missing distributions
