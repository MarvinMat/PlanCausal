using Core.Abstraction.Domain.Processes;

namespace Core.Abstraction.Domain;

public interface ICustomer
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public List<CustomerOrder> Orders { get; set; }
}