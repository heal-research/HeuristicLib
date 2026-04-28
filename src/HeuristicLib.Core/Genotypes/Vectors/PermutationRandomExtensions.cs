using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Genotypes.Vectors;

public static class PermutationRandomExtensions
{
  extension(IRandomNumberGenerator random)
  {
    public Permutation NextPermutation(int length)
    {
      var elements = Enumerable.Range(0, length).ToArray();
      for (var i = elements.Length - 1; i > 0; i--) {
        var j = random.NextInt(i + 1);
        (elements[i], elements[j]) = (elements[j], elements[i]);
      }

      return Permutation.FromMemory(elements);
    }

    public Permutation Swap(Permutation permutation)
    {
      var length = permutation.Count;
      var index1 = random.NextInt(length);
      var index2 = random.NextInt(length);

      var newElements = permutation.ToArray();
      (newElements[index1], newElements[index2]) = (newElements[index2], newElements[index1]);
      return Permutation.FromMemory(newElements);
    }
  }
}