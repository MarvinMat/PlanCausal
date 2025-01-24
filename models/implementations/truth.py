import pandas as pd
import numpy as np
from models.abstract.pgmpy import PGMPYModel
from models.abstract.model import Model
from modules.factory.Operation import Operation
from pgmpy.models import BayesianNetwork
from pgmpy.factors.discrete import TabularCPD

class TruthModel(PGMPYModel):
    def __init__(self):    
        self.model = self.create_model()
        super().__init__()   
        
    
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
            
    def read_from_pre_xlsx(self, file):
        data = pd.read_excel(file)
        data.drop(columns=data.columns[0], axis=1, inplace=True)
        return data
    
    def create_model(self):
        print("Set edges by user")
        # Defining the Bayesian network structure manually
        model = BayesianNetwork([
            ('previous_machine_pause', 'machine_status'),
            ('machine_status', 'delay'),
            ('machine_status', 'pre_processing'),
            ('pre_processing', 'delay')
        ])
        
        # Define CPDs
        # CPD for 'previous_machine_pause' (root node, no parents)
        cpd_previous_machine_pause = TabularCPD(
            variable='previous_machine_pause', 
            variable_card=2,  # 2 states: 0 and 1
            values=[[0.8], [0.2]]  # P(previous_machine_pause=0)=0.8, P(previous_machine_pause=1)=0.2
        )
        
        # CPD for 'machine_status' (dependent on 'previous_machine_pause')
        cpd_machine_status = TabularCPD(
            variable='machine_status', 
            variable_card=2,  # 2 states: 0 and 1
            values=[
                [0.9, 0.3],  # P(machine_status=0 | previous_machine_pause)
                [0.1, 0.7]   # P(machine_status=1 | previous_machine_pause)
            ],
            evidence=['previous_machine_pause'],  # Parent node
            evidence_card=[2]  # Number of states of parent
        )
        
        # CPD for 'pre_processing' (dependent on 'machine_status')
        cpd_pre_processing = TabularCPD(
            variable='pre_processing', 
            variable_card=2,  # 2 states: 0 and 1
            values=[
                [0.6, 0.4],  # P(pre_processing=0 | machine_status)
                [0.4, 0.6]   # P(pre_processing=1 | machine_status)
            ],
            evidence=['machine_status'],  # Parent node
            evidence_card=[2]  # Number of states of parent
        )
        
        # CPD for 'delay' (dependent on 'machine_status' and 'pre_processing')
        cpd_delay = TabularCPD(
            variable='delay', 
            variable_card=2,  # 2 states: 1.0 and 1.2
            values=[
                [0.7, 0.5, 0.4, 0.2],  # P(delay=1.0 | machine_status, pre_processing)
                [0.3, 0.5, 0.6, 0.8]   # P(delay=1.2 | machine_status, pre_processing)
            ],
            evidence=['machine_status', 'pre_processing'],  # Parent nodes
            evidence_card=[2, 2]  # Number of states for each parent
        )
        
        # Add CPDs to the model
        model.add_cpds(
            cpd_previous_machine_pause, 
            cpd_machine_status, 
            cpd_pre_processing, 
            cpd_delay
        )
        
        # Validate the model (ensures the structure and CPDs are consistent)
        model.check_model()
        
        return model
    
    def get_new_duration(self, operation: Operation, inferenced_variables) -> int:
        new_duration = round(operation.duration * inferenced_variables['delay'],0)
        return new_duration

    def inference(self, operation: Operation) -> list:
        # Beispielaufruf mit CSV-Datei (Dateipfad anpassen)

        if operation.machine is not None:
            previous_machine_pause =  operation.tool != operation.machine.current_tool
        else:
            previous_machine_pause = True
            
        evidence = {
            'previous_machine_pause': previous_machine_pause
            # Weitere Evidenzen können hier hinzugefügt werden, falls nötig
        }

        # Inferenz durchführen
        result = self.sample(evidence=evidence)

        # Variablen für delay, machine_status und pre_processing initialisieren
        has_delay = False
        machine_status = None
        pre_processing = None

        # Sampling für die delay-Variable
        if 'delay' in result:
            delay_values = result['delay'].values
            if len(delay_values) == 2:
                # Wahrscheinlichkeiten extrahieren
                delay_probabilities = delay_values / delay_values.sum()  # Normalisieren
                # Zustand für delay basierend auf den Wahrscheinlichkeiten würfeln
                has_delay = np.random.choice([0, 1], p=delay_probabilities)
        
        # Sampling für die machine_status-Variable
        if 'machine_status' in result:
            machine_status_values = result['machine_status'].values
            machine_status_probabilities = machine_status_values / machine_status_values.sum()  # Normalisieren
            machine_status = np.random.choice([0, 1], p=machine_status_probabilities)
        
        # Sampling für die pre_processing-Variable
        if 'pre_processing' in result:
            pre_processing_values = result['pre_processing'].values
            pre_processing_probabilities = pre_processing_values / pre_processing_values.sum()  # Normalisieren
            pre_processing = np.random.choice([0, 1], p=pre_processing_probabilities)
        
        # Berechnung des Multiplikators
        delay = 1.2 if has_delay else 1.0

        inferenced_variables = {
            'previous_machine_pause': previous_machine_pause,
            'delay': delay,
            'machine_status': machine_status,
            'pre_processing': pre_processing
        }
        
        return self.get_new_duration(operation, inferenced_variables), inferenced_variables
