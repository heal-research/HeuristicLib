using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

public enum MoveMode
{
    Enumerate,
    RandomWithRepetition,
    RandomWithoutRepetition
}

public record IntegerVectorConstructionNeighborhood(MoveMode mode, int maxSize = int.MaxValue) : IntegerVectorNeighborhood<int>
{
    public static Parameter<IntegerVectorConstructionNeighborhood, MoveMode> ModeParameter { get; } =
        new(nameof(mode), static neighborhood => neighborhood.mode, static (neighborhood, value) => neighborhood with { mode = value });

    public static Parameter<IntegerVectorConstructionNeighborhood, int> MaxSizeParameter { get; } =
        new(nameof(maxSize), static neighborhood => neighborhood.maxSize, static (neighborhood, value) => neighborhood with { maxSize = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [ModeParameter, MaxSizeParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    public override IEnumerable<int> Moves(IntegerVector genotype, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace, IProblem<IntegerVector, IntegerVectorSearchSpace> problem)
    {
        if (genotype.Count >= searchSpace.Length)
            yield break;

        var l = searchSpace.GetMinimum(genotype.Count);
        var u = searchSpace.GetMaximum(genotype.Count);
        switch (mode)
        {
            case MoveMode.Enumerate:
                foreach (var i in Enumerable.Range(l, Math.Min(u - l, maxSize)))
                    yield return i;
                break;
            case MoveMode.RandomWithRepetition:
                for (var i = 0; i < maxSize; i++)
                    yield return random.NextInt(l, u, true);
                break;
            case MoveMode.RandomWithoutRepetition:
                if (u - l > maxSize / 2) //rejection sampling greater than 50%
                {
                    var seen = new HashSet<int>();
                    for (var j = 0; j < maxSize; j++)
                    {
                        int i;
                        do
                        {
                            i = random.NextInt(l, u, true);
                        } while (seen.Contains(i));

                        seen.Add(i);
                        yield return i;
                    }
                }
                else
                {
                    foreach (var i in Enumerable.Range(l, Math.Min(u - l, maxSize)).Shuffle(random).Take(maxSize))
                    {
                        yield return i;
                    }
                }

                break;
            default:
                throw new InvalidOperationException();
        }
    }

    public override IntegerVector Apply(IntegerVector genotype, int move, IRandomNumberGenerator random, IntegerVectorSearchSpace searchSpace, IProblem<IntegerVector, IntegerVectorSearchSpace> problem) =>
        new(genotype.Append(move));
}
