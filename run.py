from modules.data_processing import generate_data, save_data
from modules.simulation import run_simulation
from modules.metrics import calculate_makespan, compare_job_flow_times, calculate_duration_deviation, print_comparison_table, extended_compare_all_schedules
from models.implementations.causal import CausalModel
from models.implementations.truth import TruthModel
from models.implementations.basic import BasicModel
from models.implementations.average import AverageModel
from models.implementations.normal_distribution import NormalDistributionModel
from models.implementations.exponential_distribution import ExponentialDistributionModel
from models.implementations.kde_distribution import KDEDistributionModel
from models.implementations.log_normal_distribution import LogNormalDistributionModel
from modules.plan.GifflerThompson import GifflerThompson
from modules.vizualisation import GanttSchedule
from modules.logger import Logger
import argparse
import pandas as pd 
import logging

# Define the logger
logger = Logger.get_global_logger(category="General", level=logging.DEBUG, log_to_file=True, log_filename="output/logs/app.log")
# Set the log level for modules 
Logger.set_log_level(category="General", level=logging.DEBUG)
Logger.set_log_filter(category="General", level=logging.DEBUG)
Logger.set_log_level(category="Simulation", level=logging.ERROR)
Logger.set_log_filter(category="Simulation", level=logging.ERROR)
Logger.set_log_level(category="Model", level=logging.ERROR)
Logger.set_log_filter(category="Model", level=logging.ERROR)

# Command-line argument parser
def parse_arguments():
    parser = argparse.ArgumentParser(description="Run simulation and optionally perform evaluation.")
    parser.add_argument("--evaluate", action="store_true", help="Perform evaluation step.")
    parser.add_argument("--priority_rule", type=str, default="dynamic", choices=["dynamic", "fcfs"], help="Priority rule to use: 'dynamic' or 'fcfs'.")
    parser.add_argument("--instances", type=int, default=150, help="Number of instances for data generation.")
    parser.add_argument("--output", type=str, default="./output/results/", help="Output path for schedule data.")
    parser.add_argument("--seed", type=int, help="Seed for random generators.")
    parser.add_argument("--observed_data", type=str, default="./data/data_observe.csv", help="Path to observed data CSV.")
    parser.add_argument("--result_data", type=str, default="./output/results/schedule_TruthModel.csv", help="Path to result data CSV.")
    parser.add_argument("--plots", type=str, default="./output/plots", help="Output path for Gantt plots.")
    return parser.parse_args()


def main():
    args = parse_arguments()
    
    models = []
    try:
        models.append(TruthModel(seed=args.seed))  
        models.append(AverageModel(csv_file=args.observed_data)) 
        #models.append(LogNormalDistributionModel(csv_file=args.result_data, seed=args.seed))
        models.append(CausalModel(csv_file=args.observed_data, truth_model=models[0]))
        #models.append(BasicModel())
        #models.append(NormalDistributionModel(csv_file=args.result_data, seed=args.seed))
        #models.append(ExponentialDistributionModel(csv_file=args.result_data, seed=args.seed))
        #models.append(KDEDistributionModel(csv_file=args.result_data, seed= args.seed))
    except Exception as e:
        logger.error(f"Error initializing models: {e}")
        return

    logger.debug("Start model iteration.")
    schedules = {}
    
    for model in models: 
        
        model.initialize()
        
        # Step 1: Generate data
        operations, machines = generate_data(num_instances=args.instances)
        model_name = type(model).__name__

        # Step 2: Create plan    
        # TODO: must this be the basic giffler thompson plan - but with inferencing while simulation with something the truth?
        plan = GifflerThompson(rule_name=args.priority_rule, inference=model.inference)
        schedule = plan.giffen_thompson(operations, machines)
        
        schedule_results = None
        
        # Step 3: Run simulation
        if isinstance(model, TruthModel):
            
            result = run_simulation(machines, operations, model, args.observed_data)
            schedule_results = pd.DataFrame([op.to_dict_sim() for op in result])
            # observings müssen noch erstellt werden in output_path_observed
        else: 
            schedule_results = pd.DataFrame([op.to_dict() for op in schedule])
        
        schedules[model_name] = schedule_results
        
        # Step 4: Save schedule data
        output_path = save_data(schedule_results, args.output, model_name)

        # Step 5: Calculate metrics
        makespan = calculate_makespan(schedule_results)
        
        # Output the results with the schedule name
        logger.debug(f"{model_name} | avg_makespan | {makespan}")

        job_flow_times = compare_job_flow_times(schedules[TruthModel.__name__], schedules[model_name])

        logger.debug(f"{model_name} | avg_job_flow_times | {job_flow_times['avg_flow_time_diff']}")

        duration_deviation = calculate_duration_deviation(schedules[model_name])

        logger.debug(f"{model_name} | avg_duration_deviation | {duration_deviation['avg_duration_deviation']}")
        
        # Step 6: Create GanttCharts
        output_path = GanttSchedule.create(schedule_results, args.plots, model_name)
    
    # Perform evaluation
    if args.evaluate:
        logger.debug("Compare each schedule with the simulated one.")
        print_comparison_table(extended_compare_all_schedules(schedules=schedules, truth_model_name=TruthModel.__name__))
            
if __name__ == "__main__":
    main()