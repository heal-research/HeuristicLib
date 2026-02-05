using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Operators;

public interface IOperator<out TExecutionInstance> : IExecutable<TExecutionInstance>
  where TExecutionInstance : IOperatorInstance
{
  new TExecutionInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IOperatorInstance : IExecutionInstance
{
}
