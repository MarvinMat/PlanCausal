import os
import random
import concurrent.futures
from tabulate import tabulate
from modules.data_processing import ProductionGenerator
from modules.simulation import run_simulation
from modules.metrics import calculate_schedule, calculate_throughput, compare_throughput, calculate_duration_deviation, print_comparison_table, extended_compare_all_schedules, extended_compare_schedules_pairwaise
from models.implementations.truth_small import TruthSmallModel
from models.implementations.causal_small import CausalSmallModel
from models.implementations.causal import CausalModel
from models.implementations.causal_do import CausalDoModel
from models.implementations.causal_continious import CausalContinousModel
from models.implementations.truth_continous import TruthContinousModel
from models.implementations.truth_continous_small import TruthContinousSmallModel
from models.implementations.causal_continious_small import CausalContinousSmallModel
from models.implementations.causal_continious_small_log_learn import CausalContinousSmallLogLearnModel
from models.implementations.causal_continious_small_log_copy import CausalContinousSmallLogCopyModel
from models.implementations.truth_continous_small_log_learn import TruthContinousSmallLogLearnModel
from models.implementations.truth_continous_small_log_copy import TruthContinousSmallLogCopyModel
from models.implementations.truth_continous_small_trunc_learn import TruthContinousSmallTruncNormalLearnModel
from models.implementations.causal_continious_small_trunc_learn import CausalContinousSmallTruncNormalLearnModel
from models.implementations.truth import TruthModel
from models.implementations.basic import BasicModel
from models.implementations.average import AverageModel
from models.implementations.average_operation import AverageOperationModel
from models.implementations.normal_distribution import NormalDistributionModel
from models.implementations.exponential_distribution import ExponentialDistributionModel
from models.implementations.kde_distribution import KDEDistributionModel
from models.implementations.histo_distribution import HistoDistributionModel
from models.implementations.log_normal_distribution import LogNormalDistributionModel
from modules.plan.GifflerThompson import GifflerThompson
from modules.vizualisation import GanttSchedule
from modules.logger import Logger
import argparse
import pandas as pd 
import logging
from datetime import datetime

# Define the logger
logger = Logger.get_global_logger(category="General", level=logging.DEBUG, log_to_file=True, log_filename="output/logs/app.log")
# Set the log level for modules 
Logger.set_log_level(category="General", level=logging.DEBUG)
Logger.set_log_filter(category="General", level=logging.DEBUG)
Logger.set_log_level(category="Simulation", level=logging.ERROR)
Logger.set_log_filter(category="Simulation", level=logging.ERROR)
Logger.set_log_level(category="Model", level=logging.DEBUG)
Logger.set_log_filter(category="Model", level=logging.DEBUG)

# Command-line argument parser
def parse_arguments():
    parser = argparse.ArgumentParser(description="Run simulation and optionally perform evaluation.")
    #parser.add_argument("--eval", action="store_true", help="Perform evaluation step.")
    parser.add_argument("--planned_mode", action="store_true", help="Using planned schedule.")
    parser.add_argument("--priority_rule", type=str, default="dynamic", choices=["dynamic", "fcfs"], help="Priority rule to use: 'dynamic' or 'fcfs'.")
    parser.add_argument("--instances", type=int, default=150, help="Number of instances for data generation.")
    parser.add_argument("--output", type=str, default="./output/results/", help="Output path for schedule data.")
    parser.add_argument("--seed", type=int, help="Seed for random generators.")
    parser.add_argument("--seed_iterator", type=int, default=1, help="Seed for random generators.")
    #parser.add_argument("--observed_data", type=str, default="./data/data_observe", help="Path to observed data CSV.")
    #parser.add_argument("--result_data", type=str, default="./output/results/schedule", help="Path to result data CSV.")
    parser.add_argument("--experiment_folder", type=str, default="./output/experiments", help="Path to experiment result data CSV.")
    parser.add_argument("--plots", type=str, default="./output/plots", help="Output path for Gantt plots.")
    parser.add_argument("--parallel", action="store_true", help="Uses parallel computing for experiments.")
    return parser.parse_args()


