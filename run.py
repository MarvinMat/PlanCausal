import os
import random
from modules.data_processing import ProductionGenerator
from modules.simulation import run_simulation
from modules.metrics import calculate_makespan, compare_makespan, calculate_duration_deviation, print_comparison_table, extended_compare_all_schedules
from models.implementations.causal import CausalModel
from models.implementations.truth import TruthModel
from models.implementations.basic import BasicModel
from models.implementations.average import AverageModel
from models.implementations.average_operation import AverageOperationModel
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
    parser.add_argument("--eval", action="store_true", help="Perform evaluation step.")
    parser.add_argument("--planned_mode", action="store_true", help="Using planned schedule.")
    parser.add_argument("--priority_rule", type=str, default="dynamic", choices=["dynamic", "fcfs"], help="Priority rule to use: 'dynamic' or 'fcfs'.")
    parser.add_argument("--instances", type=int, default=150, help="Number of instances for data generation.")
    parser.add_argument("--output", type=str, default="./output/results/", help="Output path for schedule data.")
    parser.add_argument("--seed", type=int, help="Seed for random generators.")
    parser.add_argument("--seed_iterator", type=int, default=1, help="Seed for random generators.")
    parser.add_argument("--observed_data_path", type=str, default="./data/data_observe.csv", help="Path to observed data CSV.")
    parser.add_argument("--result_data", type=str, default="./output/results/schedule_TruthModel.csv", help="Path to result data CSV.")
    parser.add_argument("--experiment_result_data", type=str, default="./output/experiments/experiment_results.csv", help="Path to experiment result data CSV.")
    parser.add_argument("--plots", type=str, default="./output/plots", help="Output path for Gantt plots.")
    parser.add_argument("--useGroundTruthPlan", action="store_true", help="Use ground truth plan for scheduling.")
    return parser.parse_args()


def run_experiment(seed, args):
    """
    Runs the experiment for a single seed.
    """
    logger.debug(f"Running experiment with seed: {seed}")
    args.seed = seed  # Update the seed for this run
    models = []
    try:
        models.append(TruthModel(seed=args.seed))
        models.append(AverageOperationModel(csv_file=args.result_data))
        #models.append(AverageModel(csv_file=args.observed_data_path))
        models.append(LogNormalDistributionModel(csv_file=args.result_data, seed=args.seed))
        models.append(CausalModel(csv_file=args.observed_data_path, truth_model=models[0], structure_learning_func='pgmpy', structure_learning_method='ExhaustiveSearch'))
    except Exception as e:
        logger.error(f"Error initializing models: {e}")
        return

    logger.debug("Start model iteration.")
    schedules = {}
    planed_schedules = {}
    truthModel = None

    for model in models:
        model.initialize()
        model_name = type(model).__name__

        # Step 1: Generate data
        production = ProductionGenerator()
        operations, machines = production.generate_data_dynamic(amount_products = 10,
                                                                product_types_relation = None,
                                                                avg_operations=4, 
                                                                avg_duration=15,
                                                                machine_groups=3, 
                                                                tools_per_machine=2,
                                                                num_instances=args.instances, 
                                                                distribution='equal',
                                                                seed=args.seed)
        
        # Step 2: Create schedule
        # TODO: Always use the ground truth plan    
        plan = GifflerThompson(rule_name=args.priority_rule, inference=model.inference)
        schedule = plan.create_schedule(operations, machines)
        planed_schedules[model_name] = pd.DataFrame([op.to_dict() for op in schedule])

        schedule_results = None

        # Step 3: Run simulation
        if isinstance(model, TruthModel):
            # Show the product structure based on the given template
            production.job_data_metric()
            # Run simulation with the truth model
            result = run_simulation(machines, operations, model, args.planned_mode, args.observed_data_path)
            schedule_results = pd.DataFrame([op.to_dict_sim() for op in result])
            truthModel = model
        else:
            # Use the approach model to run the simulation
            model_feedback_path = os.path.join(os.path.dirname(args.observed_data_path), model_name)
            result = run_simulation(machines, operations, model, args.planned_mode, model_feedback_path)
            schedule_results = pd.DataFrame([op.to_dict_sim() for op in result])
            

        schedules[model_name] = schedule_results

        # Step 4: Save schedule data
        output_path = production.save_data(schedule_results, args.output, model_name)

        # Step 5: Calculate metrics
        makespan = calculate_makespan(schedule_results)
        makespan_diff = compare_makespan(planed_schedules[TruthModel.__name__], schedules[model_name])
        logger.debug(f"{model_name} | Makespan (Approach) {makespan} | Makespan-Diff (vs Truth) {makespan_diff['makespan']}")

        # Step 6: Create GanttCharts
        viz_output_path = GanttSchedule.create(schedule_results, args.plots, model_name)

    # Perform evaluation
    if args.eval:
        results_run = extended_compare_all_schedules(schedules=schedules, reference_schedule=planed_schedules[TruthModel.__name__])
        results_run['seed'] = args.seed  # Add seed to the results
        results_run['instance'] = args.instances  # Add instance to the results
        results_run['priority_rule'] = args.priority_rule
        print_comparison_table(results_run)
        
        file_exists = os.path.exists(args.experiment_result_data)  # Check if the file already exists

        # Save results for this seed to the CSV file
        results_df = pd.DataFrame(results_run)
        results_df.to_csv(args.experiment_result_data, mode='a', header=not file_exists, index=False)
        logger.debug(f"Results for seed {args.seed} saved to {args.experiment_result_data}")


def main():
    args = parse_arguments()

    os.makedirs(os.path.dirname(args.experiment_result_data), exist_ok=True)

    # Write the header to the CSV file if it doesn't exist
    #with open(args.experiment_result_data, 'w') as f:
        #print(f'{args.experiment_result_data} created')

    args.seed = random.randint(0, 10000) if args.seed is None else args.seed

    # Check if seed_iterator is provided
    if args.seed_iterator > 1:
        seed_end = args.seed + args.seed_iterator
        seed_range = range(args.seed, seed_end)
        for seed in seed_range:
            run_experiment(seed, args)
    else:
        # Run for a single seed
        run_experiment(args.seed, args)


if __name__ == "__main__":
    main()