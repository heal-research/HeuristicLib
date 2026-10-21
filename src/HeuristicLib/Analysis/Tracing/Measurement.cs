using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;

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
/// Reads objective vectors: those an evaluator just produced, or those of a search state's population.
/// </summary>
/// <remarks>
/// A search state is read from algorithm observations and from the states interceptors return alike, so one
/// measurement serves evaluators, algorithms and interceptors.
/// </remarks>
public sealed record ObjectiveVectorsMeasurement<TCandidate>
    : IMeasurement<OperatorObservation<Evaluation, TCandidate>, ObjectiveVector>,
      IMeasurement<IStateObservation<PopulationState<TCandidate>>, ObjectiveVector>
{
    public IReadOnlyList<ObjectiveVector> Read(OperatorObservation<Evaluation, TCandidate> input) => input.ObjectiveVectors;

    public IReadOnlyList<ObjectiveVector> Read(IStateObservation<PopulationState<TCandidate>> input) =>
        [.. input.State.Population.EvaluatedCandidates.Select(candidate => candidate.ObjectiveVector)];
}

/// <summary>
/// Reads evaluated candidates: those an evaluator just evaluated, paired with the objective vectors it produced, or
/// those of a search state's population.
/// </summary>
public sealed record CandidatesMeasurement<TCandidate>
    : IMeasurement<OperatorObservation<Evaluation, TCandidate>, EvaluatedCandidate<TCandidate>>,
      IMeasurement<IStateObservation<PopulationState<TCandidate>>, EvaluatedCandidate<TCandidate>>
{
    public IReadOnlyList<EvaluatedCandidate<TCandidate>> Read(OperatorObservation<Evaluation, TCandidate> input) =>
        input.Candidates.ToEvaluated(input.ObjectiveVectors);

    public IReadOnlyList<EvaluatedCandidate<TCandidate>> Read(IStateObservation<PopulationState<TCandidate>> input) =>
        input.State.Population.EvaluatedCandidates;
}

/// <summary>
/// Reads the offspring a crossover or mutator just produced.
/// </summary>
public sealed record OffspringMeasurement<TCandidate>
    : IMeasurement<OperatorObservation<Crossover, TCandidate>, TCandidate>,
      IMeasurement<OperatorObservation<Mutation, TCandidate>, TCandidate>
{
    public IReadOnlyList<TCandidate> Read(OperatorObservation<Crossover, TCandidate> input) => input.Offspring;

    public IReadOnlyList<TCandidate> Read(OperatorObservation<Mutation, TCandidate> input) => input.Offspring;
}
