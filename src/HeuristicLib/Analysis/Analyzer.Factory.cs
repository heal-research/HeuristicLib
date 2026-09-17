using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class Analyzer
{
    /// <summary>
    /// Tracks best, median and worst quality at every interception the given interceptors observe.
    /// </summary>
    /// <remarks>
    /// Name the four type arguments instead when the analysis has to read a concrete search space, problem or state.
    /// </remarks>
    public static BestMedianWorstAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>> BestMedianWorst<T>(params IReadOnlyList<IInterceptor<T>> interceptors) =>
        BestMedianWorst<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>>(interceptors);

    public static BestMedianWorstAnalysis<T, TS, TP, TR> BestMedianWorst<T, TS, TP, TR>(params IReadOnlyList<IInterceptor<T>> interceptors)
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
    public static BestMedianWorstAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, TR> BestMedianWorst<TSelf, T, TR>(Algorithm<TSelf, T, TR> algorithm)
        where TSelf : Algorithm<TSelf, T, TR>
        where TR : PopulationState<T> =>
        BestMedianWorst<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, TR>(algorithm);

    public static BestMedianWorstAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, TR> BestMedianWorstOf<T, TR>(params IReadOnlyList<IAlgorithm<T, TR>> algorithms)
        where TR : PopulationState<T> =>
        BestMedianWorst<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, TR>(algorithms);

    public static BestMedianWorstAnalysis<T, TS, TP, TR> BestMedianWorst<T, TS, TP, TR>(params IReadOnlyList<IAlgorithm<T, TR>> algorithms)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new() { Algorithms = algorithms.ToValueArray() };
    }

    public static BestMedianWorstPerEvaluationAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>> BestMedianWorstPerEvaluation<T>(IEvaluator<T>[] evaluators, IInterceptor<T>[] interceptors) =>
        BestMedianWorstPerEvaluation<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>>(evaluators, interceptors);

    public static BestMedianWorstPerEvaluationAnalysis<T, TS, TP, TR> BestMedianWorstPerEvaluation<T, TS, TP, TR>(IEvaluator<T>[] evaluators, IInterceptor<T>[] interceptors)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new(evaluators, interceptors);
    }

    public static BestQualityAlgorithmAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>> BestQuality<T>(params IEvaluator<T>[] evaluators) =>
        BestQuality<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>>(evaluators);

    public static BestQualityAlgorithmAnalysis<T, TS, TP> BestQuality<T, TS, TP>(params IEvaluator<T>[] evaluators)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
    {
        return new(evaluators);
    }

}
