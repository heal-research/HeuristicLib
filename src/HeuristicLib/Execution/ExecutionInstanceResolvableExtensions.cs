namespace HEAL.HeuristicLib.Execution;

public static class ExecutionInstanceResolvableExtensions
{
    extension<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable)
      where TExecutionInstance : class, IExecutionInstance
    {
        public TExecutionInstance CreateExecutionInstance(Run run)
        {
            var registry = new ExecutionInstanceRegistry(run);
            return registry.Resolve(resolvable);
        }

        public TExecutionInstance CreateExecutionInstance(Run run, out ExecutionInstanceRegistry registry)
        {
            registry = new ExecutionInstanceRegistry(run);
            return registry.Resolve(resolvable);
        }
    }
}
