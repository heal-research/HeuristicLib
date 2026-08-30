using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The inputs and outputs captured after one evaluator call.
/// </summary>
public sealed record EvaluatorObservation<TCandidate, TSearchSpace, TProblem>(
    IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator,
    IReadOnlyList<ObjectiveVector> ObjectiveVectors,
    IReadOnlyList<TCandidate> Candidates,
    TSearchSpace SearchSpace,
    TProblem Problem) : Observation
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

/// <summary>
/// Delivers an observation after every evaluator call.
/// </summary>
internal sealed record ObservingEvaluator<TCandidate, TSearchSpace, TProblem>
    : WrappingEvaluator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly IEvaluator<TCandidate, TSearchSpace, TProblem> anchor;
    private readonly ValueArray<IObservationRecorder<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>>> recorders;

    public ObservingEvaluator(
        IEvaluator<TCandidate, TSearchSpace, TProblem> anchor,
        IEvaluator<TCandidate, TSearchSpace, TProblem> childEvaluator,
        IReadOnlyList<IObservationRecorder<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>>> recorders)
        : base(childEvaluator)
    {
        this.anchor = anchor;
        this.recorders = recorders.ToValueArray();
    }

    protected override WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator) =>
        new Instance(anchor, childEvaluator, recorders);

    private sealed class Instance(
        IEvaluator<TCandidate, TSearchSpace, TProblem> anchor,
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> childEvaluator,
        ValueArray<IObservationRecorder<EvaluatorObservation<TCandidate, TSearchSpace, TProblem>>> recorders)
        : WrappingEvaluatorInstance<TCandidate, TSearchSpace, TProblem>(childEvaluator)
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var objectiveVectors = ChildEvaluator.Evaluate(candidates, random, searchSpace, problem);
            var observation = new EvaluatorObservation<TCandidate, TSearchSpace, TProblem>(anchor, objectiveVectors, candidates, searchSpace, problem);
            foreach (var recorder in recorders)
                recorder.Record(observation);

            return objectiveVectors;
        }
    }
}
