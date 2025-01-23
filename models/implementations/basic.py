from models.abstract.model import Model
from modules.factory.Operation import Operation

class BasicModel(Model):

    def __init__(self):
        pass

    def inference(self, operation: Operation) -> int:
        return operation.duration

