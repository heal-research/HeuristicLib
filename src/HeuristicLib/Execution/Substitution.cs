using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators;

namespace HEAL.HeuristicLib.Execution;

/// <summary>What to match in a configuration graph and what to put in its place.</summary>
/// <remarks>
/// A substitution maps one configuration graph to another and stores nothing in the configuration it returns, so its
/// delegates are not configuration data. A substitution is never stored or serialized; persist the configuration it
/// produced, an ordinary graph containing the nodes it placed. The type argument of each factory says which nodes
/// are considered.
/// </remarks>
public sealed class Substitution : ISubstitution
{
    private readonly IReadOnlyList<Substitution> expansion;
    private readonly Func<IConfigurationNode, bool> selects;
    private readonly Func<IConfigurationNode, IConfigurationNode> replace;

    private Substitution(Type nodeType, Func<IConfigurationNode, bool> selects, Func<IConfigurationNode, IConfigurationNode> replace, IConfigurationNode? target)
    {
        expansion = [this];
        NodeType = nodeType;
        this.selects = selects;
        this.replace = replace;
        Target = target;
    }

    // The type the replacement function works on, which decides whether a slot is typed for another operation.
    internal Type NodeType { get; }

    // The configuration a placement names; a substitution with a target reports an error when it matches nothing.
    internal IConfigurationNode? Target { get; }

    internal bool Selects(IConfigurationNode node) => selects(node);

    internal IConfigurationNode Replace(IConfigurationNode node) => replace(node);

    /// <summary>Returns the cached one-element list containing this substitution.</summary>
    public IReadOnlyList<Substitution> Expand() => expansion;

    /// <summary>Replaces every node of the type <typeparamref name="TNode"/> the predicate selects.</summary>
    /// <remarks>
    /// A predicate that selects nothing changes nothing. In a predicate, <c>op == x</c> compares references when
    /// <c>op</c> is typed by an interface; use <c>op.Equals(x)</c> or a parameter typed by the concrete record for value
    /// equality.
    /// </remarks>
    /// <param name="selects">Tests nodes from the graph the substitution call received.</param>
    /// <param name="replace">Receives each selected node with its children already substituted.</param>
    public static Substitution Of<TNode>(Func<TNode, bool> selects, Func<TNode, TNode> replace)
        where TNode : class, IConfigurationNode =>
        new(typeof(TNode), node => node is TNode typed && selects(typed), node => replace((TNode)node), null);

    /// <summary>Replaces every node of the type <typeparamref name="TNode"/>, which the lambda's parameter type states.</summary>
    /// <param name="replace">Receives each matched node with its children already substituted.</param>
    public static Substitution Of<TNode>(Func<TNode, TNode> replace)
        where TNode : class, IConfigurationNode =>
        new(typeof(TNode), static node => node is TNode, node => replace((TNode)node), null);

    /// <summary>Replaces the given configuration, which it is an error not to find.</summary>
    /// <param name="target">The configuration to match in the graph the substitution call received.</param>
    /// <param name="replace">Receives the matched node with its children already substituted.</param>
    /// <param name="comparer">
    /// Decides what counts as the target. <see langword="null"/> means reference equality, unlike the usual .NET default:
    /// a caller who supplies an operator means that exact object, and slots left at their defaults are value-equal even in
    /// differently configured algorithms. Every reachable node the comparer considers equal is replaced.
    /// </param>
    public static Substitution Of<TNode>(TNode target, Func<TNode, TNode> replace, IEqualityComparer<IConfigurationNode>? comparer = null)
        where TNode : class, IConfigurationNode
    {
        var equality = comparer ?? ReferenceEqualityComparer.Instance;
        return new(typeof(TNode), node => node is TNode && equality.Equals(node, target), node => replace((TNode)node), target);
    }

    /// <inheritdoc cref="Of{TNode}(TNode, Func{TNode, TNode}, IEqualityComparer{IConfigurationNode})"/>
    /// <remarks>Works through the operator's operation, so a target in a concretely typed variable can be wrapped.</remarks>
    public static Substitution Of<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target,
        Func<IOperator<TOperation, TCandidate>, IOperator<TOperation, TCandidate>> replace, IEqualityComparer<IConfigurationNode>? comparer = null)
        where TOperation : IOperation<TOperation> =>
        Of<IOperator<TOperation, TCandidate>>(target, replace, comparer);

    /// <inheritdoc cref="Of{TNode}(TNode, Func{TNode, TNode}, IEqualityComparer{IConfigurationNode})"/>
    /// <remarks>Works through the algorithm's interface, so a target in a concretely typed variable can be wrapped.</remarks>
    public static Substitution Of<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> target,
        Func<IAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> replace, IEqualityComparer<IConfigurationNode>? comparer = null)
        where TSearchState : class, ISearchState =>
        Of<IAlgorithm<TCandidate, TSearchState>>(target, replace, comparer);
}

