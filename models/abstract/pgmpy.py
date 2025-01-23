# inference.py
from abc import ABC, abstractmethod
from modules.factory.Operation import Operation
from models.abstract.model import Model
from pgmpy.inference import VariableElimination, CausalInference

class PGMPYModel(Model):
    """
    Base class for all inference models.
    """
    def __init__(self, model):
        super().__init__(model)

    def infer_duration(self, operation: Operation) -> list:
        pass

    def sample(self, variable={}, evidence={}, do={}) -> list:
            """
            Führt eine Inferenz auf dem gelernten Modell durch.
            """
            if not self.model:
                raise ValueError("Kein Modell zur Inference vorhanden.")
            
            if not variable:
                all_model_variables = self.model.nodes()
            else:
                all_model_variables = variable
            
            # VariableElimination für reguläre Inferenz
            inference = VariableElimination(self.model)

            # CausalInference-Objekt für kausale Abfragen (do-Operator)
            causal_inference = CausalInference(self.model)

            result = {}     

            # Reguläre Inferenz ohne "do"-Intervention
            if not any(do):
                for variable in all_model_variables:
                    if variable not in evidence:
                        # Nur Variablen abfragen, die nicht in der Evidenz enthalten sind
                        query_result = inference.query(variables=[variable], evidence=evidence, joint=True)
                        result[variable] = query_result

            # Kausale Inferenz (do-Intervention)
                    
            else:
                #print(f"Durchführung einer 'do'-Intervention: Setze 'pre_processing' auf {do}")
                for variable in all_model_variables:
                    if variable not in evidence:
                        do_result = causal_inference.query(variables=[variable], do=do, joint=True)
                        result[variable] = do_result

            return result
    
