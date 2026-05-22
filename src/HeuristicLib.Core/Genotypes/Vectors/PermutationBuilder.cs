namespace HEAL.HeuristicLib.Genotypes.Vectors;

public static class PermutationBuilder
{
    public static Permutation Create(ReadOnlySpan<int> elements)
      => Permutation.FromOwnedArray(elements.ToArray());
}
