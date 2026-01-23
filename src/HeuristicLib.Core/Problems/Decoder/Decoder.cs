namespace HEAL.HeuristicLib.Problems.Decoder;

public static class Decoder
{
  public static IdentityDecoder<T> Identity<T>() => new();
  public static CustomDecoder<TGenotype, TPhenotype> Create<TGenotype, TPhenotype>(Func<TGenotype, TPhenotype> decoderFunc) => new(decoderFunc);
}
