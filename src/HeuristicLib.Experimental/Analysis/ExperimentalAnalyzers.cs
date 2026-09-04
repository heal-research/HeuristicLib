using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <remarks>
/// Each factory comes in two forms. The one naming only the candidate writes the analysis at the widest search space,
/// problem and population state, which serves any run because observers are contravariant in all three. Name the
/// remaining arguments when an analysis has to read a concrete search space or problem.
/// </remarks>
public static class ExperimentalAnalyzers
{
    /// <inheritdoc cref="ExperimentalAnalyzers" path="/remarks/node()"/>
    public static BestPerEvaluationAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>> QualityCurve<T>(
        params IEvaluator<T>[] evaluators) =>
        QualityCurve<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>>(evaluators);

    /// <inheritdoc cref="ExperimentalAnalyzers" path="/remarks/node()"/>
    public static AllPopulationsAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>> AllPopulations<T>(
        IInterceptor<T> interceptor) =>
        AllPopulations<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>>(interceptor);

    /// <inheritdoc cref="ExperimentalAnalyzers" path="/remarks/node()"/>
    public static HyperVolumeAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>> HyperVolume<T>(
        ObjectiveDirections objective,
        ObjectiveVector referencePoint,
        params IEvaluator<T>[] evaluators) =>
        HyperVolume<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>>(objective, referencePoint, evaluators);

    /// <inheritdoc cref="ExperimentalAnalyzers" path="/remarks/node()"/>
    public static GenealogyAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>> Genealogy<T>(
        ICrossover<T>? crossover = null,
        IMutator<T>? mutator = null,
        IInterceptor<T>? interceptor = null,
        IEqualityComparer<T>? equality = null,
        bool saveSpace = false)
        where T : notnull =>
        Genealogy<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>>(crossover, mutator, interceptor, equality, saveSpace);

    /// <inheritdoc cref="ExperimentalAnalyzers" path="/remarks/node()"/>
    public static RankAnalysis<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>> Rank<T>(
        ICrossover<T>? crossover = null,
        IMutator<T>? mutator = null,
        IInterceptor<T>? interceptor = null,
        IEqualityComparer<T>? equality = null)
        where T : notnull =>
        Rank<T, ISearchSpace<T>, IProblem<T, ISearchSpace<T>>, PopulationState<T>>(crossover, mutator, interceptor, equality);

    public static BestPerEvaluationAnalysis<T, TS, TP> QualityCurve<T, TS, TP>(
        params IEvaluator<T>[] evaluators)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
    {
        return new(evaluators);
    }

    public static AllPopulationsAnalysis<T, TS, TP, TR> AllPopulations<T, TS, TP, TR>(
        IInterceptor<T> interceptor)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new(interceptor);
    }

    public static HyperVolumeAnalysis<T, TS, TP> HyperVolume<T, TS, TP>(
        ObjectiveDirections objective,
        ObjectiveVector referencePoint,
        params IEvaluator<T>[] evaluators)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
    {
        return new(objective, referencePoint, evaluators);
    }

    public static GenealogyAnalysis<T, TS, TP, TR> Genealogy<T, TS, TP, TR>(
        ICrossover<T>? crossover = null,
        IMutator<T>? mutator = null,
        IInterceptor<T>? interceptor = null,
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
        ICrossover<T>? crossover = null,
        IMutator<T>? mutator = null,
        IInterceptor<T>? interceptor = null,
        IEqualityComparer<T>? equality = null)
        where T : notnull
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new(crossover, mutator, interceptor, equality);
    }
}
