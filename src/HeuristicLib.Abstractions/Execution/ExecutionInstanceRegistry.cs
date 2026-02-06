namespace HEAL.HeuristicLib.Execution;

public sealed class ExecutionInstanceRegistry
{
  private readonly Dictionary<object, object> cache = new(ReferenceEqualityComparer.Instance);

  // ToDo: does not currently work since TExecutionInstance cannot be inferred
  // public TExecutionInstance GetOrCreate<TExecutable, TExecutionInstance>(TExecutable @operator)
  //   where TExecutable : class, IExecutable<TExecutionInstance>
  //   where TExecutionInstance : class, IExecutionInstance
  // {
  //   if (cache.TryGetValue(@operator, out var existing)) {
  //     return (TExecutionInstance)existing;
  //   }
  //
  //   var instance = @operator.CreateExecutionInstance(this);
  //   cache[@operator] = instance;
  //   return instance;
  // }
  
  public TExecutionInstance GetOrAdd<TExecutable, TExecutionInstance>(TExecutable @operator, Func<TExecutionInstance> factory)
    where TExecutable : class, IExecutable<TExecutionInstance>
    where TExecutionInstance : class, IExecutionInstance
  {
    if (cache.TryGetValue(@operator, out var existing)) {
      return (TExecutionInstance)existing;
    }

    var instance = factory();
    cache[@operator] = instance;
    return instance;
  }
}
