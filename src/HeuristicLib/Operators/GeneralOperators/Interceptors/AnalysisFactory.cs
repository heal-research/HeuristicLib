namespace HEAL.HeuristicLib.Operators.Interceptors;

public static class AnalysisFactory {
  public static BestMedianWorstAnalyzer<TGenotype> BestMedianWorstAnalyzer<TGenotype>() {
    return new BestMedianWorstAnalyzer<TGenotype>();
  }
}
