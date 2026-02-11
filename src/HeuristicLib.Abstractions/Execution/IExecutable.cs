namespace HEAL.HeuristicLib.Execution;

public interface IExecutable<out TExecutionInstance>
  where TExecutionInstance : IExecutionInstance
{
  // ToDo: think about how we prevent users from calling this themselves, since the created instance would not be auto-registered.
  /// <summary>
  /// Implement this method but never call it directly. Instead, use the extension method CreateNewExecutionInstance() to create a new execution instance.
  /// This ensures that the created instance is automatically registered in the ExecutionInstanceRegistry.
  /// </summary>
  TExecutionInstance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public interface IExecutionInstance;

public static class ExecutableExtensions
{
  extension<TExecutionInstance>(IExecutable<TExecutionInstance> executable) where TExecutionInstance : class, IExecutionInstance
  {
    public TExecutionInstance CreateNewExecutionInstance() => executable.CreateExecutionInstance(new ExecutionInstanceRegistry());
    public TExecutionInstance CreateNewExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => instanceRegistry.GetOrCreate(executable);
  }
}