def run_experiment(seed, args):
    """
    Runs the experiment for a single seed.
    """
    logger.debug(f"Running experiment with seed: {seed}")
    args.seed = seed  # Update the seed for this run
    models = []
    try:
        #models.append(TruthContinousSmallLogLearnModel(seed=args.seed))
        #models.append(TruthContinousSmallTruncNormalLearnModel(seed=args.seed))
        
        #models.append(TruthContinousSmallModel(seed=args.seed))
        #models.append(TruthContinousSmallModel(seed=args.seed))
        
        #models.append(TruthSmallModel(seed=args.seed, lognormal_shape_modifier=False))
        #models.append(TruthSmallModel(seed=args.seed, lognormal_shape_modifier=False))
        #models.append(TruthContinousModel(seed=args.seed, lognormal_shape_modifier=False))
        models.append(TruthContinousSmallLogCopyModel(seed=args.seed))
        models.append(TruthContinousSmallLogCopyModel(seed=args.seed))

        #models.append(TruthContinousModel(seed=args.seed, lognormal_shape_modifier=False))
        #models.append(TruthModel(seed=args.seed))
                
        result_data_path = f"{args.result_data}/schedule_{type(models[0]).__name__}_{args.seed}.csv"
        observed_data_path = f"{args.observed_data_path}/data_observe_{type(models[0]).__name__}_{args.instances}_{args.seed}.csv"
        
        models.append(CausalContinousSmallLogCopyModel(csv_file=observed_data_path, truth_model=models[0], structure_learning_lib='gcastle', structure_learning_method='GES'))
        #models.append(CausalContinousSmallTruncNormalLearnModel(csv_file=observed_data_path, truth_model=models[0], structure_learning_lib='gcastle', structure_learning_method='GES'))
        #models.append(CausalContinousSmallLogLearnModel(seed=args.seed, csv_file=observed_data_path, truth_model=models[0], structure_learning_lib='gcastle', structure_learning_method='GES'))
        #models.append(CausalContinousSmallModel(seed= args.seed, csv_file=observed_data_path, truth_model=models[0], structure_learning_lib='gcastle', structure_learning_method='GES'))
        #models.append(CausalSmallModel(csv_file=observed_data_path, truth_model=models[0], structure_learning_lib='pgmpy', structure_learning_method='ExhaustiveSearch'))
        
        #models.append(CausalModel(seed=args.seed,csv_file=observed_data_path, truth_model=models[0], structure_learning_lib='pgmpy', structure_learning_method='ExhaustiveSearch'))
        
        #models.append(CausalContinousModel(csv_file=observed_data_path, truth_model=models[0], structure_learning_lib='gcastle', structure_learning_method='GES'))
        #models.append(CausalDoModel(seed=args.seed, csv_file=observed_data_path, truth_model=models[0], structure_learning_lib='pgmpy', structure_learning_method='ExhaustiveSearch'))
        
        models.append(AverageOperationModel(csv_file=result_data_path))
        #models.append(AverageModel(csv_file=observed_data_path))
        models.append(LogNormalDistributionModel(csv_file=result_data_path))
        models.append(NormalDistributionModel(csv_file=result_data_path))
        models.append(HistoDistributionModel(csv_file=result_data_path)) 
        #models.append(BasicModel())
        basic_model = BasicModel()
    except Exception as e:
        logger.error(f"Error initializing models: {e}")
        return

    logger.debug("Start model iteration.")
        
    schedules = {}
    planed_schedules = {}

    for model in models:
        model.initialize()
        model_name = type(model).__name__

        # Step 1: Generate data
        production = ProductionGenerator()
        
        if model == models[0]:
            numb_instances = args.instances
            model_name = type(model).__name__ + f"_{args.instances}"
        else:
            numb_instances = 50
                        
        operations, machines = production.generate_data_static(num_instances = numb_instances #, seed=1) 
                                                               , seed=args.seed)
        
        # operations, machines = production.generate_data_dynamic(amount_products = 2,
        #                                                         product_types_relation = None,
        #                                                         avg_operations= 5, 
        #                                                         avg_duration= 10,
        #                                                         machine_groups= 3, 
        #                                                         machine_instances= 1,
        #                                                         tools_per_machine= 2,
        #                                                         num_instances=args.instances, 
        #                                                         distribution='equal',
        #                                                         seed=args.seed)
        
        # Step 2: Create schedule
        # TODO: Always use the ground truth plan 
        if not args.planned_mode:
            # Use the planned schedule from the truth model
            #plan = GifflerThompson(rule_name=args.priority_rule, inference=basic_model.inference, do_calculus=False) 
            plan = GifflerThompson(rule_name=args.priority_rule, inference=basic_model.inference, do_calculus=False) 
            #machines = production.read_from_csv(args.result_data, machines=True)
        else:
            if isinstance(model, TruthModel):
                plan = GifflerThompson(rule_name=args.priority_rule, inference=basic_model.inference, do_calculus=False)
            if isinstance(model, CausalDoModel):
                plan = GifflerThompson(rule_name=args.priority_rule, inference=model.inference, do_calculus=True)
            else:
                plan = GifflerThompson(rule_name=args.priority_rule, inference=model.inference, do_calculus=False)              
        schedule = plan.create_schedule(operations, machines)
        planed_schedules[model_name] = pd.DataFrame([op.to_dict() for op in schedule])

        schedule_results = None

        # Step 3: Run simulation
        #if isinstance(model, type(models[0])):
            # Use the planned schedule from the truth model
            # Show the product structure based on the given template
            # production.job_data_metric()
            # Run simulation with the truth model
        #    result = run_simulation(machines, operations, model, False, observed_data_path)
        #    schedule_results = pd.DataFrame([op.to_dict_sim() for op in result])

        #else:
        # Use the approach model to run the simulation
        model_feedback_path = os.path.join(os.path.dirname(observed_data_path), "data_observe_"+ model_name + f"_{args.seed}" + ".csv")
        result = run_simulation(machines, operations, model, args.planned_mode, model_feedback_path)
        schedule_results = pd.DataFrame([op.to_dict_sim() for op in result])
            

        schedules[model_name] = schedule_results

        # Step 4: Save schedule data
        # TODO: Save the schedule data to a CSV file and find a good folder structure
        production_schedule_path = f"{args.result_data}/schedule_{model_name}_{args.seed}.csv"
        output_path = production.save_data(schedule_results, production_schedule_path)

        # Step 5: Calculate metrics
        schuedule_duration = calculate_schedule(schedule_results)['schedule_makespan']
        logger.debug(f"{model_name} | Schedule {schuedule_duration}")
        #makespan_diff = compare_makespan(planed_schedules[TruthModel.__name__], schedules[model_name])
        #logger.debug(f"{model_name} | Makespan (Approach) {makespan} | Makespan-Diff (vs Truth) {makespan_diff['makespan']}")

        # Step 6: Create GanttCharts
        viz_output_path = GanttSchedule.create(schedule_results, args.plots, model_name)

    # Perform evaluation
    if not args.planned_mode:
        logger.debug("Performing evaluation.")
        # Compare all schedules with the reference planned schedule (TruthModel)
        results_run = extended_compare_all_schedules(schedules=schedules, reference_schedule=schedules[type(models[0]).__name__])
        results_run['seed'] = args.seed  # Add seed to the results
        results_run['instance'] = args.instances  # Add instance to the results
        results_run['priority_rule'] = args.priority_rule
        print_comparison_table(results_run)
        
        file_exists = os.path.exists(args.experiment_result_data)  # Check if the file already exists
        # Save results for this seed to the CSV file
        results_df = pd.DataFrame(results_run)
        logger.debug(f"results_df: {results_df}")
        results_df.to_csv(args.experiment_result_data, mode='a', header=not file_exists, index=False)
        logger.debug(f"Results for seed {args.seed} saved to {args.experiment_result_data}")
        return results_df
    
    if args.planned_mode:
        # Compare plannded schedules with the real schedule from the simulation
        #planned_run = extended_compare_schedules_pairwaise(planed_schedules, schedules)
        planned_run = extended_compare_all_schedules(schedules=planed_schedules, reference_schedule=schedules[type(models[1]).__name__])
        planned_run['seed'] = args.seed
        planned_run['instance'] = args.instances
        planned_run['priority_rule'] = args.priority_rule
        print_comparison_table(planned_run)
        file_exists = os.path.exists(args.experiment_result_data)  # Check if the file already exists
        planned_df = pd.DataFrame(planned_run)
        logger.debug(f"planned_df: {planned_df}")
        planned_df.to_csv(args.experiment_result_data, mode='a', header=not file_exists, index=False)
        logger.debug(f"Planned results for seed {args.seed} saved to {args.experiment_result_data}")
        # Save the planned results to the CSV file
        return planned_df

