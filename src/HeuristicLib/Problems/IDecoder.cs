namespace HEAL.HeuristicLib.Problems;

public interface IDecoder<in TGenotype, out TPhenotype> {
  TPhenotype Decode(TGenotype genotype);
  //TGenotype Encode(TPhenotype phenotype);
}
