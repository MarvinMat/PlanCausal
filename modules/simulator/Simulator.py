"""
Simpy simulation for Causal Inferencing
"""
from modules.factory.Machine import Machine
from modules.simulator.Wrapper import patch_resource
from modules.simulator.Monitoring.BasicMonitor import monitorResource
from models.abstract.model import Model
from modules.logger import Logger
import logging
import pandas as pd
import simpy

class Simulator:
    def __init__(self, machines, schedule, monitor_data, model: Model, oberserved_data_path, planned_mode=True):
        """
        Args:
            machines: Array of machine configurations
            schedule: Array of operations
            monitor_data: Resource monitor data
            model: Model for inference
            oberserved_data_path: Path for observed data output
            planned_mode: If True, uses planned starts and machines. If False, uses dynamic scheduling
        """
        self.schedule = schedule
        self.oberserved_data_path = oberserved_data_path
        self.observed_data = []
        self.machines = machines
        self.model = model
        self.pre_resource_monitor = monitor_data[0]
        self.post_resource_monitor = monitor_data[1]
        self.env = simpy.Environment()
        self.pools = self.build_pools(machines)
        self.planned_mode = planned_mode
        self.machine_groups = self._group_machines_by_type()
        self.build_jobs()
        self.logger = Logger.get_logger(category="Simulation", level=logging.DEBUG, 
                                      log_to_file=False, log_filename="output/logs/simulation.log")

    def _group_machines_by_type(self):
        """Group machines by their type for unplanned mode"""
        groups = {}
        for machine_id, machine in self.pools.items():
            if machine.group not in groups:
                groups[machine.group] = []
            groups[machine.group].append(machine)
        return groups

    def get_next_available_machine(self, machine_group_id):
        """Find next available machine of required type"""
        if machine_group_id not in self.machine_groups:
            return None
        
        machines = self.machine_groups[machine_group_id]
        # Return machine with shortest queue
        return min(machines, key=lambda m: len(m.queue))

    def operation(self, operation, precedent_tasks):
        """Modified operation method to support both modes"""
        # Wait for precedent tasks in both modes
        yield self.env.all_of(precedent_tasks)
        
        if self.planned_mode:
            # Planned mode - use specified start times and machines
            plan_start = operation.plan_start if operation.plan_start is not None else 0
            delay = max(0, plan_start - self.env.now)
            yield self.env.timeout(delay)
            machine = self.get_machine(operation.plan_machine_id)
        else:
            # Unplanned mode - get next available machine of required type
            machine = self.get_next_available_machine(operation.req_machine_group_id)
            if not machine:
                self.logger.error(f"No suitable machine found for operation {operation.operation_id}, "
                                  f"required machine group: {operation.req_machine_group_id}")
                return

        self.logger.debug(f'{self.env.now}, job: {operation.job_id}, operation_id: {operation.operation_id}, getting resource')
        
        with machine.request() as req:
            yield req
            # Rest of the operation logic remains the same
            operation.sim_start = self.env.now
            operation.machine = machine  # Store the actually used machine
            self.logger.debug(f'{self.env.now}, job: {operation.job_id}, operation_id: {operation.operation_id}, starting operation')
            machine.current_operation = operation
            operation.sim_duration, influenced_variables = self.model.inference(operation, machine.current_tool)
            self.observed_data.append(influenced_variables)
            machine.current_tool = operation.tool
            
            yield self.env.timeout(operation.sim_duration)
            
        operation.sim_end = self.env.now
        operation.machine.current_operation = None
        operation.machine.history.append(operation)
        self.logger.debug(f'{self.env.now}, job: {operation.job_id}, operation_id: {operation.operation_id}, finished operation')

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

    def write_data(self):
        df_observed_data = pd.DataFrame(self.observed_data)
        return df_observed_data.to_csv(self.oberserved_data_path)
