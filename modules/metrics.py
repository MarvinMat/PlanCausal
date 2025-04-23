import pandas as pd
from tabulate import tabulate
from modules.logger import Logger
import logging
from scipy.stats import kendalltau
from Levenshtein import distance as levenshtein_distance

logger = Logger.get_global_logger(category="General", level=logging.DEBUG, log_to_file=True, log_filename="output/logs/app.log")

def compare_metrics(schedule_truth, schedule_compare):
    """
    Compare metrics between truth and simulated schedules.
    """
    return {
        'makespan_diff': round(calculate_makespan(schedule_compare) - calculate_makespan(schedule_truth),0)
    }
    
def calculate_schedule(df_schedule):
    """
    Calculate the duration from the minimum start_time of all operations
    to the maximum end_time of all operations and return it.
    """
    min_start_time = df_schedule['start_time'].min()
    max_end_time = df_schedule['end_time'].max()
    
    duration = max_end_time - min_start_time
    return duration

def calculate_makespan(df_schedule):
    # Convert the list of operation objects to a DataFrame
    
    # Calculate start and end times for each job
    grouped_schedule = df_schedule.groupby('job_id').agg({'start_time': 'min', 'end_time': 'max'})

    # Calculate the makespan for each job
    grouped_schedule['makespan'] = grouped_schedule['end_time'] - grouped_schedule['start_time']

    # Calculate the average makespan across all jobs
    average_makespan = round(grouped_schedule['makespan'].mean(),1)
    
    return average_makespan

def compare_makespan(schedule_truth, schedule_approach):
    makespan_truth = calculate_makespan(schedule_truth)
    makespan_approach   = calculate_makespan(schedule_approach)
    return {
        "makespan": round(makespan_approach - makespan_truth , 0)
    }

def calculate_makespan_product(df_schedule):
    # Convert the list of operation objects to a DataFrame
    
    # Calculate start and end times for each job
    grouped_schedule = df_schedule.groupby(['product_type', 'job_id']).agg({'start_time': 'min', 'end_time': 'max'})

    # Calculate the makespan for each job
    grouped_schedule['makespan'] = grouped_schedule['end_time'] - grouped_schedule['start_time']

    # Calculate the average makespan across all jobs
    average_makespan = round(grouped_schedule['makespan'].mean(),1)
    
    return average_makespan

def compare_makespan_product(schedule_truth, schedule_approach):
    makespan_truth = calculate_makespan_product(schedule_truth)
    makespan_approach   = calculate_makespan_product(schedule_approach)
    return {
        "makespan(product)": round(makespan_approach - makespan_truth , 0)
    }

# --- Positional Differences: Using the truth schedule as reference ---
def compare_positional_deviations(schedule_truth, schedule_approach):
    """
    Compares the positional deviations between the truth schedule and a simulation schedule.
    This function merges the truth and simulation DataFrames on (job_id, operation_id) and
    calculates the absolute differences in start and end times.
    
    Returns:
        - avg_abs_start_deviation: Average absolute difference in start times.
        - max_abs_start_deviation: Maximum absolute difference in start times.
        - avg_abs_end_deviation: Average absolute difference in end times.
        - max_abs_end_deviation: Maximum absolute difference in end times.
    """
    merged = pd.merge(
        schedule_truth, schedule_approach,
        on=['job_id', 'operation_id'],
        suffixes=('_truth', '_approach')
    )
    
    merged['abs_start_deviation'] = (merged['start_time_approach'] - merged['start_time_truth']).abs()
    merged['abs_end_deviation'] = (merged['end_time_approach'] - merged['end_time_truth']).abs()
    
    return {
        'avg_abs_start_deviation': round(merged['abs_start_deviation'].mean(), 1),
        'max_abs_start_deviation': round(merged['abs_start_deviation'].max(), 1),
        'avg_abs_end_deviation': round(merged['abs_end_deviation'].mean(), 1),
        'max_abs_end_deviation': round(merged['abs_end_deviation'].max(), 1)
    }

