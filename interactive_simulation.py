import pandas as pd
import numpy as np
import simpy
from production_simulation import Machine, ProductionProcess, run_simulation

def load_order_data(filename):
    return pd.read_csv(filename)

def main():
    order_data_file = input('Enter the path to the order data file (default is "order_data.csv"): ') or 'order_data.csv'

    try:
        df = load_order_data(order_data_file)
    except FileNotFoundError:
        print(f"File {order_data_file} not found. Please provide a valid file path.")
        return

    env = simpy.Environment()

    machines = [
        Machine(env, machine_id=1, process_time_mean=5.0, process_time_std=1.0),
        Machine(env, machine_id=2, process_time_mean=6.0, process_time_std=1.5)
    ]

    orders = df.to_dict('records')

    interarrival_time_mean = 7.0
    interarrival_time_std = 2.0

    run_simulation(machines, interarrival_time_mean, interarrival_time_std, orders)

if __name__ == "__main__":
    main()
