using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Resolves configurations into executors and applies the advice of this scope and its ancestors.</summary>
/// <remarks>
/// A scope is the sharing unit: within one scope, each configuration reference resolves to one executor, built on its
/// first request, and every request applies the scope's advice around that executor. Advice belongs to the scope:
/// aspects added with <see cref="AddAspect"/>, the only way to add advice, apply to every later resolution in the scope
/// and in its child scopes. A child scope from <see cref="CreateChildScope"/> keeps the advice and the shared
/// configurations of its ancestors and builds its own executors for everything else. <see cref="Share"/> lets child
/// scopes use one executor of a configuration, kept in this scope's table and built in a child scope of its own. All
/// scopes created from one root share one construction lock, one resource table and one fate: after a construction
/// failure, every resolution through the tree throws.
/// </remarks>
public partial class ResolutionScope
{
    private readonly Node node;

    private ResolutionScope(Node node)
    {
        this.node = node;
    }

    /// <summary>Creates a scope that resolves exactly like <paramref name="scope"/>, for a typed scope over it.</summary>
    private protected ResolutionScope(ResolutionScope scope)
        : this(scope.node)
    {
    }

    /// <summary>Creates the root of a new scope tree, without advice.</summary>
    public static ResolutionScope Create() =>
        Create(new RunResources([]));

    internal static ResolutionScope Create(RunResources resources) =>
        new(new Node(new Tree(resources), null));

    /// <remarks>Lookup is lazy and uses handle identity. It does not serialize access to the resource itself.</remarks>
    public TResource Resource<TResource>(ResourceHandle<TResource> handle) where TResource : class =>
        node.Tree.Resources.Resource(handle);

    /// <summary>Adds an aspect to this scope, so that its advice applies to every later resolution here and in child scopes.</summary>
    /// <remarks>
    /// The added aspect wraps closer to each executor than the aspects added before it. Adding an aspect this scope
    /// already has changes nothing. Adding advice that would newly apply to a configuration this scope has resolved or is
    /// still building is an error, as is adding advice once the scope has child scopes, because the advice around those
    /// executors could no longer be the same for every request.
    /// </remarks>
    public void AddAspect(Aspect aspect)
    {
        using var locked = node.Tree.Sync.EnterScope();
        if (node.Aspects.Contains(aspect, ReferenceEqualityComparer.Instance))
            return;
        if (node.HasChildScopes)
            throw new InvalidOperationException($"{aspect.GetType().Name} cannot be added to a resolution scope that already has child scopes. " +
                "Add aspects before resolving anything that creates child scopes, so that every child sees the same advice.");
        foreach (var configuration in node.Advised)
        {
            var effective = MatchingAdvice(configuration).Select(pair => pair.Advice).ToHashSet(ReferenceEqualityComparer.Instance);
            foreach (var (pointcut, advice) in aspect)
                if (pointcut.Matches(configuration) && !effective.Contains(advice))
                    throw new InvalidOperationException($"{advice.GetType().Name} would apply to {configuration.GetType().Name}, which this resolution scope " +
                        "has already resolved without it. Add the aspect before the configuration is first resolved in this scope, " +
                        "or resolve the configuration in a child scope that starts with the aspect.");
        }
        node.Aspects.Insert(0, aspect);
    }

    /// <summary>Makes child scopes use this scope's executor of each given configuration instead of building their own.</summary>
    /// <remarks>
    /// A shared executor is built on its first request, in a new child scope of this scope that lives as long as this
    /// scope: it receives this scope's advice, while the advice its own construction adds and the children it builds stay
    /// its own. Shared configurations therefore share children only when those are shared too. Each request still
    /// applies its own scope's advice around the shared executor. The nearest scope sharing a configuration wins. Sharing
    /// a configuration again changes nothing. Otherwise sharing is fixed before anything uses it: sharing in a scope that
    /// already has child scopes, or sharing a configuration this scope has resolved or is still building, is an error, so
    /// a shared configuration is always built in a child scope of its own, whatever the order of resolution.
    /// </remarks>
    public void Share(params IReadOnlyList<IConfigurationNode> configurations)
    {
        using var locked = node.Tree.Sync.EnterScope();
        var added = configurations.Where(configuration => node.Shared?.Contains(configuration) != true).ToList();
        if (added.Count == 0)
            return;
        if (node.HasChildScopes)
            throw new InvalidOperationException("A resolution scope that already has child scopes cannot share more configurations. " +
                "Share configurations before resolving anything that creates child scopes.");
        if (added.FirstOrDefault(node.Advised.Contains) is { } resolved)
            throw new InvalidOperationException($"{resolved.GetType().Name} cannot be shared in this resolution scope, which has already " +
                "resolved it. Share it on a node that encloses every use, so that it is shared before it is first resolved.");
        (node.Shared ??= new(ReferenceEqualityComparer.Instance)).UnionWith(added);
    }

