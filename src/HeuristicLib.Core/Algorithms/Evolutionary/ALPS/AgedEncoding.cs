using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms.ALPS;

public class AgedSearchSpace<TGenotype, TEncoding>(TEncoding innerEncoding) : IEncoding<AgedGenotype<TGenotype>>
  where TEncoding : class, IEncoding<TGenotype> {
  public TEncoding InnerEncoding { get; } = innerEncoding;

  public bool Contains(AgedGenotype<TGenotype> genotype) {
    return InnerEncoding.Contains(genotype.InnerGenotype);
  }
}
