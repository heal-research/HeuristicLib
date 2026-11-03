using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Execution;

/// <remarks>
/// Every resolution constructs an executor with the requesting context's children and advice. Private data belongs
/// to that executor. Contexts share one flat resource table and apply their own aspects before ancestor aspects.
/// Resource use is not automatically synchronized and supplied resources remain caller owned.
/// </remarks>
public partial class ResolutionScope
{
    private readonly RunResources resources;
    private readonly ConstructionGuard construction;
    private readonly ResolutionScope? parent;
    private readonly ImmutableArray<Aspect> aspects;

    private ResolutionScope(RunResources resources, ConstructionGuard construction, ResolutionScope? parent, ImmutableArray<Aspect> aspects)
    {
        this.resources = resources;
        this.construction = construction;
        this.parent = parent;
        this.aspects = aspects;
    }

    /// <summary>Creates a scope that resolves exactly like <paramref name="scope"/>, for a typed scope over it.</summary>
    private protected ResolutionScope(ResolutionScope scope)
        : this(scope.resources, scope.construction, scope.parent, scope.aspects)
    {
    }

    /// <remarks>Aspects apply in the supplied order. An empty collection creates an unadvised scope.</remarks>
    public static ResolutionScope Create(params IReadOnlyList<Aspect> aspects) =>
        Create(new RunResources([]), aspects);

    internal static ResolutionScope Create(RunResources resources, IReadOnlyList<Aspect> aspects) =>
        new(resources, new ConstructionGuard(), null, [.. aspects]);

    /// <remarks>Lookup is lazy and uses handle identity. It does not serialize access to the resource itself.</remarks>
    public TResource Resource<TResource>(ResourceHandle<TResource> handle) where TResource : class =>
        resources.Resource(handle);

    /// <remarks>Uses the same run resources. The supplied aspects apply before this scope's aspects.</remarks>
    public ResolutionScope With(params IReadOnlyList<Aspect> aspects) =>
        new(resources, construction, this, [.. aspects]);

    /// <summary>Builds a configuration's executor and applies the matching advice for its role.</summary>
    /// <remarks>Matching advice must derive from <see cref="Advice{TConfiguration, TExecutor}"/> for this executor type.</remarks>
    public TExecutor Resolve<TExecutor>(IConfigurationNode<TExecutor> configuration)
        where TExecutor : class, IExecutor
    {
        using var building = construction.Enter(configuration);
        var executor = configuration.CreateExecutor(this);
        foreach (var (pointcut, advice) in Advices())
            if (pointcut.Matches(configuration))
                executor = advice.WrapNode(executor, configuration) as TExecutor
                    ?? throw new InvalidOperationException($"{advice.GetType().Name} cannot wrap {typeof(TExecutor).Name} of {configuration.GetType().Name}. " +
                        $"Advice for this role derives from Advice<TConfiguration, {typeof(TExecutor).Name}> for a TConfiguration the configuration implements.");
        return executor;
    }

    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecutor? ResolveOptional<TExecutor>(IConfigurationNode<TExecutor>? configuration)
        where TExecutor : class, IExecutor => configuration is null ? null : Resolve(configuration);

    /// <remarks>Invalid-operation failures become reasons; other exceptions propagate. Failed attempts may be retried.</remarks>
    public bool TryResolve<TExecutor>(IConfigurationNode<TExecutor> configuration,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TExecutor : class, IExecutor =>
        Attempt(() => Resolve(configuration), out executor, out reason);

    // Runs a resolution and reports an invalid-operation failure as a reason; other exceptions propagate.
    private static bool Attempt<TExecutor>(Func<TExecutor> resolve,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TExecutor : class, IExecutor
    {
        try
        {
            executor = resolve();
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            executor = null;
            reason = exception.Message;
            return false;
        }
    }

    // Walks from this scope towards the root and yields each advice instance once, at its first position.
    // Once: aspects share advice on purpose, for example two traces reading one clock; that clock must still count each
    // call once, so applying it twice would wrap the executor twice around the same data. The same holds when one aspect
    // is added twice.
    // First position: a trace lists its clock advice before its observations, so the clock wraps closer to the source
    // and has counted a call when the trace records it. Keeping a later occurrence, such as the same clock added again
    // after the trace, would move it outside the trace's observations and make the trace record one call behind.
    private IEnumerable<(Pointcut Pointcut, Advice Advice)> Advices()
    {
        var applied = new HashSet<Advice>(ReferenceEqualityComparer.Instance);
        for (var scope = this; scope is not null; scope = scope.parent)
            foreach (var aspect in scope.aspects)
                foreach (var pair in aspect)
                    if (applied.Add(pair.Advice))
                        yield return pair;
    }

    private sealed class ConstructionGuard
    {
        private readonly Lock sync = new();
        private readonly HashSet<IConfigurationNode> constructing = new(ReferenceEqualityComparer.Instance);

        internal IDisposable Enter(IConfigurationNode configuration)
        {
            sync.Enter();
            if (!constructing.Add(configuration))
            {
                sync.Exit();
                throw new InvalidOperationException($"Recursive construction of {configuration.GetType().Name}.");
            }
            return new Entry(this, configuration);
        }

        private sealed class Entry(ConstructionGuard guard, IConfigurationNode configuration) : IDisposable
        {
            public void Dispose()
            {
                guard.constructing.Remove(configuration);
                guard.sync.Exit();
            }
        }
    }
}