def create_folder_structure(args):
    # Generate a new file name with the current datetime in short format
    # Generate a timestamp for the experiment folder
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    main_experiment_folder = os.path.join(args.experiment_folder, timestamp)

    # Create the main experiment folder
    os.makedirs(main_experiment_folder, exist_ok=True)

    # Define subfolders and create them
    subfolders = ["observe", "results"]
    for subfolder in subfolders:
        os.makedirs(os.path.join(main_experiment_folder, subfolder), exist_ok=True)

    # Set paths for experiment result data and other outputs
    args.experiment_result_data = os.path.join(main_experiment_folder, f"experiment_results_{timestamp}.csv")
    os.makedirs(os.path.dirname(args.experiment_result_data), exist_ok=True)
    args.observed_data_path = os.path.join(main_experiment_folder, "observe")
    args.result_data = os.path.join(main_experiment_folder, "results")
    
    return args

def main():
    args = parse_arguments()

    # Create folder structure for the experiment
    args = create_folder_structure(args)
    # Write the header to the CSV file if it doesn't exist
    #with open(args.experiment_result_data, 'w') as f:
        #print(f'{args.experiment_result_data} created')

    args.seed = random.randint(0, 10000) if args.seed is None else args.seed
    results = pd.DataFrame()
    # Check if seed_iterator is provided
    if args.seed_iterator > 1:
        seed_end = args.seed + args.seed_iterator
        seed_range = range(args.seed, seed_end)
        for seed in seed_range:
            run_results = run_experiment(seed, args)
            if run_results is not None:
                results = pd.concat([results, run_results], ignore_index=True)
            logger.debug(f"Experiment completed for seed: {seed}")
        # build mean from results
            # Group by 'Model' and calculate descriptive statistics for each metric
        describe_table = results.groupby("Model").describe()

        # Display the descriptive statistics table
        describe_table

        # Extract the mean values for each variable
        results = describe_table.xs('mean', level=1, axis=1)

    else:
        # Run for a single seed
        run_results = run_experiment(args.seed, args)
        if run_results is not None:
                results = pd.concat([results, run_results], ignore_index=True)
    
    # Display the mean values
    for key, value in vars(args).items():
        print(f"{key} = {value}")
    print(tabulate(results, headers='keys', tablefmt='rounded_outline'))

