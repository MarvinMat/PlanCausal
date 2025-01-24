import heapq
from modules.factory.Operation import Operation

class GifflerThompson:
    """GT with
    priority rule = func(args...) # implement required (spt, mdd, spr...)
    inference = func(task) # inference module to predict times default inference is = """
    def __init__(self, priority_rule, inference):
        self.priority_rule = priority_rule
        self.inference = inference
        self.schedule = []
        self.qlength = []

    def update_priorities(self, ready_operations):
        temp_heap = []
        while ready_operations:
            _,_, operation = heapq.heappop(ready_operations)
            new_priority = self.priority_rule(operation)
            heapq.heappush(temp_heap, (new_priority, (str(operation.job_id) + str(operation.operation_id)), operation))
        return temp_heap

    def giffen_thompson(self, operations, machine_pools):
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
                priority = self.priority_rule(operation)
                heapq.heappush(ready_operations, (priority, (str(operation.job_id) + str(operation.operation_id)), operation))
                inserted_operations.add(operation)
        n = 0
        while ready_operations:

            # Aktualisiere alle Prioritäten in der ready_operations Heap
            ready_operations = self.update_priorities(ready_operations)
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
            current_duration, _ = self.inference(current_operation)#, available_times[selected_machine_idx][1])    
            end_time = earliest_start_time + current_duration
            current_operation.plan_duration = current_duration
            current_operation.plan_start = earliest_start_time
            current_operation.plan_end = end_time
            current_operation.plan_machine_id = str(current_operation.req_machine_group_id) + '_' + str(selected_machine_idx)
            available_times[selected_machine_idx] = [end_time, current_operation.tool]

            # Aktualisiere die geplante Startzeit für die Nachfolgeaufgaben
            if current_operation.successor_operation:
                successor = current_operation.successor_operation
                # Überprüfe, ob alle Vorgängeraufgaben erledigt sind
                if all(pred.plan_end is not None for pred in successor.predecessor_operations):
                    earliest_start = max(pred.plan_end for pred in successor.predecessor_operations)
                    successor.plan_start = max(earliest_start, end_time)
                    # Füge die Nachfolgeaufgabe zur Heap-Warteschlange hinzu
                    priority = self.priority_rule(successor)
                    heapq.heappush(ready_operations, (priority, (str(current_operation.job_id) + str(current_operation.operation_id)), successor))
                    inserted_operations.add(successor)

            # Füge die Aufgabe zur Zeitplanung hinzu
            self.schedule.append(current_operation)

        return self.schedule
