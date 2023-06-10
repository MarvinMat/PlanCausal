using Core.Abstraction.Domain.Resources;

namespace Core.Abstraction.Domain;

public interface IFeedback
{
    public Guid Id { get; init; }
    public List<IResource> Resources { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
}