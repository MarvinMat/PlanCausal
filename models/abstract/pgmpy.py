# inference.py
from models.abstract.model import Model
from pgmpy.inference import VariableElimination, CausalInference, BeliefPropagation

class PGMPYModel(Model):
    """
    Base class for all inference models.
    """
    def __init__(self, seed = None):
        super().__init__(seed=seed)
        
    def initialize(self):
        # Inference-Objekte für reguläre Inferenz
        self.variable_elemination = VariableElimination(self.model)
        self.belief_propagation = BeliefPropagation(self.model)

        # CausalInference-Objekt für kausale Abfragen (do-Operator)
        self.causal_inference = CausalInference(self.model)
        return super().initialize()

    def inference(self) -> tuple[int, list[tuple]]:
        raise NotImplementedError("This method must be implemented in derived classes.")
    
    def sample(self, variable={}, evidence={}, do={}) -> list:
        """
        Führt eine Inferenz auf dem gelernten Modell durch.
        """
        if not self.model:
            raise ValueError("No model for inference.")
        
        #if not variable:
            #all_model_variables = self.model.nodes()
        #else:
            #all_model_variables = variable

        result = {}
        
        # Get all variables in the model
        all_model_variables = set(self.model.nodes())

        # Remove any variables that are in `evidence` or `do`
        query_variables = list(all_model_variables - set(evidence) - set(do))
        
        # Regular Inference (No do-intervention)
        if not any(do):
            for variable in query_variables:
                
                query_result = self.variable_elemination.query(
                    variables=[variable], 
                    evidence=evidence, 
                    joint=True, 
                    show_progress=False
                )
                result[variable] = query_result

        # Causal Inference (do-Intervention)
        else:
            for variable in query_variables:
                do_result = self.causal_inference.query(
                    variables=[variable], 
                    evidence=evidence, 
                    adjustment_set=set(do), 
                    do=do, joint=True, 
                    inference_algo="ve", 
                    show_progress=False
                )
                result[variable] = do_result

        return result

