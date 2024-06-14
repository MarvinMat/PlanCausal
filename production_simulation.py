import simpy
import pandas as pd
import numpy as np
from io import StringIO

class Machine:
    def __init__(self, env, machine_id, process_time_mean, process_time_std):
        self.env = env
        self.machine_id = machine_id
        self.process_time_mean = process_time_mean
        self.process_time_std = process_time_std
        self.resource = simpy.PriorityResource(env, capacity=1)

    def process(self, order, output):
        process_time = np.random.normal(self.process_time_mean, self.process_time_std)
        yield self.env.timeout(process_time)
        output.write(f'Order {order["OrderID"]} processed by Machine {self.machine_id} in {process_time:.2f} units of time.\n')

class ProductionProcess:
    def __init__(self, env, machines, output):
        self.env = env
        self.machines = machines
        self.output = output

    def process_order(self, order):
        machine = self.machines[order['PreferredMachine'] - 1]
        with machine.resource.request(priority=order['Priority']) as request:
            yield request
            yield self.env.process(machine.process(order, self.output))

def generate_orders(env, production_process, interarrival_time_mean, interarrival_time_std, orders):
    for order in orders:
        yield env.timeout(np.random.normal(interarrival_time_mean, interarrival_time_std))
        env.process(production_process.process_order(order))

def run_simulation(machines, interarrival_time_mean, interarrival_time_std, orders, output):
    env = simpy.Environment()
    for machine in machines:
        machine.env = env
    production_process = ProductionProcess(env, machines, output)
    env.process(generate_orders(env, production_process, interarrival_time_mean, interarrival_time_std, orders))
    env.run()

# Beispiel für direkte Ausführung
if __name__ == "__main__":
    output = StringIO()
    env = simpy.Environment()
    machines = [
        Machine(env=env, machine_id=1, process_time_mean=5.0, process_time_std=1.0),
        Machine(env=env, machine_id=2, process_time_mean=6.0, process_time_std=1.5)
    ]

    orders = [
        {'OrderID': i, 'Priority': np.random.randint(1, 3), 'PreferredMachine': np.random.choice([1, 2])}
        for i in range(20)
    ]

    interarrival_time_mean = 7.0
    interarrival_time_std = 2.0

    run_simulation(machines, interarrival_time_mean, interarrival_time_std, orders, output)
    print(output.getvalue())
