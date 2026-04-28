namespace HEAL.HeuristicLib.Execution;

public static class ExecutableExtensions
{
  extension<TExecutionInstance>(IExecutable<TExecutionInstance> executable)
    where TExecutionInstance : class, IExecutionInstance
  {
    public TExecutionInstance CreateExecutionInstance(Run run)
    {
      var registry = new ExecutionInstanceRegistry(run);
      return registry.Resolve(executable);
    }

    public TExecutionInstance CreateExecutionInstance(Run run, out ExecutionInstanceRegistry registry)
    {
      registry = new ExecutionInstanceRegistry(run);
      return registry.Resolve(executable);
    }
  }
}