def compare_operation_start_end_shifts(schedule_truth, schedule_approach):
    merged = pd.merge(
        schedule_truth, schedule_approach,
        on=['job_id', 'operation_id'],
        suffixes=('_truth', '_approach')
    )
    # These differences may be positive or negative (shift direction)
    merged['start_shift'] = merged['start_time_truth'] - merged['start_time_approach']
    merged['end_shift'] = merged['end_time_approach'] - merged['end_time_truth']
    
    return {
        'avg_start_shift': round(merged['start_shift'].mean(), 1),
        'max_start_shift': round(merged['start_shift'].abs().max(), 1),
        'avg_end_shift': round(merged['end_shift'].mean(), 1),
        'max_end_shift': round(merged['end_shift'].abs().max(), 1)
    }

def calculate_duration_deviation(schedule):
    """
    Calculates per-job deviations between actual and planned durations.
    """
    df = schedule.copy()
    df['duration_deviation'] = df['duration'] - df['plan_duration']
    deviation_per_job = df.groupby('job_id')['duration_deviation'].sum()
    return {
        'avg_duration_deviation': round(deviation_per_job.mean(), 1),
        'max_duration_deviation': round(deviation_per_job.abs().max(), 1)
    }

def compare_duration_deviation(schedule_truth, schedule_approach):
    deviations_truth = calculate_duration_deviation(schedule_truth)
    deviations_approach   = calculate_duration_deviation(schedule_approach)
    
    deviations = {}
    deviations['avg_duration_deviation_truth'] = deviations_truth['avg_duration_deviation']
    deviations['max_duration_deviation_truth'] = deviations_truth['max_duration_deviation']
    deviations['avg_duration_deviation_approach']   = deviations_approach['avg_duration_deviation']
    deviations['max_duration_deviation_approach']   = deviations_approach['max_duration_deviation']
    
    return {
        'avg_duration_deviation_diff': round(deviations_approach['avg_duration_deviation'] - deviations_truth['avg_duration_deviation'], 1),
        'max_duration_deviation_diff': round(deviations_approach['max_duration_deviation'] - deviations_truth['max_duration_deviation'], 1)
    }

def compare_machine_operation_sequences(schedule_truth, schedule_approach):
    """
    Groups operations by machine, sorts by start time, and compares sequences between schedules.
    Returns similarity metrics for each machine's operation order, including Levenshtein distance
    and normalized discounted cumulative gain (nDCG).
    
    Args:
        schedule_truth: DataFrame with truth schedule
        schedule_approach: DataFrame with approach schedule
    
    Returns:
        dict: Dictionary with machine-wise sequence comparisons and overall metrics
    """
    # Ensure both schedules are sorted by start time    
    machine_metrics = {}
    overall_levenshtein_similarity = []
    overall_ndcg_similarity = []
    
    # Group by machine and sort by start time for both schedules
    truth_grouped = schedule_truth.sort_values('start_time').groupby('machine')
    approach_grouped = schedule_approach.sort_values('start_time').groupby('machine')
    
    # Compare sequences for each machine
    for machine in truth_grouped.groups.keys():
        truth_ops = truth_grouped.get_group(machine)[['job_id', 'operation_id']].values.tolist()
        
        if machine in approach_grouped.groups:
            approach_ops = approach_grouped.get_group(machine)[['job_id', 'operation_id']].values.tolist()
            
            # Convert operation lists to comparable format
            truth_seq = [f"{job}_{op}" for job, op in truth_ops]
            approach_seq = [f"{job}_{op}" for job, op in approach_ops]

            # Calculate Levenshtein distance between the two sequences           
            levenshtein_distance_value = list_distance(truth_seq, approach_seq)
            
            # Calculate nDCG
            ndcg_value = calculate_ndcg(truth_seq, approach_seq)
            
            machine_metrics[machine] = {
                'truth_sequence': truth_seq,
                'approach_sequence': approach_seq,
                'operations_count': len(truth_seq),
                'misplaced_operations': len(set(truth_seq) ^ set(approach_seq)),
                'levenshtein_distance': levenshtein_distance_value,
                'ndcg': round(ndcg_value, 10)
            }
            
            overall_levenshtein_similarity.append(levenshtein_distance_value)
            overall_ndcg_similarity.append(ndcg_value)
    
    # Calculate overall metrics
    results = {
        'machine_wise_metrics': machine_metrics,
        'overall_levenshtein_sequence_similarity': round(sum(overall_levenshtein_similarity) / len(overall_levenshtein_similarity), 3) if overall_levenshtein_similarity else 0,
        'overall_ndcg_similarity': round(sum(overall_ndcg_similarity) / len(overall_ndcg_similarity), 10) if overall_ndcg_similarity else 0
    }
    # Add a dict entry for each machine inline
    results.update({f'machine_{machine}_levenshtein_distance': metrics['levenshtein_distance'] for machine, metrics in machine_metrics.items()})
    results.update({f'machine_{machine}_ndcg': metrics['ndcg'] for machine, metrics in machine_metrics.items()})
    
    return results

