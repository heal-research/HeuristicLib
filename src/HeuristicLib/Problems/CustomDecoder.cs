namespace HEAL.HeuristicLib.Problems;

public class CustomDecoder<TGenotype, TSolution>(Func<TGenotype, TSolution> decoder) : IDecoder<TGenotype, TSolution> {
  public TSolution Decode(TGenotype genotype) => decoder(genotype);
}
