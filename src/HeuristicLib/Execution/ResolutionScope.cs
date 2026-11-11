using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Resolves configurations into executors.</summary>
/// <remarks>
/// A scope is the sharing unit: within one scope, each configuration reference resolves to one executor, built on its
/// first request. Building one configuration never changes how another is built: <see cref="Share"/>, the one change a
/// construction can make to its scope, changes only how long an executor lives. A child scope from
/// <see cref="CreateChildScope"/> keeps the shared configurations of its ancestors and builds its own executors for
/// everything else. <see cref="Share"/> lets child scopes use one executor of a configuration, kept in this scope's table
/// and built in a child scope of its own. All scopes created from one root share one construction lock, one run's
/// <see cref="Results"/> and one fate: after a construction failure, every resolution through the tree throws, while the
/// results stay readable.
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

    /// <summary>Creates the root of a new scope tree, with the results of a new run.</summary>
    public static ResolutionScope Create() => Create(TimeProvider.System);

    // AlgorithmRun uses this provider for elapsed time; direct scope execution does not start measurement.
    internal static ResolutionScope Create(TimeProvider timeProvider)
    {
        var results = new RunResults(timeProvider);
        var root = new ResolutionScope(new Node(new Tree(results), null));
        results.Root = root;
        return root;
    }

    /// <summary>The results of the run this scope tree belongs to, one per instrument.</summary>
    /// <remarks>
    /// Configurations look up the results they write into or read, such as the counter a terminator checks. Every scope
    /// of one tree has the same results.
    /// </remarks>
    public RunResults Results => node.Tree.Results;

    /// <summary>Makes child scopes use this scope's executor of each given configuration instead of building their own.</summary>
    /// <remarks>
    /// A shared executor is built on its first request, in a new child scope of this scope that lives as long as this
    /// scope, so the children it builds stay its own. Shared configurations therefore share children only when those are
    /// shared too. The nearest scope sharing a configuration wins. Sharing a configuration again changes nothing. Sharing a
    /// configuration that this scope or a scope below it has resolved or is still building is an error, so a shared
    /// configuration is always built in a child scope of its own, whatever the order of resolution. Anything else can be
    /// shared at any time, also once the scope has child scopes. Reading a run result does not count as resolving it, so a
    /// result read before the run keeps its executor when the run shares it.
    /// </remarks>
    public void Share(params IReadOnlyList<IConfigurationNode> configurations)
    {
        using var locked = node.Tree.Sync.EnterScope();
        var added = configurations.Where(configuration => node.Shared?.Contains(configuration) != true).ToList();
        if (added.Count == 0)
            return;
        if (added.FirstOrDefault(configuration => node.ResolvedHereOrBelow.TryGetValue(configuration, out _)) is { } resolved)
            throw new InvalidOperationException($"{resolved.GetType().Name} cannot be shared in this resolution scope, because this scope or a scope below it " +
                "has already resolved it. Share it on a node that encloses every use, so that it is shared before it is first resolved.");
        (node.Shared ??= new(ReferenceEqualityComparer.Instance)).UnionWith(added);
    }

    /// <summary>Creates a scope below this one, for one activation of a child, such as a Cycle turn.</summary>
    /// <remarks>
    /// The child scope keeps the shared configurations of its ancestors and builds its own executors for every other
    /// configuration, so it shares nothing else with this scope or its siblings.
    /// </remarks>
    public ResolutionScope CreateChildScope() => new(new Node(node.Tree, node));

    /// <summary>Builds a configuration's executor, or returns the one this scope already built for it.</summary>
    public TExecutor Resolve<TExecutor>(IConfigurationNode<TExecutor> configuration)
        where TExecutor : class, IExecutor
    {
        node.Tree.ThrowIfFailed();
        using var building = node.Tree.Enter(configuration);
        var owner = Owner(configuration);
        var existing = Existing(owner, configuration, static executor => executor is TExecutor);
        node.MarkResolved(configuration);
        try
        {
            // The resolver is the one place that constructs executors; the delegate only chooses the constructing scope.
#pragma warning disable HLib0001
            return (TExecutor?)existing ?? Build(owner, configuration, scope => configuration.CreateExecutor(scope));
#pragma warning restore HLib0001
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
    /// A rejection before anything is built, such as a signature mismatch, becomes a reason and leaves the scope usable. A
    /// construction failure ends the scope tree and propagates.
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

    // The scope whose table holds the configuration's executor: the nearest one sharing it, otherwise the root for a run
    // result and this one for every other executor.
    private Node Owner(IConfigurationNode configuration)
    {
        var root = node;
        for (var scope = node; scope is not null; root = scope, scope = scope.Parent)
            if (scope.Shared?.Contains(configuration) == true)
                return scope;
        return configuration is IConfigurationNode<IRunResult> ? root : node;
    }

    // What the run reads. It does not go through Resolve, because the results of a failed run stay readable, also one that
    // was never built. A read is not a resolution, so it leaves Share free: looking at a result cannot break the run.
    internal TResult RunResult<TResult>(IConfigurationNode<TResult> configuration)
        where TResult : class, IRunResult
    {
        using var locked = node.Tree.Sync.EnterScope();
        var root = node;
        while (root.Parent is { } parent)
            root = parent;
        if (root.Executors.TryGetValue(configuration, out var existing))
            return (TResult)existing;
        using var building = node.Tree.Building(configuration);
        // The resolver is the one place that constructs executors.
#pragma warning disable HLib0001
        var executor = configuration.CreateExecutor(new ResolutionScope(root));
#pragma warning restore HLib0001
        root.Executors.Add(configuration, executor);
        return executor;
    }

    // Called while the construction lock is held. Returns the owner's executor of the configuration, or null if
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
    // so its children stay its own.
    private static TExecutor Build<TExecutor>(Node owner, IConfigurationNode configuration, Func<ResolutionScope, TExecutor> create)
        where TExecutor : class, IExecutor
    {
        var building = owner;
        if (owner.Shared?.Contains(configuration) == true)
            building = new Node(owner.Tree, owner);
        var executor = create(new ResolutionScope(building));
        // A construction that caught a nested failure must not publish an executor from the failed tree.
        owner.Tree.ThrowIfFailed();
        owner.Executors.Add(configuration, executor);
        return executor;
    }

    // One scope: its table, its sharing set and its parent. Every view of a scope, typed or not, shares one node.
    private sealed class Node(Tree tree, Node? parent)
    {
        public Tree Tree { get; } = tree;
        public Node? Parent { get; } = parent;

        // The executors this scope built, by configuration reference.
        public Dictionary<IConfigurationNode, IExecutor> Executors { get; } = new(ReferenceEqualityComparer.Instance);

        // The configurations whose executor this scope builds for its child scopes too. Null while it shares nothing.
        public HashSet<IConfigurationNode>? Shared { get; set; }

        // The configurations this scope or a scope below it has started to resolve, which it can no longer share. Weak by
        // reference, because a scope must not retain what a discarded child scope resolved.
        public ConditionalWeakTable<IConfigurationNode, object> ResolvedHereOrBelow { get; } = [];

        public void MarkResolved(IConfigurationNode configuration)
        {
            // An ancestor that already holds it got it from an earlier resolution, which marked the ones above it too.
            var scope = this;
            while (scope is not null && scope.ResolvedHereOrBelow.TryAdd(configuration, configuration))
                scope = scope.Parent;
        }
    }

    // What all scopes of one root share: the construction lock, the run's results and whether construction failed.
    private sealed class Tree(RunResults results)
    {
        // The configurations under construction. Only the thread holding the lock changes it.
        private readonly HashSet<IConfigurationNode> constructing = new(ReferenceEqualityComparer.Instance);
        private volatile string? failure;

        public Lock Sync => Results.Sync;
        public RunResults Results { get; } = results;
        public bool Failed => failure is not null;

        public void ThrowIfFailed()
        {
            if (failure is { } failed)
                throw new InvalidOperationException($"Building {failed} failed in this resolution scope tree, so none of its scopes resolves anything " +
                    "more: what it built before the failure could otherwise be reused. Resolve through a new scope.");
        }

        public void Fail(IConfigurationNode configuration) => failure ??= configuration.GetType().Name;

        internal IDisposable Enter(IConfigurationNode configuration)
        {
            using var locked = Sync.EnterScope();
            ThrowIfFailed();
            return Building(configuration);
        }

        // Holds the lock and marks the configuration as under construction until disposed.
        internal IDisposable Building(IConfigurationNode configuration)
        {
            Sync.Enter();
            try
            {
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
