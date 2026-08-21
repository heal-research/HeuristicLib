using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class Analyzer
{
    public static BestMedianWorstAnalysis<T, TS, TP, TR> BestMedianWorst<T, TS, TP, TR>(params IReadOnlyList<IInterceptor<T, TS, TP, TR>> interceptors)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new(interceptors);
    }

    /// <summary>
    /// Tracks best, median and worst quality at the end of every iteration the given algorithms yield.
    /// </summary>
    /// <remarks>
    /// The algorithm is used as a reference-identity anchor, so a copy produced by <c>with</c> is a different anchor
    /// and is not observed. Prefer <c>TrackBestMedianWorst</c> on the run, which resolves the anchor at attach time.
    /// </remarks>
    public static BestMedianWorstAnalysis<T, TS, TP, TR> BestMedianWorst<T, TS, TP, TR>(params IReadOnlyList<IAlgorithm<T, TS, TP, TR>> algorithms)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new() { Algorithms = algorithms.ToValueArray() };
    }

    public static BestMedianWorstPerEvaluationAnalysis<T, TS, TP, TR> BestMedianWorstPerEvaluation<T, TS, TP, TR>(IEvaluator<T, TS, TP>[] evaluators, IInterceptor<T, TS, TP, TR>[] interceptors)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new(evaluators, interceptors);
    }

    public static BestQualityAlgorithmAnalysis<T, TS, TP> BestQuality<T, TS, TP>(params IEvaluator<T, TS, TP>[] evaluators)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
    {
        return new(evaluators);
    }

}
