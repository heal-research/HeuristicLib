using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record RealVectorSearchSpace : SearchSpace<RealVector>
{
  public int Length { get; }
  public RealVector Minimum { get; }
  public RealVector Maximum { get; }

  public RealVectorSearchSpace(int length, double minimum, double maximum) : this(length, new RealVector(minimum), new RealVector(maximum)) { }

  public RealVectorSearchSpace(int length, RealVector minimum, RealVector maximum)
  {
    if (!RealVector.AreCompatible(length, minimum, maximum)) {
      throw new ArgumentException("Minimum and Maximum vector must be of length 1 or match the searchSpace length");
    }

    Length = length;
    Minimum = minimum;
    Maximum = maximum;
  }

  public override bool Contains(RealVector genotype)
  {
    return genotype.Count == Length
           && (genotype >= Minimum).All()
           && (genotype <= Maximum).All();
  }
}
