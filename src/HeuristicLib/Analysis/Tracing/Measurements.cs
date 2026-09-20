using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

public static class Measurement
{
    public static EvaluatedCandidatesMeasurement<TCandidate, TSearchState> Candidates<TCandidate, TSearchState>(
        IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : PopulationState<TCandidate>
        => new();

    public static InterceptedCandidatesMeasurement<TCandidate, TSearchState> Candidates<TCandidate, TSearchState>(
        IInterceptor<TCandidate> interceptor)
        where TSearchState : PopulationState<TCandidate>
        => new();

    public static EvaluatedCandidatesFromEvaluationMeasurement<TCandidate> Candidates<TCandidate>(
        IEvaluator<TCandidate> evaluator)
        => new();

    public static ObjectiveVectorsMeasurement<TCandidate, TSearchState> ObjectiveVectors<TCandidate, TSearchState>(
        IAlgorithm<TCandidate, TSearchState> algorithm)
        where TSearchState : PopulationState<TCandidate>
        => new();

    public static InterceptedObjectiveVectorsMeasurement<TCandidate, TSearchState> ObjectiveVectors<TCandidate, TSearchState>(
        IInterceptor<TCandidate> interceptor)
        where TSearchState : PopulationState<TCandidate>
        => new();

    public static ObjectiveVectorsFromEvaluationMeasurement<TCandidate> ObjectiveVectors<TCandidate>(
        IEvaluator<TCandidate> evaluator)
        => new();

    public static OffspringMeasurement<TCandidate> Offspring<TCandidate>(
        ICrossover<TCandidate> crossover)
        => new();

}
