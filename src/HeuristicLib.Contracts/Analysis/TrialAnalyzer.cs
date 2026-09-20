namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Creates one analyzer per experiment trial, bound to boundaries selected from that trial's algorithm.
/// </summary>
/// <remarks>
/// An analyzer holds one run's data, so a trial analyzer is a factory rather than a shared analyzer. The experiment
/// hands back the analyzer it created for each trial, and the caller reads its data directly.
/// </remarks>
public static class TrialAnalyzer
{
    public static TrialAnalyzer<TAlgorithm, TAnalyzer> Create<TAlgorithm, TAnalyzer>(
        Func<TAlgorithm, TAnalyzer> analyzerFactory)
        where TAnalyzer : IAnalyzer => new(analyzerFactory);
}

/// <summary>
/// A trial analyzer for one algorithm type, which is what an experiment run accepts.
/// </summary>
public abstract class TrialAnalyzer<TAlgorithm>
{
    private protected TrialAnalyzer()
    {
    }

    /// <summary>
    /// Creates the analyzer for one trial's algorithm.
    /// </summary>
    internal abstract IAnalyzer CreateFor(TAlgorithm algorithm);
}

public sealed class TrialAnalyzer<TAlgorithm, TAnalyzer> : TrialAnalyzer<TAlgorithm>
    where TAnalyzer : IAnalyzer
{
    internal Func<TAlgorithm, TAnalyzer> AnalyzerFactory { get; }

    internal TrialAnalyzer(Func<TAlgorithm, TAnalyzer> analyzerFactory)
    {
        AnalyzerFactory = analyzerFactory;
    }

    internal override IAnalyzer CreateFor(TAlgorithm algorithm) => AnalyzerFactory(algorithm);
}

/// <summary>
/// One trial and the analyzer that observed it.
/// </summary>
public sealed record TrialAnalysis<TTrial, TAnalyzer>(TTrial Trial, TAnalyzer Analyzer)
    where TAnalyzer : IAnalyzer;

public static class TrialAnalysis
{
    public static TrialAnalysis<TTrial, TAnalyzer> From<TTrial, TAnalyzer>(TTrial trial, TAnalyzer analyzer)
        where TAnalyzer : IAnalyzer => new(trial, analyzer);
}
