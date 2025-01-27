from models.abstract.model import Model
from modules.factory.Operation import Operation

class BasicModel(Model):
    """
    Model using the opertion fix times
    """

    def __init__(self):
        super().__init__()
    
    def initialize(self):
        return super().initialize()

    def get_new_duration(self, operation: Operation) -> int:
        return operation.duration

    def sample(self, model) -> list:
        pass

    def inference(self, operation: Operation) -> tuple[int, list[tuple]]:
        return self.get_new_duration(operation), None

