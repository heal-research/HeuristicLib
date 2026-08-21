using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class ExperimentalAnalyzers
{
    public static BestPerEvaluationAnalysis<T, TS, TP> QualityCurve<T, TS, TP>(
        params IEvaluator<T, TS, TP>[] evaluators)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
    {
        return new(evaluators);
    }

    public static AllPopulationsAnalysis<T, TS, TP, TR> AllPopulations<T, TS, TP, TR>(
        IInterceptor<T, TS, TP, TR> interceptor)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new(interceptor);
    }

    public static HyperVolumeAnalysis<T, TS, TP> HyperVolume<T, TS, TP>(
        ObjectiveDirections objective,
        ObjectiveVector referencePoint,
        params IEvaluator<T, TS, TP>[] evaluators)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
    {
        return new(objective, referencePoint, evaluators);
    }

    public static GenealogyAnalysis<T, TS, TP, TR> Genealogy<T, TS, TP, TR>(
        ICrossover<T, TS, TP>? crossover = null,
        IMutator<T, TS, TP>? mutator = null,
        IInterceptor<T, TS, TP, TR>? interceptor = null,
        IEqualityComparer<T>? equality = null,
        bool saveSpace = false)
        where T : notnull
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new(crossover, mutator, interceptor, equality, saveSpace);
    }

    public static RankAnalysis<T, TS, TP, TR> Rank<T, TS, TP, TR>(
        ICrossover<T, TS, TP>? crossover = null,
        IMutator<T, TS, TP>? mutator = null,
        IInterceptor<T, TS, TP, TR>? interceptor = null,
        IEqualityComparer<T>? equality = null)
        where T : notnull
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new(crossover, mutator, interceptor, equality);
    }
}