    /// <summary>Creates a scope below this one, for one activation of a child, such as a Cycle turn.</summary>
    /// <remarks>
    /// The child scope keeps the advice and the shared configurations of its ancestors and builds its own executors for
    /// every other configuration, so it shares nothing else with this scope or its siblings. Aspects added to the child
    /// scope wrap closer than its ancestors' advice. This scope's advice and sharing are fixed from now on.
    /// </remarks>
    public ResolutionScope CreateChildScope()
    {
        using var locked = node.Tree.Sync.EnterScope();
        node.HasChildScopes = true;
        return new(new Node(node.Tree, node));
    }

    /// <summary>Builds a configuration's executor and applies the matching advice for its role.</summary>
    /// <remarks>Matching advice must derive from <see cref="Advice{TConfiguration, TExecutor}"/> for this executor type.</remarks>
    public TExecutor Resolve<TExecutor>(IConfigurationNode<TExecutor> configuration)
        where TExecutor : class, IExecutor
    {
        node.Tree.ThrowIfFailed();
        using var building = node.Tree.Enter(configuration);
        var owner = Owner(configuration);
        var existing = Existing(owner, configuration, static executor => executor is TExecutor);
        var applicable = MatchingAdvice(configuration);
        // This scope's advice for the configuration is fixed from here on, also for its own construction.
        node.Advised.Add(configuration);
        try
        {
            // The resolver is the one place that constructs executors; the delegate only chooses the constructing scope.
#pragma warning disable HLib0001
            var executor = (TExecutor?)existing ?? Build(owner, configuration, scope => configuration.CreateExecutor(scope));
#pragma warning restore HLib0001
            foreach (var (_, advice) in applicable)
                executor = advice.WrapNode(executor, configuration) as TExecutor
                    ?? throw new InvalidOperationException($"{advice.GetType().Name} cannot wrap {typeof(TExecutor).Name} of {configuration.GetType().Name}. " +
                        $"Advice for this role derives from Advice<TConfiguration, {typeof(TExecutor).Name}> for a TConfiguration the configuration implements.");
            return executor;
        }
        catch
        {
            node.Tree.Fail(configuration);
            throw;
        }
    }

    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecutor? ResolveOptional<TExecutor>(IConfigurationNode<TExecutor>? configuration)
        where TExecutor : class, IExecutor => configuration is null ? null : Resolve(configuration);

