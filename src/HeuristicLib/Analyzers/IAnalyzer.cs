namespace HEAL.HeuristicLib.Analyzers;

public interface IAnalyzer<in TBuilder> {
  void AddToBuilder(TBuilder builder);
}
