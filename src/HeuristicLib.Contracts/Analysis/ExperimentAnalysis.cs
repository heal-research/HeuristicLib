namespace HEAL.HeuristicLib.Analysis;

public static class ExperimentAnalysis
{
    public static AnalyzerBinding<TAlgorithm, TOperator, TResult> ForEach<TAlgorithm, TOperator, TResult>(Func<TAlgorithm, TOperator> selector, Func<TOperator, IAnalyzer<TResult>> analyzerFactory)
        where TResult : class => new(selector, analyzerFactory);
}

public sealed class AnalyzerBinding<TAlgorithm, TOperator, TResult>
    where TResult : class
{
    internal Func<TAlgorithm, TOperator> Selector { get; }

    internal Func<TOperator, IAnalyzer<TResult>> AnalyzerFactory { get; }

    internal AnalyzerBinding(Func<TAlgorithm, TOperator> selector, Func<TOperator, IAnalyzer<TResult>> analyzerFactory)
    {
        Selector = selector;
        AnalyzerFactory = analyzerFactory;
    }
}

public sealed record ExperimentAnalysisResult<TTrial, TResult>(TTrial Trial, IAnalyzer<TResult> Analyzer, TResult Result)
    where TResult : class;
