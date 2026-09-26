using HEAL.HeuristicLib.Experiments;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>Creates a module factory that gives each experiment trial its own analyzer.</summary>
public static class TrialAnalyzer
{
    public static TrialModule<TAlgorithm, TAnalyzer> Create<TAlgorithm, TAnalyzer>(
        Func<TAlgorithm, TAnalyzer> analyzerFactory)
        where TAnalyzer : IAnalyzer => TrialModule.Create(analyzerFactory);
}

