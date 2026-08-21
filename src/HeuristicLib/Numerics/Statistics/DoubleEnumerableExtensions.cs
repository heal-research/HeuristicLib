namespace HEAL.HeuristicLib.Numerics;

public static class DoubleEnumerableExtensions
{
    extension(IEnumerable<double> values)
    {
        public double Range()
        {
            using var enumerator = values.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new InvalidOperationException("Sequence contains no elements.");

            var minimum = enumerator.Current;
            var maximum = enumerator.Current;

            while (enumerator.MoveNext())
            {
                minimum = Math.Min(minimum, enumerator.Current);
                maximum = Math.Max(maximum, enumerator.Current);
            }

            return maximum - minimum;
        }
    }
}