    /// <remarks>
    /// A rejection before anything is built, such as a signature mismatch or an advice check, becomes a reason and leaves
    /// the scope usable. A construction failure ends the scope tree and propagates.
    /// </remarks>
    public bool TryResolve<TExecutor>(IConfigurationNode<TExecutor> configuration,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TExecutor : class, IExecutor =>
        Attempt(() => Resolve(configuration), out executor, out reason);

    // Runs a resolution and reports a rejection as a reason. A failed tree means the attempt reached construction, so
    // its exception propagates like every other exception.
    private bool Attempt<TExecutor>(Func<TExecutor> resolve,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TExecutor : class, IExecutor
    {
        try
        {
            executor = resolve();
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception) when (!node.Tree.Failed)
        {
            executor = null;
            reason = exception.Message;
            return false;
        }
    }

    // The scope whose table holds the configuration's executor: the nearest one sharing it, otherwise this one.
    private Node Owner(IConfigurationNode configuration)
    {
        for (var scope = node; scope is not null; scope = scope.Parent)
            if (scope.Shared?.Contains(configuration) == true)
                return scope;
        return node;
    }

    // Called while the construction lock is held. Returns the owner's unadvised executor of the configuration, or null if
    // it has none yet. A scope holds one executor per configuration, so a request it does not fit is rejected before
    // anything is built.
    private static IExecutor? Existing(Node owner, IConfigurationNode configuration, Func<IExecutor, bool> fits)
    {
        if (!owner.Executors.TryGetValue(configuration, out var existing))
            return null;
        return fits(existing)
            ? existing
            : throw new InvalidOperationException($"{configuration.GetType().Name} was already built in this resolution scope for other run types " +
                "or another operation than this request. A scope holds one executor per configuration, so every use must request an executor " +
                "that the first one also implements.");
    }

    // Called while the construction lock is held. Builds the configuration's executor and keeps it in its owner's table. A
    // shared configuration is built in a new child scope of its owner, the scope it would have had as a child activation,
    // so the advice its construction adds and its children stay its own.
    private static TExecutor Build<TExecutor>(Node owner, IConfigurationNode configuration, Func<ResolutionScope, TExecutor> create)
        where TExecutor : class, IExecutor
    {
        var building = owner;
        if (owner.Shared?.Contains(configuration) == true)
        {
            owner.HasChildScopes = true;
            building = new Node(owner.Tree, owner);
        }
        var executor = create(new ResolutionScope(building));
        // A construction that caught a nested failure must not publish an executor from the failed tree.
        owner.Tree.ThrowIfFailed();
        owner.Executors.Add(configuration, executor);
        return executor;
    }

    // Called while the construction lock is held. Yields the advice that applies to the configuration, closest to its
    // executor first: this scope's before its ancestors', within a scope the later added aspects first, and within an
    // aspect its pairs in their listed order. Pairs are matched to the configuration first, and each advice instance
    // applies once, at its closest occurrence. Aspects share advice on purpose: two traces reading one clock must still
    // count each call once, and keeping the closest occurrence keeps that clock inside both traces' observations,
    // wherever and in whichever order they were attached.
    private List<(Pointcut Pointcut, Advice Advice)> MatchingAdvice(IConfigurationNode configuration)
    {
        var applied = new HashSet<Advice>(ReferenceEqualityComparer.Instance);
        var matching = new List<(Pointcut Pointcut, Advice Advice)>();
        for (var scope = node; scope is not null; scope = scope.Parent)
            foreach (var aspect in scope.Aspects)
                foreach (var pair in aspect)
                    if (pair.Pointcut.Matches(configuration) && applied.Add(pair.Advice))
                        matching.Add(pair);
        return matching;
    }

    // One scope: its table, its aspect list and its parent. Every view of a scope, typed or not, shares one node.
    private sealed class Node(Tree tree, Node? parent)
    {
        public Tree Tree { get; } = tree;
        public Node? Parent { get; } = parent;

        // Closest to the executor first.
        public List<Aspect> Aspects { get; } = [];

        // The unadvised executors this scope built, by configuration reference.
        public Dictionary<IConfigurationNode, IExecutor> Executors { get; } = new(ReferenceEqualityComparer.Instance);

        // The configurations whose executor this scope builds for its child scopes too. Null while it shares nothing.
        public HashSet<IConfigurationNode>? Shared { get; set; }

        // The configurations this scope has started to resolve. Their advice is fixed, so neither advice added later nor
        // sharing may change how they resolve here.
        public HashSet<IConfigurationNode> Advised { get; } = new(ReferenceEqualityComparer.Instance);

        public bool HasChildScopes { get; set; }
    }

    // What all scopes of one root share: the construction lock, the run's resources and whether construction failed.
    private sealed class Tree(RunResources resources)
    {
        private readonly HashSet<IConfigurationNode> constructing = new(ReferenceEqualityComparer.Instance);
        private volatile string? failure;

        public Lock Sync { get; } = new();
        public RunResources Resources { get; } = resources;
        public bool Failed => failure is not null;

        public void ThrowIfFailed()
        {
            if (failure is { } failed)
                throw new InvalidOperationException($"Building {failed} failed in this resolution scope tree, so none of its scopes resolves anything " +
                    "more: an executor could be left with part of its advice. Resolve through a new scope.");
        }

        public void Fail(IConfigurationNode configuration) => failure ??= configuration.GetType().Name;

        internal IDisposable Enter(IConfigurationNode configuration)
        {
            Sync.Enter();
            try
            {
                ThrowIfFailed();
                if (!constructing.Add(configuration))
                    throw new InvalidOperationException($"Recursive construction of {configuration.GetType().Name}.");
            }
            catch
            {
                Sync.Exit();
                throw;
            }
            return new Entry(this, configuration);
        }

        private sealed class Entry(Tree tree, IConfigurationNode configuration) : IDisposable
        {
            public void Dispose()
            {
                tree.constructing.Remove(configuration);
                tree.Sync.Exit();
            }
        }
    }
}
