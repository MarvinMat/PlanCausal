import pandas as pd
import random

class JobsDataGenerator:
    def __init__(self, jobs_data):
        self.jobs_data = pd.DataFrame(jobs_data, columns=['product', 'sequence', 'operation', 'tool', 'duration', 'next'])
    
    def split_by_product(self):
        self.product_groups = self.jobs_data.groupby('product')
    
    def generate_jobs_data(self, num_instances, relation):
        """
        Generate new jobs data based on the specified number of instances and relation.
        
        Parameters:
        num_instances (int): The total number of new instances to generate.
        relation (dict): A dictionary defining the percentage of each product type to include.
                         For example: {'p1': 0.5, 'p2': 0.5}
        """
        self.split_by_product()
        
        # Create new jobs_data
        new_jobs_data = []
        product_types = list(relation.keys())
        
        for i in range(3, 3 + num_instances):
            product_type = random.choices(product_types, weights=[relation[p] for p in product_types])[0]
            product_data = self.product_groups.get_group(product_type).copy()
            product_data['product'] = f'job{i}'
            product_data['product_type'] = product_type
            new_jobs_data.append(product_data)
        
        new_jobs_data = pd.concat(new_jobs_data).reset_index(drop=True)
        return new_jobs_data.values.tolist()