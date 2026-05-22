namespace HEAL.HeuristicLib.Genotypes.Vectors;

public static class IntegerVectorBuilder
{
    public static IntegerVector Create(ReadOnlySpan<int> elements)
      => IntegerVector.FromOwnedArray(elements.ToArray());
}
