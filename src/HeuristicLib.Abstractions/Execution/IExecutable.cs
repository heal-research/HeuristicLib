namespace HEAL.HeuristicLib.Execution;

public interface IExecutable<out TExecutionInstance>
  where TExecutionInstance : IExecutionInstance
{
  // ToDo: think about how we prevent users from calling this themselves, since the created instance would not be auto-registered.
  TExecutionInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IExecutionInstance
{
}
