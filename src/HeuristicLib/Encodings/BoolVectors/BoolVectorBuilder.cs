namespace HEAL.HeuristicLib.Encodings.BoolVectors;

public static class BoolVectorBuilder
{
    public static BoolVector Create(ReadOnlySpan<bool> elements)
      => BoolVector.FromOwnedArray(elements.ToArray());
}
