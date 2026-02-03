using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record BoolVectorSearchSpace(int Length) : SearchSpace<BoolVector>
{
  public override bool Contains(BoolVector genotype) => genotype.Count == Length;
}
