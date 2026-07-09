namespace HEAL.HeuristicLib.Execution;

public interface IExecutionInstanceResolver
{
    TExecutionInstance Resolve<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable)
      where TExecutionInstance : class, IExecutionInstance;

    TExecutionInstance? ResolveOptional<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance>? resolvable)
      where TExecutionInstance : class, IExecutionInstance => resolvable == null ? null : Resolve(resolvable);
}
