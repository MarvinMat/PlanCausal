namespace Generators.Abstraction;

public interface IDataGenerator<T>
{
    /// <summary>
    /// Generates a random list of type T containing the given amount of elements.
    /// </summary>
    /// <param name="amount">The expected amount of elements in the returned list.</param>
    /// <returns>A list containing <c>amount</c> elements.</returns>
    /// <exception cref="ArgumentException"><c>Amount</c> must be greater than 0</exception>
    public List<T> Generate(int amount);
}