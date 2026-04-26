namespace HEAL.HeuristicLib.Execution;

public interface IExecutionInstanceResolver
{
  TExecutionInstance Resolve<TExecutionInstance>(IExecutable<TExecutionInstance> executable)
    where TExecutionInstance : class, IExecutionInstance;
}
