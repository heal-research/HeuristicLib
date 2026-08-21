namespace HEAL.HeuristicLib.Encodings.RealVectors;

public static class RealVectorBuilder
{
    public static RealVector Create(ReadOnlySpan<double> elements)
      => RealVector.FromOwnedArray(elements.ToArray());
}
