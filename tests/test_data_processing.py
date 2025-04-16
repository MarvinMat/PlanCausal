import unittest
from modules.data_processing import generate_data
from modules.generators.jobs_data_generator import JobsDataGenerator
from modules.factory.Operation import Operation
from unittest.mock import patch, MagicMock

class TestGenerateData(unittest.TestCase):

    @patch('modules.data_processing.JobsDataGenerator')
    def test_generate_data_output_structure(self, MockJobsDataGenerator):
        """
        Test if generate_data returns the correct structure.
        """
        # Mock the JobsDataGenerator behavior
        mock_instance = MockJobsDataGenerator.return_value
        mock_instance.generate_jobs_data.return_value = [
            ['p1', 1, 'a1', 1, 30, 4],
            ['p1', 2, 'a2', 1, 45, 4],
        ]

        # Call the function
        operations, machines = generate_data(num_instances=2, seed=42)

        # Assert the structure of the returned data
        self.assertIsInstance(operations, list)
        self.assertTrue(all(isinstance(op, Operation) for op in operations))
        self.assertIsInstance(machines, list)
        self.assertTrue(all(isinstance(machine, list) for machine in machines))

    @patch('modules.data_processing.JobsDataGenerator')
    def test_generate_data_random_seed(self, MockJobsDataGenerator):
        """
        Test if generate_data produces consistent results with the same seed.
        """
        # Mock the JobsDataGenerator behavior
        mock_instance = MockJobsDataGenerator.return_value
        mock_instance.generate_jobs_data.return_value = [
            ['p1', 1, 'a1', 1, 30, 4],
            ['p1', 2, 'a2', 1, 45, 4],
        ]

        # Call the function twice with the same seed
        operations1, machines1 = generate_data(num_instances=2, seed=42)
        operations2, machines2 = generate_data(num_instances=2, seed=42)

        # Assert the results are consistent
        self.assertEqual(operations1, operations2)
        self.assertEqual(machines1, machines2)

    @patch('modules.data_processing.JobsDataGenerator')
    def test_generate_data_different_seeds(self, MockJobsDataGenerator):
        """
        Test if generate_data produces different results with different seeds.
        """
        # Mock the JobsDataGenerator behavior
        mock_instance = MockJobsDataGenerator.return_value
        mock_instance.generate_jobs_data.side_effect = [
            [['p1', 1, 'a1', 1, 30, 4], ['p1', 2, 'a2', 1, 45, 4]],
            [['p2', 1, 'a3', 1, 15, 3], ['p2', 2, 'a4', 1, 45, 3]],
        ]

        # Call the function with different seeds
        operations1, machines1 = generate_data(num_instances=2, seed=42)
        operations2, machines2 = generate_data(num_instances=2, seed=43)

        # Assert the results are different
        self.assertNotEqual(operations1, operations2)
        self.assertEqual(machines1, machines2)  # Machines should remain the same

if __name__ == '__main__':
    unittest.main()