using SimSharp;

namespace ProcessSim.Abstraction;

public interface IFactor
{
    public string Name { get; init; }
    public string Description { get; init; }
    public Func<IEnumerable<Event>>? SimulateFactor { get; }

    public object GetCurrentValue();
}
