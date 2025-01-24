from models.abstract.model import Model
from modules.factory.Operation import Operation
import pandas as pd

class AverageModel(Model):

    def __init__(self, csv_file):
        super().__init__()
        data = pd.read_csv(csv_file)
        self.avg_delay = data['delay'].mean() 
         
    def get_new_duration(self, operation: Operation) -> int:
        return operation.duration * self.avg_delay

    def inference(self, operation: Operation) -> tuple[int, list[tuple]]:
        return self.get_new_duration(operation), None
