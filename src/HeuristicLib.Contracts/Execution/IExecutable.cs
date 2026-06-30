namespace HEAL.HeuristicLib.Execution;

// TODO: Rename this contract during the execution terminology pass. The new name should describe
// the capability of being resolved to an execution instance, for example IExecutionInstanceResolvable<TExecutionInstance>.
public interface IExecutable;

public interface IExecutable<out TExecutionInstance> : IExecutable
  where TExecutionInstance : IExecutionInstance
{
    TExecutionInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IExecutionInstance;
