namespace HEAL.HeuristicLib.Analysis.GenealogyAnalysis;

public record RankAnalysisResult<T>(GenealogyGraph<T> Graph, IReadOnlyList<IReadOnlyList<double>> Ranks)
    where T : notnull;