def list_distance(A, B):
    # Assign each unique value of the list to a unicode character
    unique_map = {v:chr(k) for (k,v) in enumerate(set(A+B))}
    
    # Create string versions of the lists
    a = ''.join(list(map(unique_map.get, A)))
    b = ''.join(list(map(unique_map.get, B)))
    
    levenshtein_result = levenshtein_distance(a, b)
    return levenshtein_result

def calculate_ndcg(A, B):
    """
    Calculate the normalized discounted cumulative gain (nDCG) for two sequences.
    """
    relevance = {op: len(A) - i for i, op in enumerate(A)}
    dcg = 0
    for i, op in enumerate(B):
        contribution = relevance.get(op, 0) / (i + 1)
        dcg += contribution
       
    idcg = 0
    for i, op in enumerate(A):
        if op in relevance:
            contribution = relevance[op] / (i + 1)
            idcg += contribution
    
    #print(f"Final DCG: {dcg}, Final IDCG: {idcg}")
    ndcg_result = dcg / idcg if idcg > 0 else 0
    return ndcg_result

def compare_tool_change_on_machine(schedule_truth, schedule_approach):
    """
    Compares the tool change operations on a machine between the truth and approach schedules.
    Counts the number of tool changes without using a distance measure.
    
    Args:
        schedule_truth: DataFrame with truth schedule
        schedule_approach: DataFrame with approach schedule
    Returns:
        dict: Dictionary with machine-wise tool change counts
    """
    # Ensure both schedules are sorted by start time    
    machine_metrics = {}
    tool_changes = []
    
    # Group by machine and sort by start time for both schedules
    truth_grouped = schedule_truth.sort_values('start_time').groupby('machine')
    approach_grouped = schedule_approach.sort_values('start_time').groupby('machine')
    
    # Compare tool changes for each machine
    for machine in truth_grouped.groups.keys():
        truth_ops = truth_grouped.get_group(machine)['tool'].tolist()
        
        if machine in approach_grouped.groups:
            approach_ops = approach_grouped.get_group(machine)['tool'].tolist()
            
            # Count tool changes in truth and approach schedules
            truth_tool_changes = sum(1 for i in range(1, len(truth_ops)) if truth_ops[i] != truth_ops[i - 1])
            approach_tool_changes = sum(1 for i in range(1, len(approach_ops)) if approach_ops[i] != approach_ops[i - 1])
            
            changes_to_total = round((approach_tool_changes / len(approach_ops)) * 100, 2) if approach_ops else 0.0
            
            machine_metrics[machine] = {
                'truth_tool_changes': truth_tool_changes,
                'approach_tool_changes': approach_tool_changes,
                'tool_change_difference': approach_tool_changes - truth_tool_changes,
                'tool_change_percentage': changes_to_total
            }
            
            # Calculate the percentage of tool changes
            if truth_tool_changes > 0:
                machine_metrics[machine]['tool_change_percentage'] = round((approach_tool_changes - truth_tool_changes) / truth_tool_changes * 100, 2)
            else:
                machine_metrics[machine]['tool_change_percentage'] = 0.0
            
            tool_changes.append(changes_to_total)
    
    results = {
        'machine_wise_tool_changes': machine_metrics,
        'overall_tool_change_difference': sum(metrics['tool_change_difference'] for metrics in machine_metrics.values()),
        'overall_tool_changes': round(sum(tool_changes) / len(tool_changes), 3) if tool_changes else 0
    }
    return results
    

