using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Execution;

internal sealed class OperatorExecutor(ExecutionInstanceRegistry instanceRegistry) : IOperatorExecutor
{
  public TResult Execute<TOperatorInstance, TResult>(
    IOperator<TOperatorInstance> op,
    Func<TOperatorInstance, TResult> execute)
    where TOperatorInstance : class, IOperatorInstance
  {
    return execute(instanceRegistry.Resolve(op));
  }
}
