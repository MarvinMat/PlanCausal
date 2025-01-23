from models.abstract.model import Model
from modules.factory.Operation import Operation

class AverageModel(Model):

    def __init__(self, data):
        self.avg_delay = data['delay'].mean() 
        pass
    
    def inference(self, operation: Operation) -> int:
        return operation.duration * self.avg_delay

