from models.abstract.model import Model
from modules.factory.Operation import Operation
import numpy as np

class BasicModel(Model):
    """
    Model using the opertion fix times / no changes
    """

    def __init__(self):
        super().__init__()
    
    def initialize(self):
        return super().initialize()

    def get_new_duration(self, operation: Operation) -> int:
        return np.float64(operation.duration)

    def sample(self, model) -> list:
        pass

    def inference(self, operation: Operation, current_tool, do_calculus) -> tuple[int, list[tuple]]:
        return self.get_new_duration(operation), None

