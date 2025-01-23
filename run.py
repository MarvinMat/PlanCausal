from modules.data_processing import generate_data, save_data, prepare_data
from modules.simulation import run_simulation
from modules.metrics import calculate_makespan, compare_metrics
from models.implementations.causal import CausalModel
from models.implementations.truth import TruthModel
from modules.plan.GifflerThompson import GifflerThompson
from modules.plan.PriorityRules import calculate_dynamic_priority, calculate_fcfs_priority
import pandas as pd

# File paths
output_path = "output/results/simulation_results.csv"

# Step 1: Load and prepare data
operations, machines = generate_data(num_instances=150)

truth_model = TruthModel()
learned_model = CausalModel()

plan = GifflerThompson(calculate_dynamic_priority, truth_model.inference)
schedule = plan.giffen_thompson(operations, machines)

# Step 2: Run simulation
observed_data = run_simulation(machines, operations, truth_model)

# Step 3: Save observed data
save_data(pd.DataFrame(observed_data), output_path)

# Step 4: Calculate metrics
makespan = calculate_makespan(pd.DataFrame(operations),"sim")
print(f"Simulation Makespan: {makespan}")
