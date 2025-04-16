from models.abstract.model import Model
from modules.factory.Operation import Operation
import pandas as pd

class AverageModel(Model):

    #TODO Use Feedback data instead of oberseved data
    def __init__(self, csv_file):
        super().__init__()
        self.csv_file = csv_file
        
    def initialize(self):
        data = pd.read_csv(self.csv_file)
        self.avg_delay = data['relative_processing_time_deviation'].mean()
        return
    
    def get_new_duration(self, operation: Operation) -> int:
        return operation.duration * self.avg_delay

    def inference(self, operation: Operation) -> tuple[int, list[tuple]]:
        return self.get_new_duration(operation), None
