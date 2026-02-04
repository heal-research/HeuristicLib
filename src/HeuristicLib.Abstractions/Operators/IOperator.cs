namespace HEAL.HeuristicLib.Operators;

public interface IOperator<out TExecutionInstance>
  where TExecutionInstance : IExecutionInstance
{
  TExecutionInstance CreateExecutionInstance(ExecutionScope scope);
}

public interface IExecutionInstance;

public sealed class ExecutionScope
{
  private readonly Dictionary<object, object> cache = new();

  public TInstance GetOrAdd<TOperator, TInstance>(TOperator @operator, Func<TInstance> factory)
    where TOperator : IOperator<TInstance>
    where TInstance : IExecutionInstance
  {
    if (cache.TryGetValue(@operator, out var existing)) {
      return (TInstance)existing;
    }

    var instance = factory();
    cache[@operator] = instance!;
    return instance;
  }
}
