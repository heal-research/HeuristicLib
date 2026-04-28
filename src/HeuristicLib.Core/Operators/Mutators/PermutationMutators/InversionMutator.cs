using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Operators.Mutators.PermutationMutators;

public record InversionMutator : SingleSolutionMutator<Permutation, PermutationSearchSpace>
{
  public override Permutation Mutate(Permutation parent, IRandomNumberGenerator random, PermutationSearchSpace searchSpace)
    => Mutate(parent, random, start: null, end: null);

  public static Permutation Mutate(Permutation parent, IRandomNumberGenerator random, int? start = null, int? end = null)
  {
    var rangeStart = start ?? random.NextInt(parent.Count);
    var rangeEnd = end ?? random.NextInt(rangeStart, parent.Count);
    var newElements = parent.ToArray();
    Array.Reverse(newElements, rangeStart, rangeEnd - rangeStart + 1);
    return new Permutation(newElements);
  }
}
