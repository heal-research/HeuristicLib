namespace HEAL.HeuristicLib.Execution;

public interface IExecutionInstanceResolver
{
  TExecutionInstance Resolve<TExecutionInstance>(IExecutable<TExecutionInstance> executable)
    where TExecutionInstance : class, IExecutionInstance;

  TExecutionInstance? ResolveOptional<TExecutionInstance>(IExecutable<TExecutionInstance>? executable)
    where TExecutionInstance : class, IExecutionInstance => executable == null ? null : Resolve(executable);
}
