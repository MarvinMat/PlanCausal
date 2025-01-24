from modules.data_processing import generate_data, save_data, prepare_data
from modules.simulation import run_simulation
from modules.metrics import calculate_makespan, compare_metrics
from models.implementations.causal import CausalModel
from models.implementations.truth import TruthModel
from models.implementations.basic import BasicModel
from models.implementations.average import AverageModel
from modules.plan.GifflerThompson import GifflerThompson
from modules.plan.PriorityRules import calculate_dynamic_priority, calculate_fcfs_priority
import pandas as pd

# File paths
output_path_schedule = "output/results/simulation_results.csv"
oberserved_data_path = "input/observed_data.csv"

models = []
try:
    models.append(TruthModel())  
    models.append(CausalModel(csv_file=oberserved_data_path))
    models.append(BasicModel())
    models.append(AverageModel(csv_file=oberserved_data_path))
    
except Exception as e:
    print(f"Error initializing models: {e}")

for model in models: 
    
    # Load and prepare data
    operations, machines = generate_data(num_instances=150)
    model_name = type(model).__name__

    # Step 1: Create plan    
    plan = GifflerThompson(calculate_dynamic_priority, model.inference)
    schedule = plan.giffen_thompson(operations, machines)
    
    schedule_results = None
    # Step 2: Run simulation
    if isinstance(model, TruthModel):
        
        result = run_simulation(machines, operations, model, oberserved_data_path)
        schedule_results = pd.DataFrame([op.to_dict_sim() for op in result])
        # observings müssen noch erstellt werden in output_path_observed
    else: 
        schedule_results = pd.DataFrame([op.to_dict() for op in schedule])
        
    # Step 3: Save schedule data
    save_data(schedule_results, "{model_name}_{output_path_schedule} " )

    # Step 4: Calculate metrics
    makespan = calculate_makespan(schedule_results, model_name)
    print(f"Simulation Makespan: {makespan}")
