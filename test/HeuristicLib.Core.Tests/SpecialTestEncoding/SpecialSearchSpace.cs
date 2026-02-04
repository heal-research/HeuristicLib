using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.SpecialTestEncoding;

public record SpecialSearchSpace : SearchSpace<SpecialGenotype>
{
  public override bool Contains(SpecialGenotype genotype) => true;
}
