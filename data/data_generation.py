import pandas as pd
import numpy as np

def generate_order_data(num_orders, process_time_mean, process_time_std, interarrival_time_mean, interarrival_time_std):
    data = []
    interarrival_times = np.random.normal(interarrival_time_mean, interarrival_time_std, num_orders)
    arrival_times = np.cumsum(interarrival_times)
    for i in range(num_orders):
        order_id = i
        process_time = np.random.normal(process_time_mean, process_time_std)
        arrival_time = arrival_times[i]
        priority = np.random.randint(1, 3)
        preferred_machine = np.random.choice([1, 2])
        data.append([order_id, arrival_time, process_time, priority, preferred_machine])
    
    df = pd.DataFrame(data, columns=['OrderID', 'ArrivalTime', 'ProcessTime', 'Priority', 'PreferredMachine'])
    return df

num_orders = 100
process_time_mean = 5.0
process_time_std = 1.0
interarrival_time_mean = 7.0
interarrival_time_std = 2.0

df = generate_order_data(num_orders, process_time_mean, process_time_std, interarrival_time_mean, interarrival_time_std)
df.to_csv('order_data.csv', index=False)
print("Order data generated and saved to 'order_data.csv'")
