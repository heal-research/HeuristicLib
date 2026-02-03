namespace HEAL.HeuristicLib.Optimization;

public interface IEncoding { }

public interface IEncoding<in TGenotype> : IEncoding {
  bool Contains(TGenotype genotype);

  //bool IsSubspaceOf(IEncoding<TGenotype> other);
}
