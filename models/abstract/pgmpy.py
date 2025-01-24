# inference.py
from modules.factory.Operation import Operation
from models.abstract.model import Model
from pgmpy.inference import VariableElimination, CausalInference, BeliefPropagation

class PGMPYModel(Model):
    """
    Base class for all inference models.
    """
    def __init__(self):
        super().__init__()
        
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
        
        if not variable:
            all_model_variables = self.model.nodes()
        else:
            all_model_variables = variable

        result = {}     

        # Reguläre Inferenz ohne "do"-Intervention
        if not any(do):
            for variable in all_model_variables:
                if variable not in evidence:
                    # Nur Variablen abfragen, die nicht in der Evidenz enthalten sind
                    query_result = self.variable_elemination.query(variables=[variable], evidence=evidence, joint=True)
                    result[variable] = query_result

        # Kausale Inferenz (do-Intervention)
        else:
            #print(f"Durchführung einer 'do'-Intervention: Setze 'pre_processing' auf {do}")
            for variable in all_model_variables:
                if variable not in evidence:
                    do_result = self.causal_inference.query(variables=[variable], do=do, joint=True)
                    result[variable] = do_result

        return result
