namespace HEAL.HeuristicLib.Execution;

public static class ExecutionInstanceResolvableExtensions
{
    extension<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable)
        where TExecutionInstance : class, IExecutionInstance
    {
        public TExecutionInstance CreateExecutionInstance()
        {
            var resolver = ExecutionInstanceResolver.Create();
            return resolver.Resolve(resolvable);
        }

        public TExecutionInstance CreateExecutionInstance(out ExecutionInstanceResolver resolver)
        {
            resolver = ExecutionInstanceResolver.Create();
            return resolver.Resolve(resolvable);
        }
    }
}
