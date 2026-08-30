using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Creates one analyzer per experiment trial, bound to an operator selected from that trial's algorithm.
/// </summary>
/// <remarks>
/// An analyzer holds one run's data, so a trial analyzer is a factory rather than a shared analyzer. The experiment
/// hands back the analyzer it created for each trial, and the caller reads its data directly.
/// </remarks>
public static class TrialAnalyzer
{
    public static TrialAnalyzer<TAlgorithm, TOperator, TAnalyzer> Create<TAlgorithm, TOperator, TAnalyzer>(
        Func<TAlgorithm, TOperator> selector,
        Func<TOperator, TAnalyzer> analyzerFactory)
        where TAnalyzer : IAnalyzer => new(selector, analyzerFactory);
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
    internal abstract IExecutionHook CreateFor(TAlgorithm algorithm);
}

public sealed class TrialAnalyzer<TAlgorithm, TOperator, TAnalyzer> : TrialAnalyzer<TAlgorithm>
    where TAnalyzer : IAnalyzer
{
    internal Func<TAlgorithm, TOperator> Selector { get; }

    internal Func<TOperator, TAnalyzer> AnalyzerFactory { get; }

    internal TrialAnalyzer(Func<TAlgorithm, TOperator> selector, Func<TOperator, TAnalyzer> analyzerFactory)
    {
        Selector = selector;
        AnalyzerFactory = analyzerFactory;
    }

    internal override IExecutionHook CreateFor(TAlgorithm algorithm) => AnalyzerFactory(Selector(algorithm));
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
