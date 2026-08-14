namespace HEAL.HeuristicLib.Analysis;

public abstract class TrialAnalyzer
{
    public static TrialAnalyzer<TAlgorithm, TOperator, TResult> Create<TAlgorithm, TOperator, TResult>(
        Func<TAlgorithm, TOperator> selector,
        Func<TOperator, IAnalyzer<TResult>> analyzerFactory)
        where TResult : class => new(selector, analyzerFactory);

    private protected TrialAnalyzer()
    {
    }
}

public sealed class TrialAnalyzer<TAlgorithm, TOperator, TResult> : TrialAnalyzer
    where TResult : class
{
    internal Func<TAlgorithm, TOperator> Selector { get; }

    internal Func<TOperator, IAnalyzer<TResult>> AnalyzerFactory { get; }

    internal TrialAnalyzer(Func<TAlgorithm, TOperator> selector, Func<TOperator, IAnalyzer<TResult>> analyzerFactory)
    {
        Selector = selector;
        AnalyzerFactory = analyzerFactory;
    }
}

public sealed record TrialAnalysisResult<TTrial, TResult>(TTrial Trial, IAnalyzer<TResult> Analyzer, TResult Result)
    where TResult : class;

public static class TrialAnalysisResult
{
    public static TrialAnalysisResult<TTrial, TResult> From<TTrial, TResult>(TTrial trial, IAnalyzer<TResult> analyzer, TResult result)
        where TResult : class => new(trial, analyzer, result);
}
