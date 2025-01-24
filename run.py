from modules.data_processing import generate_data, save_data, prepare_data
from modules.simulation import run_simulation
from modules.metrics import calculate_makespan, compare_metrics
from models.implementations.causal import CausalModel
from models.implementations.truth import TruthModel
from models.implementations.basic import BasicModel
from models.implementations.average import AverageModel
from modules.plan.GifflerThompson import GifflerThompson
from modules.plan.PriorityRules import calculate_dynamic_priority, calculate_fcfs_priority
from modules.vizualisation import GanttSchedule
import pandas as pd
import matplotlib.pyplot as plt

priority_rule = calculate_dynamic_priority

# File paths
output_path_schedule = "./output/results/"
oberserved_data_path = "./data/data_observe.csv"
output_plot = './output/plots'

models = []
try:
    models.append(TruthModel())  
    models.append(TruthModel())  
    models.append(CausalModel(csv_file=oberserved_data_path))
    models.append(BasicModel())
    models.append(AverageModel(csv_file=oberserved_data_path))
    
except Exception as e:
    print(f"Error initializing models: {e}")

for model in models: 
    
    model.initialize()
    
    # Load and prepare data
    operations, machines = generate_data(num_instances=150)
    model_name = type(model).__name__

    # Step 1: Create plan    
    plan = GifflerThompson(priority_rule, model.inference)
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
    save_data(schedule_results, "{model_name}_{output_path_schedule}" )

    # Step 4: Calculate metrics
    makespan = calculate_makespan(schedule_results, model_name)
    
    # Step 5: Calculate metrics
    output_path = GanttSchedule.create(schedule_results, output_plot, model_name)
