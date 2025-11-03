namespace HEAL.HeuristicLib.Problems;

[Obsolete("Not sure if we need this.")]
public interface IDecoder<in TGenotype, out TPhenotype> {
  TPhenotype Decode(TGenotype genotype);
  //TGenotype Encode(TPhenotype phenotype);
}
