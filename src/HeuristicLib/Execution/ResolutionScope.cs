using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Execution;

/// <summary>Resolves configurations into execution nodes.</summary>
/// <remarks>
/// A scope is the sharing unit: within one scope, each configuration/problem reference pair resolves to one execution node,
/// built on its first request. A configuration whose execution node needs no problem, such as a counter, uses only the
/// configuration. Building one configuration never changes how another is built: <see cref="Share"/>, the one change a
/// construction can make to its scope, changes only how long an execution node lives. A child scope from
/// <see cref="CreateChildScope"/> keeps the shared configurations of its ancestors and builds its own execution nodes for
/// everything else. <see cref="Share"/> lets child scopes use one execution node of a configuration, kept in this scope's table
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

    /// <summary>Makes child scopes use this scope's execution node of each given configuration instead of building their own.</summary>
    /// <remarks>
    /// A shared execution node is built on its first request, in a new child scope of this scope that lives as long as this
    /// scope, so the children it builds stay its own. Shared configurations therefore share children only when those are
    /// shared too. The nearest scope sharing a configuration wins. Sharing a configuration again changes nothing. Sharing a
    /// configuration that this scope or a scope below it has resolved or is still building is an error, so a shared
    /// configuration is always built in a child scope of its own, whatever the order of resolution. Anything else can be
    /// shared at any time, also once the scope has child scopes. Reading a run result does not count as resolving it, so a
    /// result read before the run keeps its execution node when the run shares it.
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
    /// The child scope keeps the shared configurations of its ancestors and builds its own execution nodes for every other
    /// configuration, so it shares nothing else with this scope or its siblings.
    /// </remarks>
    public ResolutionScope CreateChildScope() => new(new Node(node.Tree, node));

    /// <summary>Builds a configuration's execution node, or returns the one this scope already built for it.</summary>
    public TExecutionNode Resolve<TExecutionNode>(IConfigurationNode<TExecutionNode> configuration)
        where TExecutionNode : class, IExecutionNode
    {
        node.Tree.ThrowIfFailed();
        using var building = node.Tree.Enter(configuration);
        var owner = Owner(configuration);
        var existing = Existing(owner, configuration, static executionNode => executionNode is TExecutionNode, problem: null);
        node.MarkResolved(configuration);
        try
        {
            // The resolver is the one place that constructs execution nodes; the delegate only chooses the constructing scope.
#pragma warning disable HLib0001
            return (TExecutionNode?)existing ?? Build(owner, configuration, scope => configuration.CreateExecutionNode(scope), problem: null);
#pragma warning restore HLib0001
        }
        catch
        {
            node.Tree.Fail(configuration);
            throw;
        }
    }

    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecutionNode? ResolveOptional<TExecutionNode>(IConfigurationNode<TExecutionNode>? configuration)
        where TExecutionNode : class, IExecutionNode => configuration is null ? null : Resolve(configuration);

    /// <remarks>
    /// A rejection before anything is built, such as a signature mismatch, becomes a reason and leaves the scope usable. A
    /// construction failure ends the scope tree and propagates.
    /// </remarks>
    public bool TryResolve<TExecutionNode>(IConfigurationNode<TExecutionNode> configuration,
        [NotNullWhen(true)] out TExecutionNode? executionNode, [NotNullWhen(false)] out string? reason)
        where TExecutionNode : class, IExecutionNode =>
        Attempt(() => Resolve(configuration), out executionNode, out reason);

    // Runs a resolution and reports a rejection as a reason. A failed tree means the attempt reached construction, so
    // its exception propagates like every other exception.
    private bool Attempt<TExecutionNode>(Func<TExecutionNode> resolve,
        [NotNullWhen(true)] out TExecutionNode? executionNode, [NotNullWhen(false)] out string? reason)
        where TExecutionNode : class, IExecutionNode
    {
        try
        {
            executionNode = resolve();
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception) when (!node.Tree.Failed)
        {
            executionNode = null;
            reason = exception.Message;
            return false;
        }
    }

    // The scope whose table holds the configuration's execution node: the nearest one sharing it, otherwise the root for a run
    // result and this one for every other execution node.
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
        if (root.Nodes.TryGetValue(new(configuration, null), out var existing))
            return (TResult)existing;
        using var building = node.Tree.Building(configuration);
        // The resolver is the one place that constructs execution nodes.
#pragma warning disable HLib0001
        var executionNode = configuration.CreateExecutionNode(new ResolutionScope(root));
#pragma warning restore HLib0001
        root.Nodes.Add(new(configuration, null), executionNode);
        return executionNode;
    }

    // Called while the construction lock is held. Returns the owner's execution node of the configuration, or null if
    // it has none yet. Each binding has one execution node, so a request it does not fit is rejected before
    // anything is built.
    private static IExecutionNode? Existing(Node owner, IConfigurationNode configuration, Func<IExecutionNode, bool> fits, IProblem? problem)
    {
        if (!owner.Nodes.TryGetValue(new(configuration, problem), out var existing))
            return null;
        return fits(existing)
            ? existing
            : throw new InvalidOperationException($"{configuration.GetType().Name} was already built in this resolution scope for other run types " +
                "or another operation than this request. A scope holds one execution node per configuration/problem reference pair, so every use of that binding must request an execution node " +
                "that the first one also implements.");
    }

    // Called while the construction lock is held. Builds the configuration's execution node and keeps it in its owner's table. A
    // shared configuration is built in a new child scope of its owner, the scope it would have had as a child activation,
    // so its children stay its own.
    private static TExecutionNode Build<TExecutionNode>(Node owner, IConfigurationNode configuration, Func<ResolutionScope, TExecutionNode> create, IProblem? problem)
        where TExecutionNode : class, IExecutionNode
    {
        var building = owner;
        if (owner.Shared?.Contains(configuration) == true)
            building = new Node(owner.Tree, owner);
        var executionNode = create(new ResolutionScope(building));
        // A construction that caught a nested failure must not publish an execution node from the failed tree.
        owner.Tree.ThrowIfFailed();
        owner.Nodes.Add(new(configuration, problem), executionNode);
        return executionNode;
    }

    // One scope: its table, its sharing set and its parent. Every view of a scope, typed or not, shares one node.
    private sealed class Node(Tree tree, Node? parent)
    {
        public Tree Tree { get; } = tree;
        public Node? Parent { get; } = parent;

        // The execution nodes this scope built, by configuration and optional problem reference.
        public Dictionary<NodeKey, IExecutionNode> Nodes { get; } = new();

        // The configurations whose execution node this scope builds for its child scopes too. Null while it shares nothing.
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

    private readonly struct NodeKey(IConfigurationNode configuration, IProblem? problem) : IEquatable<NodeKey>
    {
        private readonly IConfigurationNode configuration = configuration;
        private readonly IProblem? problem = problem;
        public bool Equals(NodeKey other) => ReferenceEquals(configuration, other.configuration) && ReferenceEquals(problem, other.problem);
        public override bool Equals(object? obj) => obj is NodeKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(configuration), problem is null ? 0 : RuntimeHelpers.GetHashCode(problem));
    }
}
