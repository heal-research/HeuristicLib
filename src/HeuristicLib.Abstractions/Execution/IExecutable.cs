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

public static class ExecutableExtensions
{
  extension<TExecutionInstance>(IExecutable<TExecutionInstance> executable) where TExecutionInstance : class, IExecutionInstance {
    public TExecutionInstance CreateNewExecutionInstance()
    {
      return executable.CreateExecutionInstance(new ExecutionInstanceRegistry());
    }
  }
}
