from modules.generators.jobs_data_generator import JobsDataGenerator
from modules.factory.Operation import Operation
import os
from typing import List
import random

def generate_data(num_instances = 150, seed = 1):
    """
    Generate data from a template.
    """
    
    random.seed(seed)
    
    # Beispielhafte Datenstruktur
    # Produkt, Arbeitsgang, Maschinengruppe, Tool, geplante Dauer, Nachfolger
    template_jobs_data = [
        ['p1', 1, 'a1', 1, 30, 4],
        ['p1', 2, 'a2', 1, 45, 4],
        ['p1', 3, 'a1', 2, 15, 4],
        ['p1', 4, 'a3', 1, 15, -1],
        ['p2', 1, 'a1', 1, 15, 3],
        ['p2', 2, 'a4', 2, 45, 3],
        ['p2', 3, 'a3', 2, 15, 5],
        ['p2', 4, 'a2', 1, 30, 5],
        ['p2', 5, 'a4', 1, 15, -1],
    ]

    generator = JobsDataGenerator(template_jobs_data)
    relation = {'p1': 0.5, 'p2': 0.5}  # Relation of each product type

    jobs_data = generator.generate_jobs_data(num_instances, relation)

    # Maschinenpools definieren
    # id, number, tools 
    machines = [
        ['a1', 1, [1,2,3]],
        ['a2', 1, [1,2,3]],
        ['a3', 1, [1,2,3]],
        ['a4', 1, [1,2,3]],
    #    ['a5', 1, [1,2,3]],
    #    ['a6', 1, [1,2,3]],
    ]
    operations = prepare_data(jobs_data=jobs_data)
    return operations, machines

def load_data(data, input_path):
    """
    Load data to a CSV file.
    """
    data.from_csv(input_path, index=False)

def save_data(data, output_dir, model_name):
    """
    Save data to a CSV file.
    """
    os.makedirs(output_dir, exist_ok=True)
    output_file = os.path.join(output_dir, f'schedule_{model_name}.csv')
    data.to_csv(output_file, index=False)
    return output_file

def prepare_data(jobs_data) -> List[Operation]:
    """
    Convert raw jobs data into Operation objects.
    """
    return [Operation(*data) for data in jobs_data]
