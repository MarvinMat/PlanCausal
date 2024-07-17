import torch
import pyro
import pyro.distributions as dist
from pyro.infer import MCMC, NUTS

class CausalModelPyro:
    def model(self, has_lots_operations=None, is_shorter_than_15=None, one_working=None):
        # Priors for root nodes
        has_lots_operations = pyro.sample("has_lots_operations", dist.Bernoulli(0.5))
        one_working = pyro.sample("one_working", dist.Bernoulli(0.5))
        
        # Conditional probabilities
        is_shorter_than_15_probs = torch.tensor([[0.7, 0.2], [0.3, 0.8]])
        is_shorter_than_15 = pyro.sample("is_shorter_than_15", dist.Bernoulli(is_shorter_than_15_probs[has_lots_operations.long()]))

        # Combining factors for duration using tensor operations
        duration_factor = torch.tensor([1.1, 0.9])[is_shorter_than_15.long()] * torch.tensor([1.0, 1.1])[one_working.long()]
        duration = pyro.sample("duration", dist.Normal(duration_factor, 0.1))  # Normal approximation

        return duration

    def infer_duration(self, has_lots_operations, is_shorter_than_15, one_working):
        nuts_kernel = NUTS(self.model)
        mcmc = MCMC(nuts_kernel, num_samples=500, warmup_steps=200)
        mcmc.run(has_lots_operations=has_lots_operations, is_shorter_than_15=is_shorter_than_15, one_working=one_working)
        samples = mcmc.get_samples()
        return samples['duration'].mean()

# Set PyTorch to use GPU if available
if torch.cuda.is_available():
    torch.set_default_tensor_type('torch.cuda.FloatTensor')

# Example usage
model = CausalModelPyro()
estimated_duration = model.infer_duration(torch.tensor(1.), torch.tensor(1.), torch.tensor(0.))
print("Inferred duration factor:", estimated_duration.item())