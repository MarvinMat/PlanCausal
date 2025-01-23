# inference.py
from abc import ABC, abstractmethod
from modules.factory.Operation import Operation

class Model(ABC):
    """
    Base class for all inference models.
    """
    def __init__(self, model):
        self.model = model
    
    @abstractmethod
    def sample(self, model) -> list:
        pass

    @abstractmethod
    def inference(self) -> int:
        pass

    @abstractmethod
    def infer_duration(self, operation: Operation) -> list:
        pass