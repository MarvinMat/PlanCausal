using Core.Abstraction;
using Core.Abstraction.Domain.Processes;
using Generators.Abstraction;

namespace Generators.Implementation;

public class OrderGenerator : IDataGenerator<ProductionOrder>
{
    public Distribution<int> QuantityDistribution { get; set; }
    public Distribution<WorkPlan> ProductDistribution { get; set; }
 
    public OrderGenerator(Distribution<WorkPlan> productDistribution, Distribution<int> quantityDistribution)
    {
        ProductDistribution = productDistribution;
        QuantityDistribution = quantityDistribution;
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
        var randomProduct = GetRandomProduct();
        var randomQuantity = GetRandomQuantity();

        return new ProductionOrder() { Name = $"Order: {randomProduct.Name}", Quantity = randomQuantity, WorkPlan = randomProduct };
    }

    private WorkPlan GetRandomProduct()
    {
        return ProductDistribution();
    }

    private int GetRandomQuantity()
    {
        var randomQuantity = QuantityDistribution();
        if (randomQuantity <= 0) throw new ArgumentException("Quantity must be greater than 0. The given QuantityDistribution returned a value less than or equal to 0");
        return randomQuantity;
    }
}