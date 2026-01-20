namespace HEAL.HeuristicLib.Algorithms;

public interface IBuildFeature<TSpec> where TSpec : class {
  TSpec Apply(TSpec spec);
}
