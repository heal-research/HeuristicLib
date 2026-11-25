using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;

namespace HEAL.HeuristicLib.Analysis;

/// <summary>One run's accumulated state.</summary>
/// <remarks>Updates are serialized. Reading <see cref="Value"/> while the run writes to it is the state type's concern.</remarks>
public sealed class Aggregate<TState> : IRunResult where TState : class
{
    internal Aggregate(TState value)
    {
        Value = value;
    }

    /// <summary>The live state, without a copy; read after execution unless the state supports concurrent reads.</summary>
    public TState Value { get; }

    internal Lock Sync { get; } = new();
}

/// <summary>Accumulates observations from one or several sources into independent state for each run.</summary>
/// <remarks>This analyzer holds delegates, so a configuration it is placed in is not serializable. Updates are serialized per result.</remarks>
public sealed class AggregateAnalyzer<TObservation, TState> : Analyzer<Aggregate<TState>>, IObservationReceiver<TObservation>
    where TState : class
{
    private readonly ValueArray<ObservationSource<TObservation>> sources;
    private readonly Func<TState> create;
    private readonly Action<TState, TObservation> update;

    internal AggregateAnalyzer(IReadOnlyList<ObservationSource<TObservation>> sources, Func<TState> create, Action<TState, TObservation> update)
    {
        this.sources = [.. sources.DistinctBy(static source => source.Configuration, ReferenceEqualityComparer.Instance)];
        if (this.sources.Count == 0)
            throw new InvalidOperationException("An aggregate analyzer requires at least one observation source.");
        this.create = create;
        this.update = update;
    }

    protected override IReadOnlyList<ISubstitution> CreateSubstitutions() =>
        [.. sources.Select(source => source.RecordingInto(this))];

    /// <inheritdoc/>
    public override Aggregate<TState> CreateExecutionNode(ResolutionScope scope) => new(create());

    /// <inheritdoc/>
    public Action<TObservation> CreateRecorder(RunResults results)
    {
        var result = results.Result(this);
        return observation =>
        {
            lock (result.Sync)
                update(result.Value, observation);
        };
    }
}
