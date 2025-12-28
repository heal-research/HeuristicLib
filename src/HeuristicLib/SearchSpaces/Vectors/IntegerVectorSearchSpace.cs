using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record IntegerVectorSearchSpace(int Length, IntegerVector Minimum, IntegerVector Maximum) : SearchSpace<IntegerVector> {
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
