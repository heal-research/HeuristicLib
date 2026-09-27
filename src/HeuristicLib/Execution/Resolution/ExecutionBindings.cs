using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Constructs and caches execution nodes for one immutable observation path. Owns binding faults, wrapper
/// order and retained child bindings; it never selects a different preparation or owns shared execution state.
/// </summary>
internal sealed class ExecutionBindings
{
    private readonly ExecutionBindings? unchangedParent;
    private readonly ImmutableArray<RegistrationAtDepth> registrations;
    private readonly Dictionary<ExecutionPreparation, NodeConstruction> nodes = [];
    private readonly Dictionary<ExecutionSharingScope, ResolutionScope> retainedScopes = [];

    public ExecutionBindings(ExecutionBindings? parent, ImmutableArray<WrapperRegistration> local)
    {
        unchangedParent = local.IsEmpty ? parent : null;
        Depth = parent is null ? 0 : parent.Depth + 1;
        registrations =
        [
            .. (parent?.registrations ?? [])
                .Concat(local.Select(declaration => new RegistrationAtDepth(declaration, Depth)))
                .OrderBy(placement => placement.Registration.RegisteredByModule)
                .ThenByDescending(placement => placement.Depth)
                .ThenBy(placement => placement.Registration.Sequence)
        ];
    }

    public int Depth { get; }

    public ResolutionScope GetRetainedScope(ExecutionSharingScope.RetainedChildScope child, Action<ResolutionScopeBuilder>? declare)
    {
        var declarations = child.GetDeclarations(declare);
        if (!retainedScopes.TryGetValue(child.Sharing, out var scope))
        {
            scope = new ResolutionScope(child.Sharing, new ExecutionBindings(this, declarations));
            retainedScopes.Add(child.Sharing, scope);
        }

        return scope;
    }

    public (TExecution Raw, TExecution Completed) GetOrBind<TExecution>(ExecutionPreparation preparation)
        where TExecution : class, IExecutionNode =>
        Bind<TExecution>(preparation).GetNodes<TExecution>(preparation.Source);

    private NodeConstruction Bind<TExecution>(ExecutionPreparation preparation, WrappedNodes? target = null)
        where TExecution : class, IExecutionNode
    {
        var sharing = preparation.Sharing;
        sharing.BeginConstruction(preparation.Source);
        try
        {
            preparation.PrepareOnce();
            if (TryFindConstruction(preparation, out var existing))
            {
                existing.RequireCompleted();
                return existing;
            }

            var construction = new NodeConstruction();
            nodes.Add(preparation, construction);
            try
            {
                var frame = new ResolutionScope(sharing, this, preparation, target);
                var raw = preparation.GetFactory<TExecution>()(frame)
                    ?? throw new InvalidOperationException("An execution factory returned null.");
                construction.SetRaw(raw);
                var completed = target is null ? Wrap(preparation, raw) : raw;
                construction.Complete(completed);
                return construction;
            }
            catch (Exception exception)
            {
                construction.Fail(exception);
                throw;
            }
        }
        finally
        {
            sharing.EndConstruction(preparation.Source);
        }
    }

    private TExecution Wrap<TExecution>(ExecutionPreparation source, TExecution raw)
        where TExecution : class, IExecutionNode
    {
        var completed = raw;
        foreach (var declaration in registrations.Select(placement => placement.Registration))
        {
            if (!declaration.Matches(source.Source))
                continue;

            var wrapper = declaration.GetPreparation(source);
            var target = new WrappedNodes(source.Source, raw, completed);
            completed = Bind<TExecution>(wrapper, target).GetNodes<TExecution>(source.Source).Completed;
        }

        return completed;
    }

    private bool TryFindConstruction(ExecutionPreparation preparation, [NotNullWhen(true)] out NodeConstruction? construction)
    {
        // Empty child contexts may read existing ancestor nodes, but never publish their own nodes upward.
        for (var current = this; current is not null; current = current.unchangedParent)
            if (current.nodes.TryGetValue(preparation, out construction))
                return true;
        construction = null;
        return false;
    }

    private static TExecution RequireNode<TExecution>(IConfigurationNode configuration, IExecutionNode execution)
        where TExecution : class, IExecutionNode
    {
        if (execution is not TExecution typed)
        {
            throw new InvalidOperationException(
                $"{ExecutionSignature.Name(configuration.GetType())} produced {ExecutionSignature.Name(execution.GetType())}, not {ExecutionSignature.Name(typeof(TExecution))}.");
        }

        return typed;
    }

    /// <summary>Owns one construction attempt, keeping a failed raw node private and publishing only a complete chain.</summary>
    private sealed class NodeConstruction
    {
        private ConstructionStatus status;
        private IExecutionNode? raw;
        private IExecutionNode? completed;
        private ExceptionDispatchInfo? fault;

        public void SetRaw(IExecutionNode execution) => raw = execution;

        public void Complete(IExecutionNode execution)
        {
            completed = execution;
            status = ConstructionStatus.Completed;
        }

        public void Fail(Exception exception)
        {
            fault = ExceptionDispatchInfo.Capture(exception);
            status = ConstructionStatus.Faulted;
        }

        public void RequireCompleted()
        {
            if (status == ConstructionStatus.Faulted)
                fault!.Throw();
            if (status != ConstructionStatus.Completed)
                throw new InvalidOperationException("Recursive binding construction.");
        }

        public (TExecution Raw, TExecution Completed) GetNodes<TExecution>(IConfigurationNode source)
            where TExecution : class, IExecutionNode =>
            (RequireNode<TExecution>(source, raw!), RequireNode<TExecution>(source, completed!));

        private enum ConstructionStatus { Constructing, Completed, Faulted }
    }

    private readonly record struct RegistrationAtDepth(WrapperRegistration Registration, int Depth);

    /// <summary>
    /// The original source and the already-built nodes a generated wrapper wraps. Retaining this immutable target
    /// lets deferred calls reach the same inner chain without changing another wrapper or observation context.
    /// </summary>
    internal sealed class WrappedNodes(IConfigurationNode source, IExecutionNode raw, IExecutionNode inner)
    {
        public bool Matches(IConfigurationNode configuration) => ReferenceEquals(configuration, source);

        public (TExecution Raw, TExecution Completed) GetNodes<TExecution>()
            where TExecution : class, IExecutionNode =>
            (ExecutionBindings.RequireNode<TExecution>(source, raw), ExecutionBindings.RequireNode<TExecution>(source, inner));
    }
}
