using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.TestSupport.Random;

public sealed class SequenceRandomNumberGenerator(params double[] nextDoubles) : IRandomNumberGenerator
{
    private readonly Queue<double> doubles = new(nextDoubles);

    public double NextDouble()
    {
        if (doubles.Count == 0)
            throw new InvalidOperationException("No more predefined doubles are available.");

        return doubles.Dequeue();
    }

    public int NextInt() => 0;

    public IRandomNumberGenerator Fork(ulong forkKey) => this;
}
