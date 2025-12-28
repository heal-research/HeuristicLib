using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedSearchSpace<TGenotype, TSearchSpace>(TSearchSpace innerSearchSpace) : ISearchSpace<AgedGenotype<TGenotype>>
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public TSearchSpace InnerSearchSpace { get; } = innerSearchSpace;

  public bool Contains(AgedGenotype<TGenotype> genotype) {
    return InnerSearchSpace.Contains(genotype.InnerGenotype);
  }
}
