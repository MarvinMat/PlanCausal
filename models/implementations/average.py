from models.abstract.model import Model
from modules.factory.Operation import Operation

class AverageModel(Model):

    def __init__(self):
        self.avg_duration = self.get_avg_duration_from_df(self.observed_data) 
        pass

    def get_avg_duration_from_df(self, data):
        return data['delay'].mean()

    def inference(self, operation: Operation) -> int:
        return operation.duration * self.avg_duration

