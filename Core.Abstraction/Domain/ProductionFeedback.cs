using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;

namespace Core.Abstraction.Domain;

public class ProductionFeedback : IFeedback
{
    public Guid Id { get; init; }
    public List<IResource> Resources { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsFinished { get; set; }
    public int ProducedPartsCount { get; set; }
    public WorkOperation WorkOperation { get; set; }
    
    public ProductionFeedback(WorkOperation workOperation)
    {
        Id = Guid.NewGuid();
        Resources = new List<IResource>();
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
        IsFinished = false;
        ProducedPartsCount = 0;
        WorkOperation = workOperation;
    }
}