namespace HEAL.HeuristicLib.Execution;

public interface IExecutionInstanceResolvable;

public interface IExecutionInstanceResolvable<out TExecutionInstance> : IExecutionInstanceResolvable
  where TExecutionInstance : IExecutionInstance
{
    TExecutionInstance CreateExecutionInstance(ExecutionInstanceResolver resolver);
}

public interface IExecutionInstance;
