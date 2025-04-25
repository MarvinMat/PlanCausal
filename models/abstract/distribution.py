import pandas as pd
import os
from models.abstract.model import Model
from modules.factory.Operation import Operation
import numpy as np

class DistributionModel(Model):
    """
    Base class for all distribution inference models.
    """
    def __init__(self, csv_file, seed=None):
        super().__init__(seed=seed)
        self.csv_file = csv_file
        self.data = None
        self.distribution_dict = {}

    def initialize(self):
        """
        Reads the CSV and fits the distributions for each unique operation.
        """
        super().initialize()
        self.data = self.read_from_csv()

        # Group by product_type and operation_id
        unique_operations = self.data.groupby(['product_type', 'operation_id'])

        for (product_type, operation_id), group in unique_operations:
            values = group['duration'].values  # Extract relevant values
            
            if len(values) > 1:  # Avoid fitting on single data points
                params = self.fit(values)  # Fit using the specific distribution model
                self.distribution_dict[(product_type, operation_id)] = params
            else:
                self.distribution_dict[(product_type, operation_id)] = None  # Not enough data
        
    def read_from_csv(self):
        """
        Reads the dataset from CSV.
        """
        if not self.csv_file or not os.path.exists(self.csv_file):
            raise FileExistsError(f"File not found: {self.csv_file}.")

        try:
            return pd.read_csv(self.csv_file)
        except Exception as e:
            raise ImportError(f"Error reading file {self.csv_file}: {e}")

    def fit(self, data):
        """
        Abstract method for fitting a distribution to the data.
        """
        raise NotImplementedError("This method must be implemented in derived classes.")

    def sample(self, params):
        """
        Abstract method for sampling from a fitted distribution.
        """
        raise NotImplementedError("This method must be implemented in derived classes.")

    def inference(self, operation: Operation, current_tool, do_calculus) -> tuple[int, list[tuple]]:
        """
        Perform inference by sampling from the fitted distribution.
        """
        key = (operation.product_type, operation.operation_id)

        if key in self.distribution_dict and self.distribution_dict[key] is not None:
            params = self.distribution_dict[key]
            return round(self.sample(params), 0), key  # Call sample method to draw from distribution
        else:
            return np.float64(operation.duration), key  # No data available for inference
