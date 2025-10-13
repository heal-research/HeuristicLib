namespace HEAL.HeuristicLib.Optimization;

public interface IEncoding<in TGenotype> : IEncoding {
  bool Contains(TGenotype genotype);

  //bool IsSubspaceOf(IEncoding<TGenotype> other);
}

public abstract record Encoding<TGenotype> : IEncoding<TGenotype> {
  public abstract bool Contains(TGenotype genotype);

  //public abstract bool IsSubspaceOf(IEncoding<TGenotype> other);
}
