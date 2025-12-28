using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Encodings.Vectors;

public record IntegerVectorEncoding(int Length, IntegerVector Minimum, IntegerVector Maximum) : Encoding<IntegerVector> {
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
