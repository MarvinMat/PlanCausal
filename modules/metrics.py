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

def compare_each_schedule_with_truth_model(schedules, truth_model_name):
    for schedule in schedules:
            makespan_dif = compare_metrics(schedules[truth_model_name], schedules[schedule])
            logger.debug(f"{truth_model_name} vs {schedule} | {makespan_dif} dif time units")


