namespace HEAL.HeuristicLib.Execution;

public sealed class ExecutionInstanceRegistry
{
  private readonly Dictionary<IExecutable<IExecutionInstance>, IExecutionInstance> cache = new(ReferenceEqualityComparer.Instance);

  public TExecutionInstance Resolve<TExecutionInstance>(IExecutable<TExecutionInstance> executable)
    where TExecutionInstance : class, IExecutionInstance
  {
    if (cache.TryGetValue(executable, out var existing)) {
      return (TExecutionInstance)existing;
    }

    var instance = executable.CreateExecutionInstance(this);
    cache[executable] = instance;
    return instance;
  }
}
