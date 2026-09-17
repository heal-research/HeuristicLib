using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// Reads values from one typed execution observation.
/// </summary>
public interface IMeasurement<in TInput, out TValue>
{
    IReadOnlyList<TValue> Read(TInput input);
}

/// <summary>Runtime-only callback adapter. It has identity semantics and is not a serializable strategy.</summary>
public sealed class DelegateMeasurement<TInput, TValue>(Func<TInput, IReadOnlyList<TValue>> measure)
    : IMeasurement<TInput, TValue>
{
    public IReadOnlyList<TValue> Read(TInput input) => measure(input);
}

/// <summary>
/// Reads the objective vectors from the population in an algorithm observation.
/// </summary>
public sealed record ObjectiveVectorsMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IMeasurement<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, ObjectiveVector>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    public IReadOnlyList<ObjectiveVector> Read(AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState> observation) =>
        [.. observation.State.Population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector)];
}

/// <summary>
/// Reads the evaluated candidates from the population in an algorithm observation.
/// </summary>
public sealed record EvaluatedCandidatesMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IMeasurement<AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, EvaluatedCandidate<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    public IReadOnlyList<EvaluatedCandidate<TCandidate>> Read(AlgorithmObservation<TCandidate, TSearchSpace, TProblem, TSearchState> observation) =>
        observation.State.Population.EvaluatedCandidates;
}

/// <summary>
/// Reads the evaluated candidates from the population an interceptor produced.
/// </summary>
public sealed record InterceptedCandidatesMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, EvaluatedCandidate<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    public IReadOnlyList<EvaluatedCandidate<TCandidate>> Read(InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState> observation) =>
        observation.State.Population.EvaluatedCandidates;
}

/// <summary>
/// Reads the objective vectors an evaluator just produced.
/// </summary>
public sealed record ObjectiveVectorsFromEvaluationMeasurement<TCandidate, TSearchSpace, TProblem>
    : IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, ObjectiveVector>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IReadOnlyList<ObjectiveVector> Read(EvaluatorObservation<TCandidate, TSearchSpace, TProblem> observation) =>
        observation.ObjectiveVectors;
}

/// <summary>
/// Reads the candidates an evaluator just evaluated, paired with the objective vectors it produced for them.
/// </summary>
public sealed record EvaluatedCandidatesFromEvaluationMeasurement<TCandidate, TSearchSpace, TProblem>
    : IMeasurement<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>, EvaluatedCandidate<TCandidate>>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IReadOnlyList<EvaluatedCandidate<TCandidate>> Read(EvaluatorObservation<TCandidate, TSearchSpace, TProblem> observation) =>
        observation.Candidates.ToEvaluated(observation.ObjectiveVectors);
}

/// <summary>
/// Reads the offspring from a crossover observation.
/// </summary>
public sealed record OffspringMeasurement<TCandidate, TSearchSpace, TProblem>
    : IMeasurement<CrossoverObservation<TCandidate, TSearchSpace, TProblem>, TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public IReadOnlyList<TCandidate> Read(CrossoverObservation<TCandidate, TSearchSpace, TProblem> observation) =>
        observation.Offspring;
}

public sealed record InterceptedObjectiveVectorsMeasurement<TCandidate, TSearchSpace, TProblem, TSearchState>
    : IMeasurement<InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState>, ObjectiveVector>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
{
    public IReadOnlyList<ObjectiveVector> Read(InterceptorObservation<TCandidate, TSearchSpace, TProblem, TSearchState> observation) =>
        [.. observation.State.Population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector)];
}