/// <summary>Produces a configuration graph with other nodes in place of the selected ones.</summary>
/// <remarks>
/// <para>
/// The result is an ordinary configuration, equal by value to the same graph written by hand, and the receiver is not
/// changed. Only nodes that change, and the path from the receiver to them, are copied; everything else keeps its
/// reference, and the same instance is returned when nothing changed.
/// </para>
/// <para>
/// Several substitutions apply one after another, each seeing the result of the previous one, so a later substitution on
/// the same node sits closer to it. A <see cref="Substitution"/> instance listed twice applies once, at its last
/// position. One call resolves every target against the graph it received: a node stays itself for matching while only
/// its children change. Separate calls carry no such memory, so a reference an earlier call copied no longer matches.
/// </para>
/// <para>
/// A substitution reaches declared children only. A node that declares no parameters is a leaf, and what a delegate
/// creates is out of reach.
/// </para>
/// </remarks>
public static class SubstitutionExtensions
{
    extension<TRoot>(TRoot root) where TRoot : class, IConfigurationNode
    {
        /// <summary>Expands and applies the substitutions, keeping each single substitution only at its last position.</summary>
        /// <param name="substitutions">Parts in application order; later wrappers sit closer to their targets.</param>
        /// <returns>The rewritten graph, or the receiver itself when nothing changes.</returns>
        /// <exception cref="InvalidOperationException">
        /// A named target is unreachable, a replacement does not fit its slot, or a group contains itself.
        /// </exception>
        public TRoot Substitute(params IReadOnlyList<ISubstitution> substitutions) =>
            SubstitutionCall.Apply(root, [.. substitutions.SelectMany(static substitution => substitution.Expand())]);

        /// <inheritdoc cref="Substitution.Of{TNode}(Func{TNode, bool}, Func{TNode, TNode})"/>
        public TRoot Substitute<TNode>(Func<TNode, bool> selects, Func<TNode, TNode> replace)
            where TNode : class, IConfigurationNode =>
            SubstitutionCall.Apply(root, [Substitution.Of(selects, replace)]);

        /// <inheritdoc cref="Substitution.Of{TNode}(Func{TNode, TNode})"/>
        public TRoot Substitute<TNode>(Func<TNode, TNode> replace)
            where TNode : class, IConfigurationNode =>
            SubstitutionCall.Apply(root, [Substitution.Of(replace)]);

        /// <inheritdoc cref="Substitution.Of{TNode}(TNode, Func{TNode, TNode}, IEqualityComparer{IConfigurationNode})"/>
        public TRoot Substitute<TNode>(TNode target, Func<TNode, TNode> replace, IEqualityComparer<IConfigurationNode>? comparer = null)
            where TNode : class, IConfigurationNode =>
            SubstitutionCall.Apply(root, [Substitution.Of(target, replace, comparer)]);

        /// <inheritdoc cref="Substitution.Of{TOperation, TCandidate}(IOperator{TOperation, TCandidate}, Func{IOperator{TOperation, TCandidate}, IOperator{TOperation, TCandidate}}, IEqualityComparer{IConfigurationNode})"/>
        public TRoot Substitute<TOperation, TCandidate>(IOperator<TOperation, TCandidate> target,
            Func<IOperator<TOperation, TCandidate>, IOperator<TOperation, TCandidate>> replace, IEqualityComparer<IConfigurationNode>? comparer = null)
            where TOperation : IOperation<TOperation> =>
            SubstitutionCall.Apply(root, [Substitution.Of(target, replace, comparer)]);

        /// <inheritdoc cref="Substitution.Of{TCandidate, TSearchState}(IAlgorithm{TCandidate, TSearchState}, Func{IAlgorithm{TCandidate, TSearchState}, IAlgorithm{TCandidate, TSearchState}}, IEqualityComparer{IConfigurationNode})"/>
        public TRoot Substitute<TCandidate, TSearchState>(IAlgorithm<TCandidate, TSearchState> target,
            Func<IAlgorithm<TCandidate, TSearchState>, IAlgorithm<TCandidate, TSearchState>> replace, IEqualityComparer<IConfigurationNode>? comparer = null)
            where TSearchState : class, ISearchState =>
            SubstitutionCall.Apply(root, [Substitution.Of(target, replace, comparer)]);
    }
}

// One call of Substitute: its substitutions in order, and the map from copy to original that lets a node stay itself for
// matching while an earlier substitution of the same call changes its children.
internal sealed class SubstitutionCall
{
    private readonly Dictionary<IConfigurationNode, IConfigurationNode> originals = new(ReferenceEqualityComparer.Instance);

    public static TRoot Apply<TRoot>(TRoot root, IReadOnlyList<Substitution> substitutions)
        where TRoot : class, IConfigurationNode
    {
        var call = new SubstitutionCall();
        IConfigurationNode current = root;
        foreach (var substitution in LastOccurrences(substitutions))
            current = new Pass(call, substitution).Run(current, typeof(TRoot));
        return (TRoot)current;
    }

    private static List<Substitution> LastOccurrences(IReadOnlyList<Substitution> substitutions)
    {
        var seen = new HashSet<Substitution>(ReferenceEqualityComparer.Instance);
        var ordered = new List<Substitution>(substitutions.Count);
        for (var index = substitutions.Count - 1; index >= 0; index--)
            if (seen.Add(substitutions[index]))
                ordered.Add(substitutions[index]);
        ordered.Reverse();
        return ordered;
    }

