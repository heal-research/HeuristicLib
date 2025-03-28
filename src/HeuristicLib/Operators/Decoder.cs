namespace HEAL.HeuristicLib.Operators;


public interface IDecoder<in TGenotype, out TPhenotype> {
  TPhenotype Decode(TGenotype genotype);
  //TGenotype Encode(TPhenotype phenotype);
}

public static class Decoder {
  public static IdentityDecoder<T> Identity<T>() => new IdentityDecoder<T>();
  public static CustomDecoder<TGenotype, TPhenotype> Create<TGenotype, TPhenotype>(Func<TGenotype, TPhenotype> decoderFunc) => new CustomDecoder<TGenotype, TPhenotype>(decoderFunc);
}

public class CustomDecoder<TGenotype, TSolution> : IDecoder<TGenotype, TSolution> {
  private readonly Func<TGenotype, TSolution> decoder;
  public CustomDecoder(Func<TGenotype, TSolution> decoder) {
    this.decoder = decoder;
  }
  public TSolution Decode(TGenotype genotype) => decoder(genotype);
}

public class IdentityDecoder<T> : IDecoder<T, T> {
  public T Decode(T genotype) => genotype;
  public T Encode(T phenotype) => phenotype;
}
