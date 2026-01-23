namespace HEAL.HeuristicLib.Problems.Decoder;

public class IdentityDecoder<T> : IDecoder<T, T>
{
  public T Decode(T genotype) => genotype;
  public T Encode(T phenotype) => phenotype;
}
