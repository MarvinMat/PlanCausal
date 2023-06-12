using Core.Abstraction.Domain.Processes;
using Core.Abstraction.Domain.Resources;

namespace Core.Abstraction.Domain;

public class ProductionFeedback : IFeedback
{
    public Guid Id { get; init; }
    public List<IResource> Resources { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsFinished { get; init; }
    public int DoneTotal { get; init; }
    public WorkOperation WorkOperation { get; init; }
    public TimeSpan LeadTime => CreatedAt - WorkOperation.EarliestStart;
    //TODO: implement done as a computed property - calculated by the parts manufactured divided by the total part count
    public double DoneInPercent { get; init; } = 0.0;
    public ProductionFeedback(WorkOperation workOperation)
    {
        Id = Guid.NewGuid();
        Resources = new List<IResource>();
        CreatedAt = DateTime.Now;
        WorkOperation = workOperation;
    }
}