using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Crossovers.PermutationCrossovers;

public record OrderCrossover : SingleCandidateCrossover<Permutation, PermutationSearchSpace>
{
    public override Permutation CrossParents(Parents<Permutation> parents, IRandomNumberGenerator random, PermutationSearchSpace searchSpace)
    {
        var (parent1, parent2) = (parents.Parent1, parents.Parent2);
        return Cross(parent1, parent2, random);
    }

    public static Permutation Cross(Permutation parent1, Permutation parent2, IRandomNumberGenerator rng)
    {
        if (parent1.Count != parent2.Count)
        {
            throw new ArgumentException("Parent permutations must have the same length.");
        }

        var (start, end) = GetRandomBreakPoints(parent1.Count, rng);

        return Cross(parent1, parent2, start, end);
    }

    public static Permutation Cross(Permutation parent1, Permutation parent2, int start, int end)
    {
        if (parent1.Count != parent2.Count)
        {
            throw new ArgumentException("Parent permutations must have the same length.");
        }

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

    // public record BreakPoints(int First, int Second) {
    //   public static BreakPoints SingleRandom(int length, IRandomNumberGenerator rng) {
    //     int first = rng.Integer(1, length - 1);
    //     int second = rng.Integer(first + 1, length);
    //     return new BreakPoints(first, second);
    //   }
    //   public static IEnumerable<BreakPoints> MultipleRandom(int length, int count, IRandomNumberGenerator rng) {
    //     int maxPossiblePairs = length * (length - 1) / 2;
    //     count = Math.Min(count, maxPossiblePairs);
    //     var chosenBreakPoints = new HashSet<BreakPoints>();
    //     while (chosenBreakPoints.Count < count) {
    //       var breakPoints = SingleRandom(length, rng);
    //       if (chosenBreakPoints.Add(breakPoints)) {
    //         yield return breakPoints;
    //       }
    //     }
    //   }
    //   public static IEnumerable<BreakPoints> Exhaustive(int length) {
    //     for (int first = 1; first < length - 1; first++) {
    //       for (int second = first + 1; second < length; second++) {
    //         yield return new BreakPoints(first, second);
    //       }
    //     }
    //   }
    // }
}
