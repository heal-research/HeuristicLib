namespace HEAL.HeuristicLib.Operators;

public interface IOperator<out TExecutionInstance>
  where TExecutionInstance : IExecutionInstance
{
  TExecutionInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IExecutionInstance
{
}
