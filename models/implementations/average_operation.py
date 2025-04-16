from models.abstract.model import Model
from modules.factory.Operation import Operation
import pandas as pd

class AverageOperationModel(Model):

    def __init__(self, csv_file):
        super().__init__()
        self.csv_file = csv_file
        self.operation_dict = {}
        
    def initialize(self):
        data = pd.read_csv(self.csv_file)
        
        # Group by product_type and operation_id
        unique_operations = data.groupby(['product_type', 'operation_id'])

        for (product_type, operation_id), group in unique_operations:
            values = group['duration'].values 
            avg_operation_deviation = values.mean()
            self.operation_dict[(product_type, operation_id)] = avg_operation_deviation
            
        return
    
    def get_new_duration(self, operation: Operation) -> int:
        key = (operation.product_type, operation.operation_id)

        if key in self.operation_dict and self.operation_dict[key] is not None:
            avg_operation_deviation = self.operation_dict[key]
        else:
            return None  # No data available for inference
        return round(avg_operation_deviation, 0)

    def inference(self, operation: Operation, current_tool) -> tuple[int, list[tuple]]:      
        return self.get_new_duration(operation), None
