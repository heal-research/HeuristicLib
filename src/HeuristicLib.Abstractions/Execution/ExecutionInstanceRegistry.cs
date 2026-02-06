namespace HEAL.HeuristicLib.Execution;

public sealed class ExecutionInstanceRegistry
{
  private readonly Dictionary<object, object> cache = new(ReferenceEqualityComparer.Instance);

  public TExecutionInstance GetOrCreate<TExecutionInstance>(IExecutable<TExecutionInstance> @operator)
    where TExecutionInstance : class, IExecutionInstance
  {
    if (cache.TryGetValue(@operator, out var existing)) {
      return (TExecutionInstance)existing;
    }
  
    var instance = @operator.CreateExecutionInstance(this);
    cache[@operator] = instance;
    return instance;
  }
}
