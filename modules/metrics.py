import pandas as pd
from tabulate import tabulate
from modules.logger import Logger
import logging

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


# --- Positional Differences: Using the truth schedule as reference ---
def compare_positional_deviations(schedule_truth, schedule_sim):
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
        schedule_truth, schedule_sim,
        on=['job_id', 'operation_id'],
        suffixes=('_truth', '_sim')
    )
    
    merged['abs_start_deviation'] = (merged['start_time_sim'] - merged['start_time_truth']).abs()
    merged['abs_end_deviation'] = (merged['end_time_sim'] - merged['end_time_truth']).abs()
    
    return {
        'avg_abs_start_deviation': round(merged['abs_start_deviation'].mean(), 1),
        #'max_abs_start_deviation': round(merged['abs_start_deviation'].max(), 1),
        'avg_abs_end_deviation': round(merged['abs_end_deviation'].mean(), 1),
        #'max_abs_end_deviation': round(merged['abs_end_deviation'].max(), 1)
    }

# --- Existing Helper Metrics (Plan Shifts, Flow Times, Duration Deviations) ---
def compare_plan_shifts(schedule_truth, schedule_sim):
    merged = pd.merge(
        schedule_truth, schedule_sim,
        on=['job_id', 'operation_id'],
        suffixes=('_truth', '_sim')
    )
    # These differences may be positive or negative (shift direction)
    merged['start_shift'] = merged['start_time_sim'] - merged['start_time_truth']
    merged['end_shift'] = merged['end_time_sim'] - merged['end_time_truth']
    
    return {
        'avg_start_shift': round(merged['start_shift'].mean(), 1),
        #'max_start_shift': round(merged['start_shift'].abs().max(), 1),
        'avg_end_shift': round(merged['end_shift'].mean(), 1),
        #'max_end_shift': round(merged['end_shift'].abs().max(), 1)
    }

def compare_job_flow_times(schedule_truth, schedule_sim):
    truth_jobs = schedule_truth.groupby('job_id').agg({'start_time': 'min', 'end_time': 'max'})
    sim_jobs   = schedule_sim.groupby('job_id').agg({'start_time': 'min', 'end_time': 'max'})
    
    truth_jobs.rename(columns={'start_time': 'start_truth', 'end_time': 'end_truth'}, inplace=True)
    sim_jobs.rename(columns={'start_time': 'start_sim', 'end_time': 'end_sim'}, inplace=True)
    
    comparison = truth_jobs.join(sim_jobs, how='inner')
    comparison['flow_time_truth'] = comparison['end_truth'] - comparison['start_truth']
    comparison['flow_time_sim']   = comparison['end_sim'] - comparison['start_sim']
    comparison['flow_time_diff']  = comparison['flow_time_sim'] - comparison['flow_time_truth']
    
    return {'avg_flow_time_diff': round(comparison['flow_time_diff'].mean(), 1)}

def calculate_duration_deviation(schedule):
    """
    Calculates per-job deviations between actual and planned durations.
    """
    df = schedule.copy()
    df['duration_deviation'] = df['duration'] - df['plan_duration']
    deviation_per_job = df.groupby('job_id')['duration_deviation'].sum()
    return {
        'avg_duration_deviation': round(deviation_per_job.mean(), 1),
        #'max_duration_deviation': round(deviation_per_job.abs().max(), 1)
    }

# --- Combined Extended Comparison ---
def extended_compare_schedule(schedule_truth, schedule_sim):
    """
    Compares a simulation schedule to the truth schedule using various metrics.
    Returns a dictionary of metrics.
    """
    metrics = {}
    
    # Makespan metrics
    metrics['makespan_truth'] = calculate_makespan(schedule_truth)
    metrics['makespan_sim']   = calculate_makespan(schedule_sim)
    metrics['makespan_diff']  = round(metrics['makespan_sim'] - metrics['makespan_truth'], 0)
    
    # Plan shifts (raw differences, can be negative)
    metrics.update(compare_plan_shifts(schedule_truth, schedule_sim))
    
    # Flow time differences per job
    metrics.update(compare_job_flow_times(schedule_truth, schedule_sim))
    
    # Duration deviation metrics (for each schedule individually and the difference)
    truth_devs = calculate_duration_deviation(schedule_truth)
    sim_devs   = calculate_duration_deviation(schedule_sim)
    
    #metrics['avg_duration_deviation_truth'] = truth_devs['avg_duration_deviation']
    #metrics['max_duration_deviation_truth'] = truth_devs['max_duration_deviation']
    metrics['avg_duration_deviation_sim']   = sim_devs['avg_duration_deviation']
    #metrics['max_duration_deviation_sim']   = sim_devs['max_duration_deviation']
    metrics['avg_duration_deviation_diff']  = round(sim_devs['avg_duration_deviation'] - truth_devs['avg_duration_deviation'], 1)
    #metrics['max_duration_deviation_diff']  = round(sim_devs['max_duration_deviation'] - truth_devs['max_duration_deviation'], 1)
    
    # Positional deviations (using absolute differences)
    metrics.update(compare_positional_deviations(schedule_truth, schedule_sim))
    
    return { 
        'makespan_truth': metrics['makespan_truth'],    
        'makespan_diff': metrics['makespan_diff'], 
        'avg_start_shift': metrics['avg_start_shift'],
        'avg_end_shift': metrics['avg_end_shift'],
        'avg_abs_start_deviation': metrics['avg_abs_start_deviation'],
        'avg_duration_deviation_diff': metrics['avg_duration_deviation_diff'],
        #'avg_flow_time_diff': metrics['avg_flow_time_diff']
    }

def extended_compare_all_schedules(schedules, truth_model_name):
    """
    Iterates over all simulation schedules (given as a dictionary of DataFrames),
    compares each to the truth schedule, and returns a DataFrame with the extended metrics.
    
    :param schedules: Dictionary with schedule names as keys and DataFrames as values.
    :param truth_model_name: The key for the truth schedule.
    :return: DataFrame with comparison metrics for each simulation schedule.
    """
    results = []
    truth_schedule = schedules[truth_model_name]
    
    for model_name, schedule_df in schedules.items():
        #if model_name == truth_model_name:
        #    continue  # Skip the truth schedule itself
        metrics = extended_compare_schedule(truth_schedule, schedule_df)
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
    print(tabulate(df_results, headers='keys', tablefmt='pretty'))