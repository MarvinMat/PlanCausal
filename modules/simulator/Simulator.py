"""
Simpy simulation for Causal Inferencing
"""
from modules.factory.Machine import Machine
from modules.simulator.Wrapper import patch_resource
from modules.simulator.Monitoring.BasicMonitor import monitorResource
from models.abstract.model import Model
import simpy

class Simulator:
    """ Simulator to any given schedule
    machines = [[id, name, tools]] machine array with [id, name , array_tools]
    schedule = [Operation], array containing all scheduled operations
    monitor_data = resource monitor [0] pre , [1] post monitor
    model = includes a inference(operation) returning an int, as operation Duration 
    """
    def __init__(self, machines, schedule, monitor_data, model: Model , oberserved_data_path):
        self.schedule = schedule
        self.oberserved_data_path = oberserved_data_path
        self.observed_data = []
        self.machines = machines
        self.model = model
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
        #print(plan_start)
        delay =  plan_start - self.env.now 
        #print(f'{env.now}, job: {job_id}, task_id: {task_id}, created; waiting for start {delay}')    
        yield self.env.timeout(delay)

        #print(f'{env.now}, job: {job_id}, task_id: {task_id}, waiting for presedents')
        
        yield self.env.all_of(precedent_tasks)

        # TODO: 
        # Yield all prececent operations on resource q

        #print(f'{self.env.now}, job: {operation.job_id}, operation_id: {operation.operation_id}, getting resource')
        with operation.machine.request() as req:
            yield req
            operation.sim_start = self.env.now
            #print(f'{self.env.now}, job: {operation.job_id}, operation_id: {operation.operation_id}, starting operation')
            operation.machine.current_operation = operation
            operation.sim_duration, influenced_variables = self.model.inference(operation)
            self.observed_data.append(influenced_variables)
            operation.machine.current_tool = operation.machine.current_operation.tool
            
            yield self.env.timeout(operation.sim_duration) # durchführung
        
        operation.sim_end = self.env.now
        operation.machine.current_operation = None
        operation.machine.history.append(operation)
        #print(f'{self.env.now}, job: {operation.job_id}, operation_id: {operation.operation_id}, finished operation')


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
                pools[id] = Machine(id=id, group=str(pool[0]), tools=pool[2], env=self.env)
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
