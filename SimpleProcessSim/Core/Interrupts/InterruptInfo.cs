using ProcessSim.Abstraction;
using SimSharp;

namespace ProcessSim.Implementation.Core.Interrupts;

public class InterruptInfo
{
    public Func<ActiveObject<Simulation>, bool> Predicate { get; }
    public global::Core.Abstraction.Distribution<TimeSpan> Distribution { get; }
    public Func<ActiveObject<Simulation>, IEnumerable<Event>> InterruptAction { get;set; }
    
    

    public InterruptInfo(
        Func<ActiveObject<Simulation>, bool> predicate, global::Core.Abstraction.Distribution<TimeSpan> distribution,
        Func<ActiveObject<Simulation>, IEnumerable<Event>> interruptAction)
    {
        Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        Distribution = distribution ?? throw new ArgumentNullException(nameof(distribution));
        InterruptAction = interruptAction ?? throw new ArgumentNullException(nameof(interruptAction));
    }
}
