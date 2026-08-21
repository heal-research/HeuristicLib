namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

public static class IntegerVectorBuilder
{
    public static IntegerVector Create(ReadOnlySpan<int> elements)
      => IntegerVector.FromOwnedArray(elements.ToArray());
}
