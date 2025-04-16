import pandas as pd
from tabulate import tabulate
from modules.logger import Logger
import logging
from scipy.stats import kendalltau

logger = Logger.get_global_logger(category="General", level=logging.DEBUG, log_to_file=True, log_filename="output/logs/app.log")

def compare_metrics(schedule_truth, schedule_compare):
    """
    Compare metrics between truth and simulated schedules.
    """
    return {
        'makespan_diff': round(calculate_makespan(schedule_compare) - calculate_makespan(schedule_truth),0)
    }

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
    Returns similarity metrics for each machine's operation order.
    
    Args:
        schedule_truth: DataFrame with truth schedule
        schedule_approach: DataFrame with approach schedule
    
    Returns:
        dict: Dictionary with machine-wise sequence comparisons and overall metrics
    """
    # Ensure both schedules are sorted by start time    
    machine_metrics = {}
    overall_similarity = []
    
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
            
            # Cut sequences to the smaller size
            # TODO: Workaround at the moment - sizes of sequences are not equal - therefore kandall's tau cannot be calculated
            # This is a workaround - the sequences should be equal in size
            min_length = min(len(truth_seq), len(approach_seq))
            truth_seq = truth_seq[:min_length]
            approach_seq = approach_seq[:min_length]
            
            # Calculate Kendall's Tau correlation
            tau, p_value = kendalltau(range(len(truth_seq)), 
                                    [truth_seq.index(op) if op in truth_seq else len(truth_seq) 
                                     for op in approach_seq])
            
            similarity = (tau + 1) / 2  # Convert to [0,1] range
            
            machine_metrics[machine] = {
                'truth_sequence': truth_seq,
                'approach_sequence': approach_seq,
                'sequence_similarity': round(similarity, 3),
                'operations_count': len(truth_seq),
                'misplaced_operations': len(set(truth_seq) ^ set(approach_seq))
            }
            
            overall_similarity.append(similarity)
    
    # Calculate overall metrics
    results = {
        'machine_wise_metrics': machine_metrics,
        'overall_sequence_similarity': round(sum(overall_similarity) / len(overall_similarity), 3) if overall_similarity else 0
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
    
    return { 
        'Makespan (vs Truth)': metrics['makespan'], 
        #'avg operation shift': metrics['avg_start_shift'],
        'avg abs start deviation': metrics['avg_abs_start_deviation'],
        'avg duration deviation': metrics['avg_duration_deviation_diff'],
        'overall_sequence_similarity': metrics['overall_sequence_similarity'],
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