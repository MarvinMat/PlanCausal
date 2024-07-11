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
        #self.resource = simpy.PriorityResource(env, capacity=1)
        self.resource = simpy.Resource(env, capacity=1)

    def process(self, order, output, results):
        start_time = self.env.now
        planned_process_time = np.random.normal(self.process_time_mean, self.process_time_std)
        actual_process_time = max(0, planned_process_time + np.random.normal(0, 0.1 * planned_process_time))
        print(f"Order {order['OrderID']} processing for {actual_process_time:.2f} time units on Machine {self.machine_id}")
        yield self.env.timeout(actual_process_time)
        end_time = self.env.now
        result = {
            "OrderID": order["OrderID"],
            "MachineID": self.machine_id,
            "StartTime": start_time,
            "EndTime": end_time,
            "PlannedDuration": planned_process_time,
            "ActualDuration": actual_process_time
        }
        results.append(result)


def print_stats(res):
    print(f'{res.count} of {res.capacity} slots are allocated.')
    print(f'  Users: {res.users}')
    print(f'  Queued events: {res.queue}')

class ProductionProcess:
    def __init__(self, env, machines, output, results):
        self.env = env
        self.machines = machines
        self.output = output
        self.results = results

    def process_order(self, order):
        machine = self.machines[order['PreferredMachine'] - 1]
        print(f"Order {order['OrderID']} requesting Machine {machine.machine_id} with priority {order['Priority']}")
        with machine.resource.request() as request: #priority=float(order['Priority'])
            print_stats(machine.resource)
            yield request
            print_stats(machine.resource)
            print(f"Order {order['OrderID']} granted Machine {machine.machine_id}")
            process = self.env.process(machine.process(order, self.output, self.results))
            yield process
            print(f"Order {order['OrderID']} completed processing on Machine {machine.machine_id}")

def generate_orders(env, production_process, interarrival_time_mean, interarrival_time_std, orders):
    print("Generating orders...")
    for order in orders:
        interarrival_time = np.random.normal(interarrival_time_mean, interarrival_time_std)
        print(f"Order {order['OrderID']} scheduled to arrive in {interarrival_time:.2f} time units.")
        yield env.timeout(interarrival_time)
        print(f"Order {order['OrderID']} now being processed.")
        env.process(production_process.process_order(order))

def run_simulation(machines, interarrival_time_mean, interarrival_time_std, orders, output):
    env = simpy.Environment()
    results = []
    for machine in machines:
        machine.env = env
    production_process = ProductionProcess(env, machines, output, results)
    env.process(generate_orders(env, production_process, interarrival_time_mean, interarrival_time_std, orders))
    env.run()  # Removed the 'until=order_process' to let it run indefinitely
    print("Simulation started")
    output_results(results)
    print("Simulation ended")


def output_results(results):
    print("Outputting results...")  # Debug print
    if results:
        df = pd.DataFrame(results)
        df.to_csv("simulation_results.csv", index=False)
        print("Results written to simulation_results.csv")
    else:
        print("No data to write.")


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
        for i in range(1)
    ]

    interarrival_time_mean = 7.0
    interarrival_time_std = 2.0

    run_simulation(machines, interarrival_time_mean, interarrival_time_std, orders, output)
    print(output.getvalue())
