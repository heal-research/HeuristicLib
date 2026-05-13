using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Problems.MetaOptimization;

public record EmptySearchSpace : ISearchSpace<EmptyGenotype>
{
  public static readonly EmptySearchSpace Instance = new();
  private EmptySearchSpace() { }
  public bool Contains(EmptyGenotype genotype) => true;
}
