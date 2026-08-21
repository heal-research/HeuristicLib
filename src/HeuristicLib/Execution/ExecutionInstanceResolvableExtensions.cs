namespace HEAL.HeuristicLib.Execution;

public static class ExecutionInstanceResolvableExtensions
{
    extension<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable)
        where TExecutionInstance : class, IExecutionInstance
    {
        public TExecutionInstance CreateExecutionInstance()
        {
            var registry = new ExecutionInstanceRegistry();
            return registry.Resolve(resolvable);
        }

        public TExecutionInstance CreateExecutionInstance(out ExecutionInstanceRegistry registry)
        {
            registry = new ExecutionInstanceRegistry();
            return registry.Resolve(resolvable);
        }
    }
}
