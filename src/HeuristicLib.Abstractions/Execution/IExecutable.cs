using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Execution;

public interface IExecutable<out TExecutionInstance>
  where TExecutionInstance : IExecutionInstance
{
  TExecutionInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IExecutionInstance
{
}
