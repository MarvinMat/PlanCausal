namespace Core.Abstraction
{
	/// <summary>
	/// Represents a function that samples a (random) distribution and returns a value of type T.
	/// </summary>
	/// <typeparam name="T">The type of the value to be returned.</typeparam>
	/// <returns>A (random) value of type T, following some distribution.</returns>
	public delegate T Distribution<T>();
}
