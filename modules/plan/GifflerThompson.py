import heapq
import pandas as pd
import numpy as np
from modules.factory.Operation import Operation
from modules.plan.PriorityRules import get_priority

class GifflerThompson:
    """GT with
    priority rule = func(args...) # implement required (spt, mdd, spr...)
    inference = func(task) # inference module to predict times default inference is = """
    def __init__(self, rule_name, inference, do_calculus = False):
        self.rule_name = rule_name
        self.inference = inference
        self.do_calculus = do_calculus
        self.observed_data = []
        self.schedule = []
        self.qlength = []

    def update_priorities(self, ready_operations, available_times):
        temp_heap = []
        while ready_operations:
            _,_, operation = heapq.heappop(ready_operations)
            selected_machine_idx = 0
            operation.plan_machine_id = str(operation.req_machine_group_id) + '_' + str(selected_machine_idx)
            inference_tool = available_times[operation.req_machine_group_id][0][1]
            inference_duration, inferenced_variables = self.inference(operation, inference_tool, self.do_calculus) 
            new_priority = get_priority(operation=operation,rule_name=self.rule_name, infered_operation_duration=inference_duration)
            #new_priority = get_priority(operation=operation,rule_name=self.rule_name)
            heapq.heappush(temp_heap, (new_priority, (str(operation.job_id) + str(operation.operation_id)), operation))
        return temp_heap

    def create_schedule(self, operations, machine_pools):
        ready_operations = []
        inserted_operations = set()
        # [available time, setup]
        machine_available_time = {machine: [[0, None]] * qty for machine, qty, _ in machine_pools}

        # Dictionary zur Verwaltung der Operation-Objekte über (job_id, operation_id)
        operation_dict = {(operation.job_id, operation.operation_id): operation for operation in operations}

        # Setze die Referenzen zu Successor und Predecessor Operations
        for operation in operations:
            if operation.successor != -1:
                next_operation = operation_dict[(operation.job_id, operation.successor)]
                operation.successor_operation = next_operation
                next_operation.predecessor_operations.append(operation)
            if not operation.predecessor_operations:
                #priority = get_priority(operation=operation,rule_name=self.rule_name, infered_operation_duration=operation.duration)
                heapq.heappush(ready_operations, (0, (str(operation.job_id) + str(operation.operation_id)), operation))
                inserted_operations.add(operation)
        n = 0
        while ready_operations:

            # Aktualisiere alle Prioritäten in der ready_operations Heap
            ready_operations = self.update_priorities(ready_operations, machine_available_time)
            # !Potentielle optimierung: kann bei Statischen Prioritäten übersprungen werden, 
            # kann übersprungen werden wenn die Zeit nicht vorrangeschritten ist.
            _, _, current_operation = heapq.heappop(ready_operations)

            # Überprüfe die Maschinenverfügbarkeit
            machine = current_operation.req_machine_group_id
            available_times = machine_available_time[machine]
            earliest_start_time = max(available_times[0][0], current_operation.plan_start if current_operation.plan_start is not None else 0)
            selected_machine_idx = 0

            for i in range(1, len(available_times)):
                if available_times[i][0] < earliest_start_time:
                    earliest_start_time = max(available_times[i][0], current_operation.plan_start if current_operation.plan_start is not None else 0)
                    selected_machine_idx = i
            #ready_count_req_machine = len([op for op in ready_operations if op[2].req_machine_group_id == current_operation.req_machine_group_id])
            if current_operation.successor != -1:
                qleng = len([op for op in self.schedule if current_operation.successor_operation.req_machine_group_id == op.req_machine_group_id and earliest_start_time < op.plan_start])
                self.qlength.append([n, current_operation.successor_operation.req_machine_group_id, qleng])
                n = n + 1
            current_operation.plan_machine_id = str(current_operation.req_machine_group_id) + '_' + str(selected_machine_idx)
            current_tool = available_times[selected_machine_idx][1]
            current_duration, inferenced_variables = self.inference(current_operation, current_tool, self.do_calculus)  
            self.observed_data.append(inferenced_variables)
            if current_duration is None or not isinstance(current_duration, (float, np.float64)) or current_duration <= 0:
                print(f"Invalid duration {operation.duration} for operation {current_operation.job_id}_{current_operation.operation_id}: {current_duration}")
                raise ValueError("Invalid value for new_duration. It must be a positive float.")
            end_time = earliest_start_time + current_duration
            current_operation.plan_duration = current_duration
            current_operation.plan_start = earliest_start_time
            current_operation.plan_end = end_time
            available_times[selected_machine_idx] = [end_time, current_operation.tool]

            # Aktualisiere die geplante Startzeit für die Nachfolgeaufgaben
            if current_operation.successor_operation:
                successor = current_operation.successor_operation
                # Überprüfe, ob alle Vorgängeraufgaben erledigt sind
                if all(pred.plan_end is not None for pred in successor.predecessor_operations):
                    earliest_start = max(pred.plan_end for pred in successor.predecessor_operations)
                    successor.plan_start = max(earliest_start, end_time)
                    # Füge die Nachfolgeaufgabe zur Heap-Warteschlange hinzu
                    #successor.plan_machine_id = str(successor.req_machine_group_id) + '_' + str(selected_machine_idx)
                    #inference_tool = available_times[selected_machine_idx][1]
                    #inference_duration, inferenced_variables = self.inference(successor, inference_tool, self.do_calculus) 
                    #priority = get_priority(operation=successor,rule_name=self.rule_name, infered_operation_duration=inference_duration)
                    heapq.heappush(ready_operations, (0, (str(current_operation.job_id) + str(current_operation.operation_id)), successor))
                    inserted_operations.add(successor)

            # Füge die Aufgabe zur Zeitplanung hinzu
            self.schedule.append(current_operation)

        self.write_data()
        return self.schedule
    
    def write_data(self):
        df_observed_data = pd.DataFrame(self.observed_data)
        return df_observed_data.to_csv(f'./data/data_oberserve_{self.inference.__func__.__qualname__}.csv')
