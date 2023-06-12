using Core.Abstraction.Domain.Resources;

namespace Core.Abstraction.Domain;

public interface IFeedback
{
    public Guid Id { get; init; }
    public List<IResource> Resources { get; init; }
    public DateTime CreatedAt { get; init; }
    
}