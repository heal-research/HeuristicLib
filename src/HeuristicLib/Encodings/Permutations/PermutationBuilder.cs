namespace HEAL.HeuristicLib.Encodings.Permutations;

public static class PermutationBuilder
{
    public static Permutation Create(ReadOnlySpan<int> elements) =>
        Permutation.FromOwnedArray(elements.ToArray());
}
