namespace HEAL.HeuristicLib.Genotypes.Vectors;

public static class BoolVectorBuilder
{
    public static BoolVector Create(ReadOnlySpan<bool> elements)
      => BoolVector.FromOwnedArray(elements.ToArray());
}
