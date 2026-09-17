using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Analysis;

public sealed class ObservationPlan
{
    private readonly Dictionary<IExecutionInstanceResolvable, ObservationEntry> entries = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Registers an observer at an observation anchor, which may be an operator or an algorithm.
    /// </summary>
    /// <remarks>
    /// The anchor is used as a reference-identity key, so observers registered at the same anchor are merged into a
    /// single observable replacement.
    /// </remarks>
    public void Observe<TAnchor, TObserver>(TAnchor anchor, TObserver observer, Func<TAnchor, IReadOnlyList<TObserver>, IExecutionInstanceResolvable> createObservable)
        where TAnchor : class, IExecutionInstanceResolvable
        where TObserver : class
    {
        var entry = new ObservationEntry<TAnchor, TObserver>(anchor, observer, createObservable);

        if (entries.TryGetValue(anchor, out var existingEntry))
        {
            if (existingEntry.TryMerge(entry))
            {
                return;
            }

            throw new InvalidOperationException($"Observation conflict for {anchor}.");
        }

        entries.Add(anchor, entry);
    }

    internal void Install(ExecutionInstanceRegistry registry)
    {
        foreach (var entry in entries.Values)
        {
            entry.Install(registry);
        }
    }

    private abstract class ObservationEntry
    {
        public abstract bool TryMerge(ObservationEntry other);

        public abstract void Install(ExecutionInstanceRegistry registry);
    }

    private sealed class ObservationEntry<TAnchor, TObserver>(TAnchor anchor, TObserver observer, Func<TAnchor, IReadOnlyList<TObserver>, IExecutionInstanceResolvable> createObservable)
        : ObservationEntry
        where TAnchor : class, IExecutionInstanceResolvable
        where TObserver : class
    {
        private readonly List<TObserver> observers = [observer];
        private TAnchor Anchor { get; } = anchor;

        public override bool TryMerge(ObservationEntry other)
        {
            if (other is not ObservationEntry<TAnchor, TObserver> typedOther || !ReferenceEquals(typedOther.Anchor, Anchor))
            {
                return false;
            }

            observers.AddRange(typedOther.observers);
            return true;
        }

        public override void Install(ExecutionInstanceRegistry registry)
        {
            registry.RegisterReplacement(Anchor, createObservable(Anchor, observers));
        }
    }
}
