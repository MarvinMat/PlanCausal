from modules.generators.jobs_data_generator import JobsDataGenerator
from modules.factory.Operation import Operation
import os
from typing import List
from collections import defaultdict
import random
from tabulate import tabulate
import pandas as pd


class ProductionGenerator:
    def __init__(self):
        self.template_jobs_data = []
        self.job_data = []
        
    def generate_data_static(self, num_instances = 150, seed = 1):
        """
        Generate data from a template.
        """
        
        random.seed(seed)
        
        # Beispielhafte Datenstruktur
        # Produkt, Arbeitsgang, Maschinengruppe, Tool, geplante Dauer, Nachfolger
        self.template_jobs_data = [
            ['p1', 1, 'a1', 1, 15, 2],
            ['p1', 2, 'a3', 1, 10, -1],
            ['p2', 1, 'a2', 1, 15, 2],       
            ['p2', 2, 'a3', 2, 10, -1],
            ['p3', 1, 'a1', 2, 15, 2],
            ['p3', 2, 'a3', 2, 10, -1],
            ['p4', 1, 'a2', 2, 15, 2],       
            ['p4', 2, 'a3', 1, 10, -1],
        ]

        generator = JobsDataGenerator(self.template_jobs_data)
        relation = {'p1': 0.25, 'p2': 0.25 , 'p3': 0.25, 'p4': 0.25}  # Relation of each product type

        self.job_data = generator.generate_jobs_data(num_instances, relation)

        machines = [
            [f'a{i}', 1, list(range(1, 2 + 1))]
            for i in range(1, 3 + 1)
        ]
        
        operations = self.prepare_data()
        return operations, machines
    
    def generate_data_dynamic(self,
        amount_products = 10,
        product_types_relation = None,
        avg_operations=4, 
        avg_duration=30,
        machine_groups=4, 
        machine_instances=1,
        tools_per_machine=3,
        num_instances=150, 
        distribution='normal',
        seed=1,
    ):
       
        """
        Generate data dynamically based on input parameters.
        """
        # Generate product types relation based on the specified distribution
        # If product_types_relation is None, generate it based on the distribution 
        # Example: {'p1': 0.5, 'p2': 0.5}
        # If distribution is 'equal', all products have equal weight
        # If distribution is 'normal', generate weights with higher values in the middle
        if product_types_relation is None:
            if distribution == 'equal':
                product_types_relation = {f'p{i}': 1 / amount_products for i in range(1, amount_products + 1)}
            elif distribution == 'normal':
                weights = []
                for i in range(amount_products):
                    # Generate weights with higher values in the middle
                    position_factor = abs((amount_products // 2) - i) + 1
                    weight = random.gauss(1 / position_factor, 0.2 / position_factor)
                    while weight < 0:  # Regenerate if the weight is negative
                        weight = random.gauss(1 / position_factor, 0.2 / position_factor)
                    weights.append(weight)
                total_weight = sum(weights)
                product_types_relation = {f'p{i}': weight / total_weight for i, weight in enumerate(weights, 1)}
            else:
                raise ValueError("Invalid distribution type or missing custom distribution.")

        # Set the random seed for reproducibility
        random.seed(seed)

        # Generate a dynamic template_jobs_data
        # The template_jobs_data is a list of lists, where each inner list represents a job
        # Each job contains the following fields: product, operation, machinegroup, tool, duration, next
        # product: The product type (e.g., 'p1', 'p2', ...)
        # sequence: The sequence number of the operation
        # machinegroup: The machine group (e.g., 'a1', 'a2', ...)
        # tool: The tool number (e.g., 1, 2, ...)
        # duration: The duration of the operation
        # next: The successor operation (e.g., 1, 2, ... or -1 if no successor)
        for product_type in product_types_relation.items():
            num_operations = max(1, int(random.gauss(avg_operations, avg_operations * 0.2)))
            for op in range(1, num_operations + 1):
                product = product_type[0]
                machine_group = f'a{random.randint(1, machine_groups)}'
                tool = random.randint(1, tools_per_machine)
                duration = max(1, int(random.gauss(avg_duration, avg_duration * 0.5)))
                successor = max(op + 1 , min(num_operations, int(random.gauss(num_operations, num_operations * 0.1))) ) if op < num_operations else -1
                self.template_jobs_data.append([product, op, machine_group, tool, duration, successor])

        # Generate jobs data using the dynamic template
        self.job_data = JobsDataGenerator(self.template_jobs_data).generate_jobs_data(num_instances, product_types_relation)

        # Define machine pools dynamically
        # Create multiple machines per machine group
        machines = []
        for i in range(1, machine_groups + 1):
            machines.append([f'a{i}', machine_instances, list(range(1, tools_per_machine + 1))])

        operations = self.prepare_data()
        return operations, machines

    def load_data(self, data, input_path):
        """
        Load data to a CSV file.
        """
        data.from_csv(input_path, index=False)

    def save_data(self, data, output_file):
        """
        Save data to a CSV file.
        """
        #os.makedirs(output_dir, exist_ok=True)
        #output_file = os.path.join(output_dir, f'schedule_{model_name}.csv')
        data.to_csv(output_file, index=False)
        return output_file

    def prepare_data(self) -> List[Operation]:
        """
        Convert raw jobs data into Operation objects.
        """
        return [Operation(*data) for data in self.job_data]

    def job_data_metric(self):
        """
        Calculate and display metrics about the production system setup.
        This includes average operation duration, total load per machine group,
        and product mix.
        
        The metrics are displayed in a tabular format and also as an ASCII diagram
        for the product mix.
        The function returns a dictionary containing the calculated metrics.
        """
        # Group data by products and machine groups
        product_durations = defaultdict(list)
        machine_group_durations = defaultdict(list)
        product_ops_count = defaultdict(int)
        machine_group_ops = defaultdict(int)
        product_mix = defaultdict(int)

        # Collect data
        for row in self.template_jobs_data:
            product, _, machine_group, _, duration, _ = row
            product_durations[product].append(duration)
            machine_group_durations[machine_group].append(duration)
            product_ops_count[product] += 1
            machine_group_ops[machine_group] += 1

        # Count product mix
        seen_jobs = set()
        for row in self.job_data:
            job, *_ , product = row
            if job not in seen_jobs:
                seen_jobs.add(job)
                product_mix[product] += 1

        # Calculate metrics
        product_metrics = {
            product: {
                'avg_duration': round(sum(durations) / len(durations), 1),
                'total_duration': sum(durations),
                'operations': product_ops_count[product]
            }
            for product, durations in product_durations.items()
        }

        machine_metrics = {
            machine: {
                'avg_duration': round(sum(durations) / len(durations), 1),
                'total_load': sum(durations),
                'operations': machine_group_ops[machine]
            }
            for machine, durations in machine_group_durations.items()
        }

        # Create summary tables
        product_table = [
            [
                product,
                metrics['operations'],
                metrics['avg_duration'],
                metrics['total_duration']
            ]
            for product, metrics in product_metrics.items()
        ]

        machine_table = [
            [
                machine,
                metrics['operations'],
                metrics['avg_duration'],
                metrics['total_load']
            ]
            for machine, metrics in machine_metrics.items()
        ]

        # Print formatted tables
        print("\nProduct Metrics:")
        print(tabulate(
            product_table,
            headers=['Product', 'Operations', 'Avg Duration', 'Total Duration'],
            tablefmt='rounded_outline'
        ))

        print("\nMachine Group Metrics:")
        print(tabulate(
            machine_table,
            headers=['Machine Group', 'Operations', 'Avg Duration', 'Total Load'],
            tablefmt='rounded_outline'
        ))

        # Calculate and print overall metrics
        all_durations = [d for durations in product_durations.values() for d in durations]
        total_ops = sum(product_ops_count.values())
        
        print("\nOverall Metrics:")
        overall_metrics = [
            ['Total Operations', total_ops],
            ['Average Operation Duration', round(sum(all_durations) / len(all_durations), 1)],
            ['Total Processing Time', sum(all_durations)]
        ]
        print(tabulate(overall_metrics, tablefmt='rounded_outline'))

        # Display product mix as ASCII diagram
        #TODO: Make sure, that the 'p' is exluded from sorting
        print("\nProduct Mix:")
        max_product_name_length = max(len(product) for product in product_mix.keys())
        for product, count in sorted(product_mix.items()):  # Sort by product name
            bar = '#' * count
            print(f"{product.ljust(max_product_name_length)} | {bar} ({count})")

        return {
            'product_metrics': product_metrics,
            'machine_metrics': machine_metrics,
            'total_ops': total_ops,
            'avg_duration': sum(all_durations) / len(all_durations),
            'product_mix': product_mix
        }
