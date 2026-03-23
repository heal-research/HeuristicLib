using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record PermutationSearchSpace(int Length) : SearchSpace<Permutation>
{
  //uniqueness of elements is guaranteed by Permutation class
  public override bool Contains(Permutation genotype) => genotype.Count == Length;

  public static implicit operator IntegerVectorSearchSpace(PermutationSearchSpace permutationSpace) =>
    new(permutationSpace.Length, 0, permutationSpace.Length - 1);
}
