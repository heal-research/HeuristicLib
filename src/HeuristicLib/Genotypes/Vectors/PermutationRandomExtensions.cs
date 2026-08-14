using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public static class PermutationRandomExtensions
{
    extension(IRandomNumberGenerator random)
    {
        public Permutation NextPermutation(int length)
        {
            var elements = Enumerable.Range(0, length).ToArray();
            for (var i = elements.Length - 1; i > 0; i--)
            {
                var j = random.NextInt(i + 1);
                (elements[i], elements[j]) = (elements[j], elements[i]);
            }

            return Permutation.FromOwnedArray(elements);
        }
    }

    extension(Permutation permutation)
    {
        public Permutation SwapRandomIndices(IRandomNumberGenerator random)
        {
            if (permutation.Count < 2)
            {
                return permutation;
            }

            var index1 = random.NextInt(permutation.Count);
            var index2 = random.NextInt(permutation.Count);
            return permutation.Swap(index1, index2);
        }

        public Permutation InvertRandomRange(IRandomNumberGenerator random)
        {
            if (permutation.Count < 2)
            {
                return permutation;
            }

            var start = random.NextInt(permutation.Count);
            var end = random.NextInt(start, permutation.Count);
            return permutation.Invert(start, end);
        }
    }
}
