namespace HEAL.HeuristicLib.Problems;

public static class Decoder {
  public static IdentityDecoder<T> Identity<T>() => new IdentityDecoder<T>();
  public static CustomDecoder<TGenotype, TPhenotype> Create<TGenotype, TPhenotype>(Func<TGenotype, TPhenotype> decoderFunc) => new CustomDecoder<TGenotype, TPhenotype>(decoderFunc);
}
