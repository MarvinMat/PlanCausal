class Operation:
    """Operation with job id, operation id, machine, time, next operation"""
    def __init__(self, job_id, operation_id, machine, duration, succ, plan_start):
        self.job_id = job_id
        self.operation_id = operation_id
        self.machine = machine
        self.duration = duration
        self.successor = succ
        self.plan_start = plan_start
        self.plan_end = None
        self.successor_operation = None
        self.predecessor_operations = []

    def __repr__(self):
        return f"Operation(job_id='{self.job_id}', operation_id={self.operation_id}, machine='{self.machine}', " \
               f"duration={self.duration}, successor={self.successor}, plan_start={self.plan_start})"
