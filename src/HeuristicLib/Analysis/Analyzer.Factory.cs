using HEAL.HeuristicLib.Analysis.Scoring;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public static class Analyzer
{
    public static BestMedianWorstAnalysis<T, TS, TP, TR> BestMedianWorst<T, TS, TP, TR>(
        params IInterceptor<T, TS, TP, TR>[] interceptors)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new BestMedianWorstAnalysis<T, TS, TP, TR>(interceptors);
    }

    public static BestMedianWorstPerEvaluationAnalysis<T, TS, TP, TR> BestMedianWorstPerEvaluation<T, TS, TP, TR>(
        IEvaluator<T, TS, TP>[] evaluators,
        IInterceptor<T, TS, TP, TR>[] interceptors)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new BestMedianWorstPerEvaluationAnalysis<T, TS, TP, TR>(evaluators, interceptors);
    }

    public static BestQualityAlgorithmScorer<T, TS, TP> BestQuality<T, TS, TP>(
        ObjectiveDirections objective,
        params IEvaluator<T, TS, TP>[] evaluators)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
    {
        return new BestQualityAlgorithmScorer<T, TS, TP>(objective, evaluators);
    }

    public static HyperVolumeAlgorithmScorer<T, TS, TP, SearchState> HyperVolume<T, TS, TP>(
        ObjectiveDirections problemObjective,
        ObjectiveVector referencePoint,
        params IEvaluator<T, TS, TP>[] evaluators)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
    {
        return new HyperVolumeAlgorithmScorer<T, TS, TP, SearchState>(problemObjective, referencePoint, evaluators);
    }
}
