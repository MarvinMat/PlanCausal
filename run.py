from modules.data_processing import generate_data, save_data, prepare_data
from modules.simulation import run_simulation
from modules.metrics import calculate_makespan, compare_metrics
from models.implementations.causal import CausalModel
from models.implementations.truth import TruthModel
import pandas as pd

# File paths
output_path = "output/results/simulation_results.csv"

# Step 1: Load and prepare data
operations, machines = generate_data(num_instances=150)

truth_model = TruthModel()
learned_model = CausalModel()

# Step 2: Run simulation
observed_data = run_simulation(machines, operations, truth_model)

# Step 3: Save observed data
save_data(pd.DataFrame(observed_data), output_path)

# Step 4: Calculate metrics
makespan = calculate_makespan(pd.DataFrame(observed_data))
print(f"Simulation Makespan: {makespan}")