# --- Combined Extended Comparison ---
def extended_compare_schedule(schedule_truth, schedule_approach):
    """
    Compares a simulation schedule to the truth schedule using various metrics.
    Returns a dictionary of metrics.
    """
    metrics = {}
    
    # Makespan
    metrics.update(compare_makespan(schedule_truth, schedule_approach))
    
    # Makespan product
    #metrics.update(compare_makespan_product(schedule_truth, schedule_approach))
    
    # Operation shift
    #metrics.update(compare_operation_start_end_shifts(schedule_truth, schedule_approach))
        
    # Duration deviation
    metrics.update(compare_duration_deviation(schedule_truth, schedule_approach))
    
    # Positional deviations
    metrics.update(compare_positional_deviations(schedule_truth, schedule_approach))
    
    # Positional differences
    #metrics.update(compare_machine_positional_differences(schedule_truth, schedule_approach))
    
    # Compare Sequence of Operations
    metrics.update(compare_machine_operation_sequences(schedule_truth, schedule_approach))
    
    # Compare Tool Change on Machine
    metrics.update(compare_tool_change_on_machine(schedule_truth, schedule_approach))
    
    return { 
        'makespan (over all)': abs(metrics['makespan']), 
        #'makespan (over product)': metrics['makespan(product)'], 
        #'avg operation shift': metrics['avg_start_shift'],
        'avg abs start deviation': abs(metrics['avg_abs_start_deviation']),
        'avg duration deviation': abs(metrics['avg_duration_deviation_diff']),
        #'kendall_sequ_sim': metrics['overall_sequence_similarity'],
        'levenshtein_seq_sim': metrics['overall_levenshtein_sequence_similarity'],
        'ndcg_seq_similarity': metrics['overall_ndcg_similarity'],
        'a1_0_seq_sim': metrics['machine_a1_0_levenshtein_distance'],
        'a2_0_seq_sim': metrics['machine_a2_0_levenshtein_distance'],
        'a3_0_seq_sim': metrics['machine_a3_0_levenshtein_distance'],
        'overall_tool_changes': metrics['overall_tool_changes']
        #'machine_shift_count': metrics['machine_shift_count']        
    }

def extended_compare_all_schedules(schedules, reference_schedule):
    """
    Iterates over all simulation schedules (given as a dictionary of DataFrames),
    compares each to the truth schedule, and returns a DataFrame with the extended metrics.
    
    :param schedules: Dictionary with schedule names as keys and DataFrames as values.
    :param truth_model_name: The key for the truth schedule.
    :return: DataFrame with comparison metrics for each simulation schedule.
    """
    results = []
    
    for model_name, schedule_df in schedules.items():
        #if model_name == truth_model_name:
        #    continue  # Skip the truth schedule itself
        metrics = extended_compare_schedule(reference_schedule, schedule_df)
        metrics = {'Model': model_name, **metrics}
        results.append(metrics)
    
    return pd.DataFrame(results)

def compare_all_schedules(schedules, truth_model_name):
    """
    Compare makespan differences between multiple schedules and return a formatted DataFrame.
    
    :param schedules: Dictionary with schedule names as keys and corresponding DataFrames as values
    :param truth_model_name: The key of the truth schedule in the schedules dictionary
    :return: DataFrame with makespan differences
    """
    results = []
    
    # Get the makespan of the truth model
    makespan_truth = calculate_makespan(schedules[truth_model_name])
    
    for model_name, df_schedule in schedules.items():
        if model_name == truth_model_name:
            continue  # Skip comparison with itself
        
        makespan_model = calculate_makespan(df_schedule)
        makespan_diff = round(makespan_model - makespan_truth, 0)
        
        results.append({
            'Compared Model': model_name,
            'Makespan Truth': makespan_truth,
            'Makespan Compared': makespan_model,
            'Makespan Difference': makespan_diff
        })
    
    return pd.DataFrame(results)

def print_comparison_table(df_results):
    """
    Print the schedule comparison results in a nicely formatted table.
    
    :param df_results: DataFrame containing the makespan comparison results
    """
    columns_to_drop = ['instance', 'priority_rule']
    df_results = df_results.drop(columns=columns_to_drop, errors='ignore')
    print(tabulate(df_results, headers='keys', tablefmt='rounded_outline'))
    #print(tabulate(df_results, headers='keys', tablefmt='latex_booktabs'))