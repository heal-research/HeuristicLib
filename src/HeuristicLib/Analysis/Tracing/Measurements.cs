using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class Measurement
{
    public static EvaluatedCandidatesMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState> Candidates<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
        => new();

    public static InterceptedCandidatesMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState> Candidates<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
        => new();

    public static EvaluatedCandidatesFromEvaluationMeasurement<TCandidate, TSearchSpace, TProblem> Candidates<TCandidate, TSearchSpace, TProblem>(
        IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new();

    public static ObjectiveVectorsMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState> ObjectiveVectors<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState> algorithm)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
        => new();

    public static InterceptedObjectiveVectorsMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState> ObjectiveVectors<TCandidate, TSearchSpace, TProblem, TSearchState>(
        IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TSearchState : PopulationState<TCandidate>
        => new();

    public static ObjectiveVectorsFromEvaluationMeasurement<TCandidate, TSearchSpace, TProblem> ObjectiveVectors<TCandidate, TSearchSpace, TProblem>(
        IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new();

    public static OffspringMeasurement<TCandidate, TSearchSpace, TProblem> Offspring<TCandidate, TSearchSpace, TProblem>(
        ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        => new();

}
