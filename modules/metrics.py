import pandas as pd

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
    average_makespan = grouped_schedule['makespan'].mean()
    
    return average_makespan
