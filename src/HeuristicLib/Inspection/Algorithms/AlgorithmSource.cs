using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Inspection;

/// <summary>The observations of one algorithm's executor calls, such as <c>algorithm.Iterations</c> or <c>algorithm.Calls</c>.</summary>
/// <remarks>
/// A source of iterations or calls also creates a counter of them through its <c>Count</c> extension. Each call creates
/// a new counter with its own result.
/// </remarks>
public sealed class AlgorithmSource<TCandidate, TSearchState, TObservation> : ObservationSource<TObservation>
    where TSearchState : class, ISearchState
    where TObservation : IAlgorithmObservation<TCandidate, TSearchState, TObservation>
{
    internal AlgorithmSource(IAlgorithm<TCandidate, TSearchState> algorithm)
    {
        Algorithm = algorithm;
    }

    /// <summary>The observed algorithm.</summary>
    public IAlgorithm<TCandidate, TSearchState> Algorithm { get; }

    public override IConfigurationNode Configuration => Algorithm;

    public override Substitution RecordingInto(IObservationInstrument<TObservation> instrument) =>
        Substitution.Of<IAlgorithm<TCandidate, TSearchState>>(Algorithm, node => new ObservedAlgorithm<TCandidate, TSearchState, TObservation>(node, instrument));

    internal override Aspect Observe(Action<TObservation> callback) =>
        Aspect.Create(Pointcut.Reference(Algorithm), new AlgorithmObservationAdvice<TCandidate, TSearchState, TObservation>(callback));
}

/// <summary>Counts what one algorithm's executor calls do, such as the states they yield or the calls that start.</summary>
/// <remarks>
/// Attached with <c>Advised</c>, it counts at its algorithm's configuration reference. Placed explicitly with
/// <c>Counted</c>, it counts at the algorithm it wraps.
/// </remarks>
public abstract class AlgorithmCounter : Obsolete.Counter
{
    private protected AlgorithmCounter() { }

    // The advice counting into the accumulator, for the algorithm this counter is placed at.
    internal abstract AlgorithmAdvice<TCandidate, TSearchState> CreateAdvice<TCandidate, TSearchState>(CountAccumulator accumulator)
        where TSearchState : class, ISearchState;
}

// Counts at the observed algorithm through the observation advice that also serves explicit placement.
internal abstract class AlgorithmCounter<TObservedCandidate, TObservedSearchState>(IAlgorithm<TObservedCandidate, TObservedSearchState> source)
    : AlgorithmCounter
    where TObservedSearchState : class, ISearchState
{
    protected override IReadOnlyList<Substitution> CreateSubstitutions() =>
        [Substitution.Of<IAlgorithm<TObservedCandidate, TObservedSearchState>>(source, node => node.Counted(this))];

    protected sealed override Aspect CreateAspect(CountAccumulator result, RunResults results) =>
        Aspect.Create(Pointcut.Reference(source), CreateAdvice<TObservedCandidate, TObservedSearchState>(result));
}

// Counts the states an algorithm yields.
internal sealed class IterationCounter<TObservedCandidate, TObservedSearchState>(IAlgorithm<TObservedCandidate, TObservedSearchState> source)
    : AlgorithmCounter<TObservedCandidate, TObservedSearchState>(source)
    where TObservedSearchState : class, ISearchState
{
    internal override AlgorithmAdvice<TCandidate, TSearchState> CreateAdvice<TCandidate, TSearchState>(CountAccumulator accumulator) =>
        AlgorithmObservations.Advise<TCandidate, TSearchState>(_ => accumulator.IncrementBy(1));
}

// Counts the calls of an algorithm when they start.
internal sealed class CallCounter<TObservedCandidate, TObservedSearchState>(IAlgorithm<TObservedCandidate, TObservedSearchState> source)
    : AlgorithmCounter<TObservedCandidate, TObservedSearchState>(source)
    where TObservedSearchState : class, ISearchState
{
    internal override AlgorithmAdvice<TCandidate, TSearchState> CreateAdvice<TCandidate, TSearchState>(CountAccumulator accumulator) =>
        AlgorithmCalls.Advise<TCandidate, TSearchState>(call =>
        {
            if (call.Event == AlgorithmCallEvent.Started)
                accumulator.IncrementBy(1);
        });
}
