namespace HEAL.HeuristicLib.Problems;

public class CustomDecoder<TGenotype, TISolution>(Func<TGenotype, TISolution> decoder) : IDecoder<TGenotype, TISolution> {
  public TISolution Decode(TGenotype genotype) => decoder(genotype);
}
