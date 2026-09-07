using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>
/// An anchor at every crossover call.
/// </summary>
public sealed class CrossoverAnchor<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover)
    : IAnchor<CrossoverObservation<TCandidate, TSearchSpace, TProblem>, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ICrossover<TCandidate, TSearchSpace, TProblem> Crossover { get; } = crossover;

    public void Install(ExecutionInstanceResolverBuilder builder, IObservationRecorder<CrossoverObservation<TCandidate, TSearchSpace, TProblem>> recorder) =>
        builder.Decorate(Crossover, current => new ObservingCrossover<TCandidate, TSearchSpace, TProblem>(Crossover, current, [recorder]));
}

/// <summary>
/// Delivers an observation after every crossover call.
/// </summary>
internal sealed record ObservingCrossover<TCandidate, TSearchSpace, TProblem>
    : WrappingCrossover<TCandidate, TSearchSpace, TProblem>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    private readonly ICrossover<TCandidate, TSearchSpace, TProblem> anchor;
    private readonly ValueArray<IObservationRecorder<CrossoverObservation<TCandidate, TSearchSpace, TProblem>>> recorders;

    public ObservingCrossover(
        ICrossover<TCandidate, TSearchSpace, TProblem> anchor,
        ICrossover<TCandidate, TSearchSpace, TProblem> childCrossover,
        IReadOnlyList<IObservationRecorder<CrossoverObservation<TCandidate, TSearchSpace, TProblem>>> recorders)
        : base(childCrossover)
    {
        this.anchor = anchor;
        this.recorders = recorders.ToValueArray();
    }

    protected override WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem> CreateExecutionInstance(ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover) =>
        new Instance(anchor, childCrossover, recorders);

    private sealed class Instance(
        ICrossover<TCandidate, TSearchSpace, TProblem> anchor,
        ICrossoverInstance<TCandidate, TSearchSpace, TProblem> childCrossover,
        ValueArray<IObservationRecorder<CrossoverObservation<TCandidate, TSearchSpace, TProblem>>> recorders)
        : WrappingCrossoverInstance<TCandidate, TSearchSpace, TProblem>(childCrossover)
    {
        public override IReadOnlyList<TCandidate> Cross(IReadOnlyList<Parents<TCandidate>> parents, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var offspring = ChildCrossover.Cross(parents, random, searchSpace, problem);
            var observation = new CrossoverObservation<TCandidate, TSearchSpace, TProblem>(anchor, offspring, parents, searchSpace, problem);
            foreach (var recorder in recorders)
                recorder.Record(observation);

            return offspring;
        }
    }
}