    // The node as the call received it, which is what a predicate sees.
    private IConfigurationNode Original(IConfigurationNode node) => originals.GetValueOrDefault(node, node);

    // One substitution's walk: children first, each node of its input once, never entering its own replacements.
    private sealed class Pass(SubstitutionCall call, Substitution substitution) : ISlotRewriter
    {
        private readonly Dictionary<IConfigurationNode, Visited> visited = new(ReferenceEqualityComparer.Instance);
        private readonly List<string> boundaries = [];

        // Whether a slot at or below the node being visited holds the replacement. It belongs to the visit rather than
        // to the pass, because a reference list entry visits a subtree without making it a place in the graph.
        private bool placed;

        public IConfigurationNode Run(IConfigurationNode root, Type rootType)
        {
            var result = Place(root, rootType, "the receiver");
            if (substitution.Target is { } target && !placed)
                throw NotFound(target, root);
            return result;
        }

        private Visited Visit(IConfigurationNode node)
        {
            if (visited.TryGetValue(node, out var known))
                return known;
            var placedAround = placed;
            placed = false;
            var rewritten = node;
            foreach (var parameter in node.GetParameters())
                rewritten = parameter.Rewrite(rewritten, this);
            var placedBelow = placed;
            placed = placedAround;
            if (!ReferenceEquals(rewritten, node))
                call.originals[rewritten] = call.Original(node);
            var selected = substitution.Selects(call.Original(node));
            var result = new Visited(rewritten, selected ? substitution.Replace(rewritten) : rewritten, selected, placedBelow);
            visited.Add(node, result);
            return result;
        }

        public IConfigurationNode Slot(IConfigurationNode owner, string name, Type slotType, IConfigurationNode child) =>
            Place(child, slotType, $"{ExecutionSignature.Name(owner.GetType())}.{name}");

        private IConfigurationNode Place(IConfigurationNode node, Type slotType, string slot)
        {
            var entry = Visit(node);
            placed |= entry.PlacedBelow;
            if (!entry.Selected)
                return entry.Rewritten;
            // A slot typed for another operation of the same configuration keeps the configuration, which the scope's
            // table then resolves to one executor for all its operations. A changed child still copies it as on any
            // other path, and the wrapper in the matching slot holds that same copy.
            if (!slotType.IsAssignableTo(substitution.NodeType) && !substitution.NodeType.IsAssignableTo(slotType))
                return entry.Rewritten;
            if (!slotType.IsInstanceOfType(entry.Replaced))
                throw new InvalidOperationException($"{slot} is typed {ExecutionSignature.Name(slotType)} and cannot hold the replacement " +
                    $"{ExecutionSignature.Name(entry.Replaced.GetType())}. Type the slot or the receiver by the interface its role declares, so that it can hold a wrapper.");
            placed = true;
            return entry.Replaced;
        }

        // An entry keeps naming its configuration, or the copy made of it, while the replacement still contains it. Otherwise
        // it follows the replacement.
        public IConfigurationNode Reference(string name, IConfigurationNode entry)
        {
            var replaced = Visit(entry).Replaced;
            if (ReferenceEquals(replaced, entry))
                return entry;
            return FindCopyOf(call.Original(entry), replaced, new HashSet<IConfigurationNode>(ReferenceEqualityComparer.Instance)) ?? replaced;
        }

        // The node below or at the given one that is the original or a copy of it this call made, if any.
        private IConfigurationNode? FindCopyOf(IConfigurationNode original, IConfigurationNode within, HashSet<IConfigurationNode> searched)
        {
            if (!searched.Add(within))
                return null;
            if (ReferenceEquals(call.Original(within), original))
                return within;
            foreach (var parameter in within.GetParameters())
                foreach (var child in parameter.GetChildren(within))
                    if (FindCopyOf(original, child, searched) is { } found)
                        return found;
            return null;
        }

        public void Boundary(IConfigurationNode owner, string name) =>
            boundaries.Add($"{ExecutionSignature.Name(owner.GetType())}.{name}");

        private InvalidOperationException NotFound(IConfigurationNode target, IConfigurationNode root)
        {
            var delegates = boundaries.Count == 0
                ? ""
                : $" The substitution cannot enter what these delegates create: {string.Join(", ", boundaries.Distinct())}.";
            return new InvalidOperationException($"{ExecutionSignature.Name(target.GetType())} is not reachable from {ExecutionSignature.Name(root.GetType())} " +
                $"through declared parameters, so this placement selects nothing.{delegates} A reference matches exactly that object, and it stops matching once an " +
                "earlier Substitute call copied it, which happens to every ancestor of what that call changed. Three routes reach the node: pass all substitutions " +
                "in one Substitute call, which resolves every target against the same input; substitute ancestors first; or name the target from the current " +
                "configuration, as in limited.Algorithm.Iterations for a budget. CreateRun places substitutions into the configuration it receives; name targets from that configuration.");
        }

        private readonly record struct Visited(IConfigurationNode Rewritten, IConfigurationNode Replaced, bool Selected, bool PlacedBelow);
    }
}
