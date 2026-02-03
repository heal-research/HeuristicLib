using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems.Special;

public record SpecialSearchSpace : SearchSpace<SpecialGenotype>
{
  public override bool Contains(SpecialGenotype genotype) => true;
}
