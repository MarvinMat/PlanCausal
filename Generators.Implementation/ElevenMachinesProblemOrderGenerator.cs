using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Services;
using Generators.Abstraction;

namespace Generators.Implementation;

public class ElevenMachinesProblemOrderGenerator : IDataGenerator<ProductionOrder>
{
    private readonly List<WorkPlan> _plans;
    private List<double> _randomDistribution;
    private readonly Random _random = new(DateTime.Now.Millisecond);
    
    public ElevenMachinesProblemOrderGenerator(IEntityLoader<WorkPlan> workPlanProvider)
    {
        _plans = workPlanProvider.Load();
        _randomDistribution = new List<double>()
        {
            0.3, 0.1, 0.2, 0.1, 0.3
        };
    }   
    
    /// <inheritdoc/>
    public List<ProductionOrder> Generate(int amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than 0", nameof(amount));
        
        var orders = new List<ProductionOrder>();

        for (int i = 0; i < amount; i++)
        {
            var order = GenerateProductionOrder();
            orders.Add(order);
        }
        return orders;
    }

    private ProductionOrder GenerateProductionOrder()
    {
        GenerateCumulativeProbabilityDistribution();
        
        var randomProductIndexThreshold = _random.NextDouble();
        var randomProductIndex = _randomDistribution.FindIndex(item => item > randomProductIndexThreshold);
        var randomProduct = _plans[randomProductIndex];
        return new ProductionOrder() { Name = $"Order: {randomProduct.Name} ", Quantity = 1, WorkPlan = randomProduct };
    }
    /// <summary>
    /// Generates a cumulative probability distribution from a given list of probabilities.
    /// </summary>
    private void GenerateCumulativeProbabilityDistribution()
    {
        var cumulativeProbability = 0.0;
        _randomDistribution = _randomDistribution.Select(value => cumulativeProbability += value).ToList();
    }
}