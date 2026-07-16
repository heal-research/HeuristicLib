using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public static class ExperimentalAnalyzers
{
    public static BestPerEvaluationAnalysis<T, TS, TP> QualityCurve<T, TS, TP>(
        params IEvaluator<T, TS, TP>[] evaluators)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
    {
        return new BestPerEvaluationAnalysis<T, TS, TP>(evaluators);
    }

    public static AllPopulationsAnalysis<T, TS, TP, TR> AllPopulations<T, TS, TP, TR>(
        IInterceptor<T, TS, TP, TR> interceptor)
        where TS : class, ISearchSpace<T>
        where TP : class, IProblem<T, TS>
        where TR : PopulationState<T>
    {
        return new AllPopulationsAnalysis<T, TS, TP, TR>(interceptor);
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
        return new GenealogyAnalysis<T, TS, TP, TR>(crossover, mutator, interceptor, equality, saveSpace);
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
        return new RankAnalysis<T, TS, TP, TR>(crossover, mutator, interceptor, equality);
    }
}
