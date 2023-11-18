namespace Core.Abstraction
{
	/// <summary>
	/// Represents a function that samples a (random) distribution and returns a value of type T.
	/// </summary>
	/// <typeparam name="T">The type of the value to be returned.</typeparam>
	/// <returns>A (random) value of type T, following some distribution.</returns>
	public delegate T Distribution<T>();

	public abstract class Distributions
	{
		public static Distribution<T> ConstantDistribution<T>(T value)
		{
			return () => value;
		}

		/// <summary>
		/// Generates a distribution that follows the given probabilities and returns the corresponding value. 
		/// The first value in the probabilities list is the probability for the first value in the values list.
		/// </summary>
		/// <typeparam name="T">The type of the value to be returned.</typeparam>
		/// <param name="values">The possible values that the distribution can return.</param>
		/// <param name="probabilities">The probabilities of each value occurring.</param>
		/// <returns>A distribution that follows the given probabilities.</returns>
		public static Distribution<T> DiscreteDistribution<T>(List<T> values, List<double> probabilities)
		{
			if (values == null) throw new ArgumentNullException(nameof(values));
			if (probabilities == null) throw new ArgumentNullException(nameof(probabilities));
			if (values.Count == 0) throw new ArgumentException("The values list must not be empty.");
			if (probabilities.Count != values.Count) throw new ArgumentException("There must be the same number of values and probabilities given.");
			if (Math.Abs(probabilities.Sum() - 1.0) > 0.000001) throw new ArgumentException("The given probabilities must sum up to 1.");

			var valueProbabilityTuples = values.Zip(probabilities);

			return () =>
			{
				var rnd = new Random().NextDouble();
				var sum = 0.0;
				foreach (var (value, probability) in valueProbabilityTuples)
				{
					sum += probability;
					if (rnd < sum)
						return value;
				}
				throw new ArgumentException("The given probabilities must sum up to 1.");
			};
        }
	}
}
