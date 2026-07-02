using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.TestSupport.SpecialTestEncoding;

public record SpecialSearchSpace : SearchSpace<SpecialGenotype>
{
    public override bool Contains(SpecialGenotype candidate) => true;
}
