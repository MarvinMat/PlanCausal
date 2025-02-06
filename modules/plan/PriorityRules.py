from modules.factory.Operation import Operation
import re

# Define a priority rule
def calculate_dynamic_priority(operation) -> int:
    # Priorität basierend auf der geplanten Startzeit der Vorgängeraufgaben
    if not operation.predecessor_operations:
        return operation.plan_start if operation.plan_start is not None else 0
    else:
        return max(pred.plan_start for pred in operation.predecessor_operations) + operation.duration

# Define a priority rule
def calculate_fcfs_priority(operation: Operation) -> int:
     """
     Calculate the First-Come-First-Serve (FCFS) priority for the given operation.
     
     Parameters:
     operation (Operation): The operation object which contains information like planned start time.
     
     Returns:
     int: The priority value based on the FCFS rule (lower value means higher priority).
     """
     # Use the planned start time as the FCFS priority, assuming earlier times have higher priority.
     # If plan_start is None, return a large number to deprioritize unscheduled operations.
     return int(re.sub(r"\D", "", operation.job_id) + str(operation.operation_id))
     #return operation.plan_start if operation.plan_start is not None else float('inf')
     
# Mapping of priority rules
priority_rules = {
    "dynamic": calculate_dynamic_priority,
    "fcfs": calculate_fcfs_priority
}

def get_priority(operation, rule_name):
    rule_function = priority_rules.get(rule_name)
    if rule_function is not None:
        return rule_function(operation)
    else:
        raise ValueError(f"Unknown rule: {rule_name}")