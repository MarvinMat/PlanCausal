using Core.Abstraction.Domain.Processes;

namespace Core.Abstraction.Domain.Customers;

public class Customer : ICustomer
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "Unnamed Customer";
    public List<CustomerOrder> Orders { get; set; } = new();
}