namespace HEAL.HeuristicLib.Operators.Analyzer;

public static class AnalysisFactory {
  public static BestMedianWorstAnalyzer<TGenotype> BestMedianWorstAnalyzer<TGenotype>() {
    return new BestMedianWorstAnalyzer<TGenotype>();
  }
}
