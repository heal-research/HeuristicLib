using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.Permutations;

public record OrderCrossover : SingleCandidateCrossover<Permutation, PermutationSearchSpace>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace)
    {
        var (parent1, parent2) = (parents.Parent1, parents.Parent2);
        return Cross(parent1, parent2, random);
    }

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator rng)
    {
        var (start, end) = GetRandomBreakPoints(parent1.Count, rng);

        return Cross(parent1, parent2, start, end);
    }

    public static Permutation Cross(Permutation parent1, Permutation parent2, int start, int end)
    {
        if (start < 0 || end < 0 || start >= parent1.Count || end >= parent1.Count || start > end)
        {
            throw new ArgumentException("Start and end indices must be within the bounds of the permutation.");
        }

        var offspring = new int[parent1.Count];
        Cross(parent1, parent2, start, end, offspring);
        return Permutation.FromOwnedArray(offspring);
    }

    private static void Cross(Permutation parent1, Permutation parent2, int start, int end, Span<int> offspring)
    {
        Span<bool> contains = stackalloc bool[offspring.Length];
        contains.Clear();

        // 1. copy segment from parent1
        for (var i = start; i <= end; i++)
        {
            var value = parent1[i];
            offspring[i] = value;
            contains[value] = true;
        }

        // 2. fill the remaining positions from parent2, preserving cyclic order
        var currentIndex = (end + 1) % offspring.Length;
        for (var offset = 1; offset <= parent2.Count; offset++)
        {
            var value = parent2[(end + offset) % parent2.Count];
            if (contains[value])
            {
                continue;
            }

            offspring[currentIndex] = value;
            contains[value] = true;
            currentIndex = (currentIndex + 1) % offspring.Length;
        }
    }

    public static (int, int) GetRandomBreakPoints(int length, IRandomNumberGenerator rng)
    {
        if (length < 2)
        {
            throw new ArgumentException("Length must be at least 2 to have break points.");
        }

        var start = rng.NextInt(0, length - 1);
        var end = rng.NextInt(start + 1, length);

        return (start, end);
    }
}
