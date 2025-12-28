namespace HEAL.HeuristicLib.Encodings;

public abstract record Encoding<TGenotype> : IEncoding<TGenotype> {
  public abstract bool Contains(TGenotype genotype);

  //public abstract bool IsSubspaceOf(IEncoding<TGenotype> other);
}
