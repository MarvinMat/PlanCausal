using ProcessSim.Abstraction;
using SimSharp;

namespace ProcessSim.Implementation.Core.InfluencingFactors;

public class InfluencingFactor<T> : IFactor
{
    public string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public T CurrentValue { get; private set; }
    public Func<IEnumerable<Event>>? SimulateFactor { get; }

    /// <summary>
    /// Delegate to simulate the influence factor. Should return an IEnumerable of SimSharp.Event.
    /// </summary>
    /// <example>
    ///     <code>
    ///         while(true) 
    ///         {
    ///             setCurrentValue(new Random().Next(0, 100));
    ///             yield return new Timeout(TimeSpan.FromSeconds(100));
    ///         }
    ///     </code>
    /// </example>
    public delegate IEnumerable<Event> SimulateInfluenceFactor(Action<T> setCurrentValue);

    /// <summary>
    /// Create an influencing factor object.
    /// </summary>
    /// <param name="name">The name of the influencing factor</param>
    /// <param name="simulateFactor">The function to simulate the influence factor. If null is provided, no simulating will take place and the current value will stay constant.</param>
    /// <param name="currentValue">The initial value of the influencing factor</param>
    /// <exception cref="ArgumentNullException"></exception>
    public InfluencingFactor(string name, SimulateInfluenceFactor? simulateFactor, T currentValue)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(name);
        if (currentValue is null) throw new ArgumentNullException(nameof(currentValue));

        CurrentValue = currentValue;
        Name = name;
        if (simulateFactor is not null)
            SimulateFactor = () => simulateFactor((value) => CurrentValue = value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not InfluencingFactor<T> other) return false;
        if (other.Name != Name) return false;
        if (other.Description != Description) return false;
        return other.CurrentValue is not null && other.CurrentValue.Equals(CurrentValue);
    }

    public override int GetHashCode()
    {
        unchecked // allows overflow without throwing an exception, which is fine for hashing
        {
            var hash = 17; // start with a prime number
            hash = hash * 23 + (Name?.GetHashCode() ?? 0); // multiply by a prime number and add the hash of the Name
            hash = hash * 23 + (Description?.GetHashCode() ?? 0); // do the same for Description
            return hash;
        }
    }

    public object GetCurrentValue()
    {
        return CurrentValue;
    }
}