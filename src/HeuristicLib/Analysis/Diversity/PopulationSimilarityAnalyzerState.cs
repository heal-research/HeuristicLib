namespace HEAL.HeuristicLib.Analysis;

public class PopulationSimilarityAnalyzerState
{
    public List<double[,]> Similarities { get; } = [];
    public List<(double min, double avg, double max)> AvgSimilarities { get; } = [];
}
