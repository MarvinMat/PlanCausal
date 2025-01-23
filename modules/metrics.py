import pandas as pd

def calculate_makespan(df_schedule):
    """
    Calculate makespan for a given schedule.
    """
    grouped_schedule = df_schedule.groupby('job_id')
    start_times = grouped_schedule['start_time'].min()
    end_times = grouped_schedule['end_time'].max()
    return (end_times - start_times).max()

def compare_metrics(df_truth, df_sim):
    """
    Compare metrics between truth and simulated schedules.
    """
    return {
        'makespan_diff': calculate_makespan(df_sim) - calculate_makespan(df_truth)
    }

def calculate_makespan(df_schedule, schedule_name):
    # Convert the list of operation objects to a DataFrame
    
    # Calculate start and end times for each job
    grouped_schedule = df_schedule.groupby('job_id').agg({'start_time': 'min', 'end_time': 'max'})

    # Calculate the makespan for each job
    grouped_schedule['makespan'] = grouped_schedule['end_time'] - grouped_schedule['start_time']

    # Calculate the average makespan across all jobs
    average_makespan = grouped_schedule['makespan'].mean()

    # Output the results with the schedule name
    print(f"{schedule_name} | {average_makespan} time units")
