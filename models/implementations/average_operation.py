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
        
        # Group by product_type and operation_ids
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
            avg_operation_deviation = self.operation.duration
            #raise ValueError(f"Operation {key} not found in the operation dictionary.")
            #return None  # No data available for inference        
        return round(avg_operation_deviation, 0)

    def inference(self, operation: Operation, current_tool, do_calculus) -> tuple[int, list[tuple]]:      
        new_duration = self.get_new_duration(operation)
        if new_duration is None:
            raise ValueError(f"Operation {new_duration} not found in the operation dictionary.")
        return new_duration, None
