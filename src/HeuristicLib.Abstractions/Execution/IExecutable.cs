namespace HEAL.HeuristicLib.Execution;

public interface IExecutable;

public interface IExecutable<out TExecutionInstance> : IExecutable
  where TExecutionInstance : IExecutionInstance
{
  TExecutionInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IExecutionInstance;
