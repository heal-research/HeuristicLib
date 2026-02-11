namespace HEAL.HeuristicLib.Execution;

public interface IExecutable<out TExecutionInstance>
  where TExecutionInstance : IExecutionInstance
{
  TExecutionInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IExecutionInstance;

public static class ExecutableExtensions
{
  extension<TExecutionInstance>(IExecutable<TExecutionInstance> executable)
    where TExecutionInstance : class, IExecutionInstance
  {
    public TExecutionInstance CreateExecutionInstance()
    {
      var registry = new ExecutionInstanceRegistry();
      return registry.Resolve(executable);
    }

    public TExecutionInstance CreateExecutionInstance(out ExecutionInstanceRegistry registry)
    {
      registry = new ExecutionInstanceRegistry();
      return registry.Resolve(executable);
    }
  }
}
