using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.Evolutionary.ALPS;

public class AgedSearchSpace<TGenotype, TSearchSpace>(TSearchSpace innerEncoding) : ISearchSpace<AgedGenotype<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public TSearchSpace InnerEncoding { get; } = innerEncoding;

  public bool Contains(AgedGenotype<TGenotype> genotype) => InnerEncoding.Contains(genotype.InnerGenotype);
}
