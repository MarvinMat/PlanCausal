import heapq

class HodgsonMooreAlgorithm:
    def __init__(self):
        self.schedule = []

    def remaining_time(self, operation):
        time = operation.duration
        next_op = operation.successor_operation
        while next_op:
            time += next_op.duration
            next_op = next_op.successor_operation
        return time

    def schedule_jobs(self, operations, machine_pools):
        ready_operations = []
        machine_available_time = {machine: [0] * qty for machine, qty, _ in machine_pools}

        operation_dict = {(op.job_id, op.operation_id): op for op in operations}

        # Initialize the first operations of each job
        for operation in operations:
            if operation.successor != -1:
                next_operation = operation_dict[(operation.job_id, operation.successor)]
                operation.successor_operation = next_operation
                next_operation.predecessor_operations.append(operation)
            if not operation.predecessor_operations:
                # Include the unique memory id of the operation as a tiebreaker
                heapq.heappush(ready_operations, (self.remaining_time(operation), id(operation), operation))

        while ready_operations:
            _, _, current_operation = heapq.heappop(ready_operations)
            machine = current_operation.req_machine_group_id
            available_times = machine_available_time[machine]
            earliest_start_time = min(available_times)
            machine_index = available_times.index(earliest_start_time)

            current_operation.plan_start = earliest_start_time
            current_operation.plan_end = earliest_start_time + current_operation.duration
            machine_available_time[machine][machine_index] = current_operation.plan_end

            if current_operation.successor_operation:
                if all(pred.plan_end is not None for pred in current_operation.successor_operation.predecessor_operations):
                    heapq.heappush(ready_operations, (self.remaining_time(current_operation.successor_operation), id(current_operation.successor_operation), current_operation.successor_operation))

            # Add the operation to the schedule
            self.schedule.append(current_operation)

        return self.schedule