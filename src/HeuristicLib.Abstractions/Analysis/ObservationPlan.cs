using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Analysis;

public sealed class ObservationPlan
{
  private readonly Dictionary<IOperator, ObservationEntry> entries = new(ReferenceEqualityComparer.Instance);

  public void Observe<TOperator, TExecutionInstance, TObserver>(
    TOperator @operator,
    TObserver observer,
    Func<TOperator, IReadOnlyList<TObserver>, IExecutable<TExecutionInstance>> createObservable)
    where TOperator : class, IOperator<TExecutionInstance>
    where TExecutionInstance : class, IOperatorInstance
    where TObserver : class
  {
    var entry = new ObservationEntry<TOperator, TExecutionInstance, TObserver>(@operator, observer, createObservable);

    if (entries.TryGetValue(@operator, out var existingEntry)) {
      if (existingEntry.TryMerge(entry)) {
        return;
      }

      throw new InvalidOperationException($"Observation conflict for operator {@operator}.");
    }

    entries.Add(@operator, entry);
  }

  internal void Install(ExecutionInstanceRegistry registry)
  {
    foreach (var entry in entries.Values) {
      entry.Install(registry);
    }
  }

  private abstract class ObservationEntry
  {
    public abstract bool TryMerge(ObservationEntry other);

    public abstract void Install(ExecutionInstanceRegistry registry);
  }

  private sealed class ObservationEntry<TOperator, TExecutionInstance, TObserver>(
    TOperator @operator,
    TObserver observer,
    Func<TOperator, IReadOnlyList<TObserver>, IExecutable<TExecutionInstance>> createObservable) : ObservationEntry
    where TOperator : class, IOperator<TExecutionInstance>
    where TExecutionInstance : class, IOperatorInstance
    where TObserver : class
  {
    private readonly List<TObserver> observers = [observer];
    private TOperator Operator { get; } = @operator;

    public override bool TryMerge(ObservationEntry other)
    {
      if (other is not ObservationEntry<TOperator, TExecutionInstance, TObserver> typedOther ||
          !ReferenceEquals(typedOther.Operator, Operator)) {
        return false;
      }

      observers.AddRange(typedOther.observers);
      return true;
    }

    public override void Install(ExecutionInstanceRegistry registry)
    {
      registry.PreRegister(Operator, createObservable(Operator, observers));
    }
  }
}