def main_parallel():
    args = parse_arguments()
    #TODO Build folders for parallel runs
    #timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    #args.experiment_result_data = args.experiment_result_data.replace(".csv", f"_{timestamp}.csv")
    #os.makedirs(os.path.dirname(args.experiment_result_data), exist_ok=True)
    args = create_folder_structure(args)
    args.seed = random.randint(0, 10000) if args.seed is None else args.seed
    results = pd.DataFrame()

    if args.seed_iterator > 1:
        seed_end = args.seed + args.seed_iterator
        seed_range = range(args.seed, seed_end)
        with concurrent.futures.ProcessPoolExecutor() as executor:
            # Starte die Experimente parallel
            futures = [executor.submit(run_experiment, seed, args) for seed in seed_range]
            for future in concurrent.futures.as_completed(futures):
                run_results = future.result()
                if run_results is not None:
                    results = pd.concat([results, run_results], ignore_index=True)
        describe_table = results.groupby("Model").describe()
        results = describe_table.xs('mean', level=1, axis=1)
    else:
        run_results = run_experiment(args.seed, args)
        if run_results is not None:
            results = pd.concat([results, run_results], ignore_index=True)
    
    for key, value in vars(args).items():
        print(f"{key} = {value}")
    print(tabulate(results, headers='keys', tablefmt='rounded_outline'))
  
if __name__ == "__main__":
    args = parse_arguments()
    if args.parallel:
        main_parallel()
    else:
        main()