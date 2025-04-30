using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.SearchSpaces;

public record class IntegerVectorSearchSpace : SearchSpace<IntegerVector> {
  public int Length { get; }
  public IntegerVector Minimum { get; }
  public IntegerVector Maximum { get; }
  
  public IntegerVectorSearchSpace(int length, IntegerVector minimum, IntegerVector maximum) {
    Length = length;
    Minimum = minimum;
    Maximum = maximum;
  }

  public override bool Contains(IntegerVector genotype) {
    throw new NotImplementedException();
    // return genotype.Count == Length
    //        && (genotype >= Minimum).All()
    //        && (genotype <= Maximum).All();
  }

  public static implicit operator RealVectorSearchSpace(IntegerVectorSearchSpace integerVectorSpace) {
    return new RealVectorSearchSpace(integerVectorSpace.Length, integerVectorSpace.Minimum, integerVectorSpace.Maximum);
  }
  
  
}
