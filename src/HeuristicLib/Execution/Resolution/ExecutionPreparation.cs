using System.Runtime.ExceptionServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Owns one configuration's once-only factory preparation, including failure, persistent state captured by that
/// factory, selected dependencies and retained child scopes. This is not a callable execution node.
/// </summary>
internal sealed class ExecutionPreparation
{
    private readonly WeakReference<ExecutionSharingScope> sharing;
    private readonly Type executionContract;
    private readonly Func<IConfigurationNode, Delegate> prepare;
    private readonly Dictionary<IConfigurationNode, ExecutionPreparation> dependencies = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, ExecutionSharingScope.RetainedChildScope> retainedChildren = new(ReferenceEqualityComparer.Instance);
    private PreparationStatus status;
    private Delegate? factory;
    private ExceptionDispatchInfo? fault;

    private ExecutionPreparation(IConfigurationNode source, ExecutionSharingScope sharing,
        Type executionContract, Func<IConfigurationNode, Delegate> prepare)
    {
        Source = source;
        this.sharing = new(sharing);
        this.executionContract = executionContract;
        this.prepare = prepare;
    }

    public IConfigurationNode Source { get; }

    // A pinned dependency must not retain an expired declaration through its owner's retained children.
    // Live scopes, factory frames, retained children and prepared wrappers supply strong ownership.
    public ExecutionSharingScope Sharing => sharing.TryGetTarget(out var owner)
        ? owner : throw new InvalidOperationException("The logical execution owner is no longer alive.");

    public static ExecutionPreparation Create<TConfiguration, TExecution>(
        TConfiguration configuration, ExecutionSharingScope sharing, Func<TConfiguration, ExecutionFactory<TExecution>> prepare)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode =>
        new(configuration, sharing, typeof(TExecution), source => prepare(RequireConfiguration(configuration, source)));

    public ExecutionPreparation SelectDependency<TConfiguration, TExecution>(
        TConfiguration configuration, Func<TConfiguration, ExecutionFactory<TExecution>> prepare)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        if (!dependencies.TryGetValue(configuration, out var dependency))
        {
            dependency = Sharing.FindOrCreate(configuration, prepare);
            dependencies.Add(configuration, dependency);
        }

        return dependency;
    }

    public ExecutionSharingScope.RetainedChildScope GetRetainedChild(object key) =>
        ExecutionSharingScope.RetainedChildScope.GetOrAdd(retainedChildren, key, Sharing);

    public ExecutionPreparation CreateWrapper(IConfigurationNode configuration, ExecutionSharingScope sharing) =>
        new(configuration, sharing, executionContract, prepare);

    public void RequireContract<TExecution>()
        where TExecution : class, IExecutionNode
    {
        if (!typeof(TExecution).IsAssignableFrom(executionContract))
        {
            throw new InvalidOperationException(
                $"{ExecutionSignature.Name(Source.GetType())} was prepared for {ExecutionSignature.Name(executionContract)}, not {ExecutionSignature.Name(typeof(TExecution))}. " +
                "Resolve over a second search space or problem in its own scope.");
        }
    }

    public ExecutionFactory<TExecution> GetFactory<TExecution>()
        where TExecution : class, IExecutionNode
    {
        PrepareOnce();
        return (ExecutionFactory<TExecution>)factory!;
    }

    public void PrepareOnce()
    {
        if (status == PreparationStatus.Faulted)
            fault!.Throw();
        if (status == PreparationStatus.Ready)
            return;
        if (status == PreparationStatus.Preparing)
            throw new InvalidOperationException("Recursive factory preparation.");

        status = PreparationStatus.Preparing;
        try
        {
            factory = prepare(Source) ?? throw new InvalidOperationException("Execution preparation returned null.");
            status = PreparationStatus.Ready;
        }
        catch (Exception exception)
        {
            fault = ExceptionDispatchInfo.Capture(exception);
            status = PreparationStatus.Faulted;
            throw;
        }
    }

    private static TConfiguration RequireConfiguration<TConfiguration>(TConfiguration configuration, IConfigurationNode wrapped)
        where TConfiguration : class, IConfigurationNode
    {
        if (wrapped is not TConfiguration typed)
        {
            throw new InvalidOperationException(
                $"{ExecutionSignature.Name(wrapped.GetType())} was declared to wrap {ExecutionSignature.Name(configuration.GetType())}, but it is not a {ExecutionSignature.Name(typeof(TConfiguration))} and cannot stand in for it.");
        }

        return typed;
    }

    private enum PreparationStatus { Unprepared, Preparing, Ready, Faulted }
}
