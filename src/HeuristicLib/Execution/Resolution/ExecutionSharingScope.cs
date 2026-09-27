using System.Runtime.ExceptionServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Decides which configurations share prepared state through local selections and ancestor lookup.
/// It does not construct execution nodes or apply wrappers.
/// </summary>
internal sealed class ExecutionSharingScope
{
    private readonly ExecutionSharingScope? parent;
    private readonly WeakReference<ExecutionSharingScope>? declaringScope;
    private readonly Dictionary<IConfigurationNode, ExecutionPreparation> selections = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, RetainedChildScope> retainedChildren = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IConfigurationNode> constructing;

    public ExecutionSharingScope(ExecutionSharingScope? parent, bool retainParent = true)
    {
        if (retainParent)
            this.parent = parent;
        else if (parent is not null)
            declaringScope = new(parent);
        constructing = parent?.constructing ?? new(ReferenceEqualityComparer.Instance);
    }

    // A generated wrapper must not retain its declaring scope through that scope's retained children.
    // Its active declarations and construction frames provide the strong ownership instead.
    private ExecutionSharingScope? Parent => parent ??
        (declaringScope is not null && declaringScope.TryGetTarget(out var target) ? target : null);

    /// <summary>Remembers a direct selection here, including one inherited from an ancestor.</summary>
    public ExecutionPreparation Select<TConfiguration, TExecution>(
        TConfiguration configuration, Func<TConfiguration, ExecutionFactory<TExecution>> prepare)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        if (!selections.TryGetValue(configuration, out var execution))
        {
            execution = FindOrCreate(configuration, prepare);
            selections[configuration] = execution;
        }

        return execution;
    }

    /// <summary>
    /// Finds existing state or reserves it locally. A caller selecting a dependency pins the result on its
    /// own preparation, without adding an inherited selection to this scope.
    /// </summary>
    public ExecutionPreparation FindOrCreate<TConfiguration, TExecution>(
        TConfiguration configuration, Func<TConfiguration, ExecutionFactory<TExecution>> prepare)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        for (var ancestor = this; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor.selections.TryGetValue(configuration, out var found))
                return found;

        var created = ExecutionPreparation.Create(configuration, this, prepare);
        selections.Add(configuration, created);
        return created;
    }

    public RetainedChildScope GetRetainedChild(object key) =>
        RetainedChildScope.GetOrAdd(retainedChildren, key, this);

    public bool IsWithin(ExecutionSharingScope ancestor)
    {
        for (var current = this; current is not null; current = current.Parent)
            if (ReferenceEquals(current, ancestor))
                return true;
        return false;
    }

    public void BeginConstruction(IConfigurationNode configuration)
    {
        if (!constructing.Add(configuration))
            throw new InvalidOperationException($"Recursive execution construction for {ExecutionSignature.Name(configuration.GetType())}.");
    }

    public void EndConstruction(IConfigurationNode configuration) => constructing.Remove(configuration);

    /// <summary>A retained sharing scope and its once-only declaration snapshot, independent of the requesting bindings.</summary>
    internal sealed class RetainedChildScope
    {
        private DeclarationStatus status;
        private ExceptionDispatchInfo? fault;
        private ImmutableArray<WrapperRegistration> declarations = [];

        private RetainedChildScope(ExecutionSharingScope parent)
        {
            Sharing = new(parent);
        }

        public ExecutionSharingScope Sharing { get; }

        public static RetainedChildScope GetOrAdd(Dictionary<object, RetainedChildScope> children, object key, ExecutionSharingScope parent)
        {
            if (!children.TryGetValue(key, out var child))
            {
                child = new RetainedChildScope(parent);
                children.Add(key, child);
            }

            return child;
        }

        public ImmutableArray<WrapperRegistration> GetDeclarations(Action<ResolutionScopeBuilder>? declare)
        {
            if (status == DeclarationStatus.Faulted)
                fault!.Throw();
            if (status == DeclarationStatus.Ready)
                return declarations;
            if (status == DeclarationStatus.Declaring)
                throw new InvalidOperationException("Recursive retained child declaration.");

            status = DeclarationStatus.Declaring;
            try
            {
                var builder = new ResolutionScopeBuilder(Sharing);
                declare?.Invoke(builder);
                declarations = builder.Snapshot();
                status = DeclarationStatus.Ready;
                return declarations;
            }
            catch (Exception exception)
            {
                fault = ExceptionDispatchInfo.Capture(exception);
                status = DeclarationStatus.Faulted;
                throw;
            }
        }

        private enum DeclarationStatus { Undeclared, Declaring, Ready, Faulted }
    }
}
