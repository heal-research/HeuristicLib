using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Encodings;

public record class IntegerVectorEncoding : Encoding<IntegerVector> {
  public int Length { get; }
  public IntegerVector Minimum { get; }
  public IntegerVector Maximum { get; }
  
  public IntegerVectorEncoding(int length, IntegerVector minimum, IntegerVector maximum) {
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

  public static implicit operator RealVectorEncoding(IntegerVectorEncoding integerVectorSpace) {
    return new RealVectorEncoding(integerVectorSpace.Length, integerVectorSpace.Minimum, integerVectorSpace.Maximum);
  }
  
  
}
