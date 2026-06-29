namespace HEAL.HeuristicLib.Execution;

// TODO: Rename this contract during the runtime terminology pass. The new name should describe the
// capability of being resolved into a runtime object, for example IRuntimeResolvable<TRuntime>.
public interface IExecutable;

public interface IExecutable<out TExecutionInstance> : IExecutable
  where TExecutionInstance : IExecutionInstance
{
    TExecutionInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IExecutionInstance;
