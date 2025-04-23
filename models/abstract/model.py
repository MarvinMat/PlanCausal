# inference.py
from abc import ABC, abstractmethod
from modules.factory.Operation import Operation
from modules.logger import Logger
import random
import numpy as np
import logging

class Model(ABC):
    """
    Base class for all inference models.
    """
    def __init__(self, seed=None):
        self.seed = seed
        self.logger = Logger.get_global_logger(category="Model", level=logging.DEBUG, log_to_file=True, log_filename="output/logs/app.log")

    
    @abstractmethod
    def initialize(self):
        """
        Create the model if required
        """
        if self.seed is not None:  # Only set the seed if provided
            random.seed(self.seed )
            np.random.seed(self.seed)
        
    @abstractmethod
    def inference(self, operation: Operation, current_tool, do_calculus) -> tuple[int, list[tuple]]:
        """
        Inference method for planning
        
        Parameter 1: the new duration based on the delay
        Parameter 2: the influencing variables, important to build the observed data
        """
        pass