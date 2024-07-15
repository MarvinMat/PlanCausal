"""
Simpy simulation for Causal Inferencing
"""
from factory.Machine import Machine
from simulation.Wrapper import patch_resource
from simulation.Monitoring.BasicMonitor import monitorResource
import simpy

class Simulator:
    """ Simulator to any given schedule
    machines = [[id, name, tools]] machine array with [id, name , array_tools]
    schedule = [Operation], array containing all scheduled operations
    monitor_data = resource monitor [0] pre , [1] post monitor
    inference = func(operation) returning an int, as operation Duration 
    """
    def __init__(self, machines, schedule, monitor_data, inference):
        self.schedule = schedule
        self.machines = machines
        self.inference = inference
        self.pre_resource_monitor = monitor_data[0]
        self.post_resource_monitor =  monitor_data[1]
        self.env = simpy.Environment()
        self.pools = self.build_pools(machines)
        self.build_jobs()

    def operation(self, operation, precedent_tasks):
        """
        hart of the processing
        --- workflow
        waits for planned start
        waits for the completions of pressidenct operation (list can be empty)
        grabs a resouce
        spend some time doing the operation
        """
        plan_start = operation.plan_start if operation.plan_start is not None else 0 
        print(plan_start)
        delay =  plan_start - self.env.now 
        #print(f'{env.now}, job: {job_id}, task_id: {task_id}, created; waiting for start {delay}')    
        yield self.env.timeout(delay)

        #print(f'{env.now}, job: {job_id}, task_id: {task_id}, waiting for presedents')
        
        yield self.env.all_of(precedent_tasks)

        print(f'{self.env.now}, job: {operation.job_id}, operation_id: {operation.operation_id}, getting resource')
        with operation.machine.request() as req:
            
            yield req

            print(f'{self.env.now}, job: {operation.job_id}, operation_id: {operation.operation_id}, starting operation')
            operation.machine.current_operation = operation

            yield self.env.timeout(self.inference(operation)) # durchführung

        print(f'{self.env.now}, job: {operation.job_id}, operation_id: {operation.operation_id}, finished operation')


    def build_pools(self, pool_data):
        """
        builds a dict of resouces pools from data

        index 0: name of machine type
        index 1: number of machines in the pool
        index 3: tools
        """
        pools = {}       
        for pool in pool_data:
            for idx in range(0, pool[1]):
                id = str(pool[0]) + '_' + str(idx)
                pools[id] = Machine(id=id, name=id, tools=pool[2], env=self.env)
                patch_resource(pools[id], post=self.post_resource_monitor)  # Patches (only) this resource instance
        return pools

    def get_machine(self, plan_machine_id):
        """
        matches planed machine with simulation resource
        """
        for m in self.pools:
            if m == plan_machine_id:
                return self.pools[m]

    def build_jobs(self):
        """
        creates operations for each end product 
        """
        operations_without_successor = [operation for operation in self.schedule if operation.successor == -1]
        for op in operations_without_successor:
            self.build_operations(op)

    def build_operations(self, operation):
        """
        recurse down the pressidents and work from the
        leafs back creating operations, which are used as
        pressident events for the parent node.
        """

        pred_operations = []
        # recurse the pressidents to get task processes that
        # this node can use to wait on.
        if len(operation.predecessor_operations) > 0:
            for pred_node in operation.predecessor_operations:
                pred_operations.append(self.build_operations(pred_node))

        
        operation.machine = self.get_machine(operation.plan_machine_id)

        # create the task process
        t = self.operation(operation, pred_operations)

        # retrun the process to the parent, which the parent
        # will wait on as a pressident
        t = self.env.process(t)

        return t
