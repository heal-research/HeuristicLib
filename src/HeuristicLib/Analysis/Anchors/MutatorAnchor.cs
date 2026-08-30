using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// The inputs and outputs captured after one mutator call.
/// </summary>
public sealed record MutatorObservation<TCandidate, TSearchSpace, TProblem>(
    IMutator<TCandidate, TSearchSpace, TProblem> Mutator,
    IReadOnlyList<TCandidate> Offspring,
    IReadOnlyList<TCandidate> Parents,
    TSearchSpace SearchSpace,
    TProblem Problem) : Observation
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

/// <summary>
/// Delivers an observation after every mutator call.
/// </summary>
internal sealed record ObservingMutator<TCandidate, TSearchSpace, TProblem>
    : WrappingMutator<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly IMutator<TCandidate, TSearchSpace, TProblem> anchor;
    private readonly ValueArray<IObservationRecorder<MutatorObservation<TCandidate, TSearchSpace, TProblem>>> recorders;

    public ObservingMutator(
        IMutator<TCandidate, TSearchSpace, TProblem> anchor,
        IMutator<TCandidate, TSearchSpace, TProblem> childMutator,
        IReadOnlyList<IObservationRecorder<MutatorObservation<TCandidate, TSearchSpace, TProblem>>> recorders)
        : base(childMutator)
    {
        this.anchor = anchor;
        this.recorders = recorders.ToValueArray();
    }

    protected override WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator) =>
        new Instance(anchor, childMutator, recorders);

    private sealed class Instance(
        IMutator<TCandidate, TSearchSpace, TProblem> anchor,
        IMutatorInstance<TCandidate, TSearchSpace, TProblem> childMutator,
        ValueArray<IObservationRecorder<MutatorObservation<TCandidate, TSearchSpace, TProblem>>> recorders)
        : WrappingMutatorInstance<TCandidate, TSearchSpace, TProblem>(childMutator)
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = ChildMutator.Mutate(parents, random, searchSpace, problem);
            var observation = new MutatorObservation<TCandidate, TSearchSpace, TProblem>(anchor, offspring, parents, searchSpace, problem);
            foreach (var recorder in recorders)
                recorder.Record(observation);

            return offspring;
        }
    }
}
