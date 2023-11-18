using Core.Abstraction;
using Core.Abstraction.Domain.Customers;
using Core.Abstraction.Domain.Processes;
using Generators.Abstraction;

namespace Generators.Implementation;

public class CustomerOrderGenerator : IDataGenerator<CustomerOrder>
{
    public Distribution<Customer>? CustomerDistribution { get; init; }

    // the distribution of how many production orders a customer will order
    public Distribution<int>? AmountDistribution { get; init; }

    public IDataGenerator<ProductionOrder>? OrderGenerator { get; init; }

    public List<CustomerOrder> Generate(int amount)
    {
        if (amount < 0) throw new ArgumentException("Amount of customers must be greater than 0", nameof(amount));

        var customerOrders = new List<CustomerOrder>();

        for (var i = 0; i < amount; i++)
        {
            var customerOrder = GenerateCustomerOrder();
            customerOrders.Add(customerOrder);
        }

        return customerOrders;
    }

    private CustomerOrder GenerateCustomerOrder()
    {
        if(CustomerDistribution is null) throw new ArgumentException("Customer distribution is null", nameof(CustomerDistribution));
        
        var customer = CustomerDistribution();
        var customerOrder = GenerateOrderForCustomer(customer);
        
        return customerOrder;
    }

    private CustomerOrder GenerateOrderForCustomer(Customer customer)
    {
        if (OrderGenerator is null) throw new ArgumentException("Order generator is null", nameof(OrderGenerator));
        if (AmountDistribution is null) throw new ArgumentException("Amount distribution is null", nameof(AmountDistribution));
        
        var order = new CustomerOrder{CustomerId = customer.Id, OrderReceivedDate = DateTime.Now};
        var amountOfOrders = AmountDistribution();
        order.ProductionOrders = OrderGenerator.Generate(amountOfOrders);
        return order;
    }
}