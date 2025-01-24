# inference.py
from abc import ABC, abstractmethod
from modules.factory.Operation import Operation
from modules.logger import Logger

class Model(ABC):
    """
    Base class for all inference models.
    """
    def __init__(self):
        pass
    
    @abstractmethod
    def initialize(self):
        """
        Create the model if required
        """
        pass
    
    @abstractmethod
    def inference(self, operation: Operation) -> tuple[int, list[tuple]]:
        """
        Inference method for planning
        
        Parameter 1: the new duration based on the delay
        Parameter 2: the influencing variables, important to build the observed data
        """
        pass