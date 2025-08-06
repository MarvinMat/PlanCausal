from modules.metrics import calculate_schedule, calculate_throughput, compare_throughput, calculate_duration_deviation, print_comparison_table, extended_compare_all_schedules, extended_compare_schedules_pairwaise
import pandas as pd
import os
import re
from collections import defaultdict
from tabulate import tabulate
#schedule_AverageOperationModel_77.csv

# This script evaluates the results of scheduling experiments by reading CSV files,
# extracting relevant data, and performing comparisons between different scheduling models.
# Define the main folder path where the results are stored

main_folder = 'notebooks/Paper/ASIM/20250505_215818/'

folder_path = main_folder + 'results/'

# Regex pattern to extract model and experiment_id
pattern = re.compile(r"schedule_(.+)_(\d+)\.csv")

# Nested dictionary: experiment_id → model_name → DataFrame
experiment_dfs = defaultdict(dict)

for filename in os.listdir(folder_path):
    match = pattern.match(filename)
    if match:
        model_name, experiment_id = match.groups()
        experiment_id = int(experiment_id)

        if experiment_id != '2000':
            filepath = os.path.join(folder_path, filename)
            try:
                df = pd.read_csv(filepath)

                expected_columns = [
                    'job_id', 'product_type', 'operation_id',
                    'machine', 'tool', 'start_time',
                    'duration', 'plan_duration', 'end_time'
                ]
                
                if all(col in df.columns for col in expected_columns):
                    # Optionally add metadata
                    df['experiment_id'] = experiment_id
                    df['model_name'] = model_name

                    # Store in nested dict
                    if model_name != 'TruthContinousSmallLogCopyModel_2000':
                        experiment_dfs[experiment_id][model_name] = df
                else:
                    print(f"Skipping {filename}: Missing expected columns.")
            except Exception as e:
                print(f"Error reading {filename}: {e}")

# ✅ Example usage: access data for model X in experiment 85
# df = experiment_dfs[85]['LogNormalDistributionModel']
results = pd.DataFrame()
# ✅ Summary
for exp_id, schedules in experiment_dfs.items():
    results_run = extended_compare_all_schedules(schedules=schedules, reference_schedule=schedules['TruthContinousSmallLogCopyModel'],)
    results_run['seed'] = exp_id # Add seed to the results
    results_run['instance'] = 100  # Add instance to the results
    #print_comparison_table(results_run)
    # Save results for this seed to the CSV file
    results_df = pd.DataFrame(results_run)
    results = pd.concat([results, results_df], ignore_index=True)

    #print(f"Experiment {exp_id} has {len(models)} models: {list(models.keys())}")

describe_table = results.groupby("Model").describe()

# Display the descriptive statistics table
describe_table

# Extract the mean values for each variable
results = describe_table.xs('mean', level=1, axis=1)

print(tabulate(results, headers='keys', tablefmt='rounded_outline'))
    