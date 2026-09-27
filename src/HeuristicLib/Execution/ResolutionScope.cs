using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Resolves configuration into execution nodes for one run, applying the decorations declared for it.
/// </summary>
/// <remarks>
/// <para>
/// A scope carries out the resolution phase of the execution graph. Decorations are declared beforehand, on an
/// <see cref="ResolutionScopeBuilder"/> handed to <see cref="Create(Action{ResolutionScopeBuilder})"/>
/// or <see cref="CreateChildScope(Action{ResolutionScopeBuilder})"/>. The declarations are snapshotted when that call
/// returns. Retaining the builder and changing it later cannot change the scope's declarations.
/// </para>
/// <para>
/// Scopes form a tree. A child inherits its ancestors' decorations and can reuse eligible executions already held by
/// them. An ancestor never sees a child's cache, and siblings never see each other's caches. Siblings can both reuse
/// an execution already held by a common ancestor; otherwise each builds and keeps its own execution.
/// </para>
/// <para>
/// The resolution rules are documented on
/// <see cref="Resolve{TConfiguration, TExecution}(TConfiguration, Func{TConfiguration, ResolutionScope, TExecution})"/>.
/// </para>
/// </remarks>
public sealed class ResolutionScope
{
    private readonly ResolutionScope? parent;
    private readonly ImmutableDictionary<IConfigurationNode, ImmutableArray<Decoration>> decorations;
    private readonly Dictionary<IConfigurationNode, IExecutionNode> executions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IConfigurationNode, IExecutionNode> underConstruction = new(ReferenceEqualityComparer.Instance);

    internal ResolutionScope(
        ResolutionScope? parent,
        ImmutableDictionary<IConfigurationNode, ImmutableArray<Decoration>> decorations)
    {
        this.parent = parent;
        this.decorations = decorations;
        Depth = parent is null ? 0 : parent.Depth + 1;
    }

    /// <summary>
    /// Gets the distance from the root scope. A deeper decoration binds more tightly than a shallower one.
    /// </summary>
    internal int Depth { get; }

    /// <summary>
    /// Creates a root scope that applies no decorations.
    /// </summary>
    public static ResolutionScope Create() => new(null, Decoration.None);

    /// <summary>
    /// Creates a root scope, declaring its decorations first.
    /// </summary>
    /// <param name="declare">Declares what the scope decorates. Declarations are snapshotted when the callback returns.</param>
    public static ResolutionScope Create(Action<ResolutionScopeBuilder> declare) =>
        Declare(null, declare);

    /// <summary>
    /// Creates a child scope that adds no decorations of its own.
    /// </summary>
    /// <remarks>
    /// The child applies every decoration its ancestors declared and can reuse eligible executions already held by
    /// them. When none is found, it builds and stores a new execution locally. A fresh scope does not guarantee fresh
    /// executions for configurations already resolved by an ancestor.
    /// </remarks>
    public ResolutionScope CreateChildScope() => new(this, Decoration.None);

    /// <summary>
    /// Creates a child scope, declaring the decorations it adds to its ancestors'.
    /// </summary>
    /// <param name="declare">Declares what the child adds. Declarations are snapshotted when the callback returns.</param>
    public ResolutionScope CreateChildScope(Action<ResolutionScopeBuilder> declare) =>
        Declare(this, declare);

    private static ResolutionScope Declare(ResolutionScope? parent, Action<ResolutionScopeBuilder> declare)
    {
        var builder = new ResolutionScopeBuilder(parent);
        declare(builder);
        return builder.Build();
    }

    /// <summary>
    /// Resolves configuration into the execution node that this scope's decorations apply to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Completed resolution applies every decoration declared for this configuration by this scope or an ancestor,
    /// and no others. Identical decoration chains permit ancestor reuse but do not guarantee sharing: an eligible
    /// execution must already exist when the search reaches that ancestor.
    /// </para>
    /// <para>
    /// The search walks from this scope towards the root. At each scope, an under-construction execution answers first,
    /// then a cached execution. If neither exists and the scope declares a decoration for this configuration, the search
    /// stops: executions above it have a shorter chain. Declarations for other configurations do not stop this search.
    /// Under-construction executions let a wrapper resolve the chain already built for its child.
    /// </para>
    /// <para>
    /// A newly built execution is stored here rather than hoisted to an ancestor. Ancestor lookups cannot find this
    /// entry, and this scope keeps its execution even if an ancestor resolves the same configuration later. Siblings
    /// cannot reuse each other's locally built executions, but can both reuse an eligible common ancestor execution.
    /// </para>
    /// <para>
    /// <paramref name="create"/> performs every creation step: first for the configuration passed in, then for each
    /// decoration of it, innermost first. Every decoration therefore has to produce a <typeparamref name="TConfiguration"/>.
    /// Pass a <see langword="static"/> lambda, so the compiler caches the delegate instead of allocating one per
    /// resolution.
    /// </para>
    /// <para>
    /// A scope serves one execution, so an execution of another type found here was built for a different search
    /// space or problem. That is reported rather than cast.
    /// </para>
    /// </remarks>
    public TExecution Resolve<TConfiguration, TExecution>(TConfiguration configuration, Func<TConfiguration, ResolutionScope, TExecution> create)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        for (var scope = this; scope is not null; scope = scope.parent)
        {
            // A configuration currently being wrapped answers with the execution built so far, so that a decoration
            // resolving what it wraps receives that rather than starting the chain again.
            if (scope.underConstruction.TryGetValue(configuration, out var partial))
                return RequireExecutionOf<TExecution>(configuration, partial);

            if (scope.executions.TryGetValue(configuration, out var resolved))
                return RequireExecutionOf<TExecution>(configuration, resolved);

            if (scope.decorations.ContainsKey(configuration))
                break;
        }

        var execution = Build(configuration, create);
        executions.Add(configuration, execution);
        return execution;
    }

    /// <summary>
    /// Resolves a configuration that creates its own execution node.
    /// </summary>
    /// <remarks>
    /// Convenience over
    /// <see cref="Resolve{TConfiguration, TExecution}(TConfiguration, Func{TConfiguration, ResolutionScope, TExecution})"/>,
    /// which documents the resolution rules.
    /// </remarks>
    public TExecution Resolve<TExecution>(IConfigurationNode<TExecution> configuration)
        where TExecution : class, IExecutionNode =>
        Resolve(configuration, static (target, scope) => target.CreateExecutionInstance(scope));

    /// <summary>
    /// Resolves an operator that may be absent, returning <see langword="null"/> when it is.
    /// </summary>
    /// <remarks>
    /// Use this for optional slots such as a terminator or a refiner.
    /// <see cref="Resolve{TExecution}(IConfigurationNode{TExecution})"/> stays strict, so passing a
    /// possibly-null operator to it is a compile-time error rather than a null execution discovered later.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecution? ResolveOptional<TExecution>(IConfigurationNode<TExecution>? configuration)
        where TExecution : class, IExecutionNode =>
        configuration is null ? null : Resolve(configuration);

    /// <summary>
    /// Gathers every decoration that applies to a configuration here, ordered innermost first.
    /// </summary>
    /// <remarks>
    /// Ordering is by specificity, so that the more specific wrapper sits closer to what it decorates. Configuration
    /// binds tighter than a module, so that a wrapper which measures never measures an observer. A deeper scope binds
    /// tighter than a shallower one, so that the most local budget is the least disturbed. Within the same origin and
    /// scope, the first decoration declared binds tightest. Its successful-operation callback therefore runs first,
    /// which lets a trace install the clocks it reads before installing itself. Entry work runs in the opposite order.
    /// </remarks>
    private ImmutableArray<Decoration> Chain(IConfigurationNode configuration)
    {
        var gathered = new List<(Decoration Decoration, int Depth)>();
        for (var scope = this; scope is not null; scope = scope.parent)
        {
            if (scope.decorations.TryGetValue(configuration, out var declared))
            {
                foreach (var decoration in declared)
                    gathered.Add((decoration, scope.Depth));
            }
        }

        return
        [
            .. gathered
                .OrderBy(entry => entry.Decoration.Origin)
                .ThenByDescending(entry => entry.Depth)
                .ThenBy(entry => entry.Decoration.Sequence)
                .Select(entry => entry.Decoration)
        ];
    }

    /// <summary>
    /// Builds the execution by folding the applicable decorations over the configuration, innermost first.
    /// </summary>
    /// <remarks>
    /// A decoration produces a configuration, and that wrapper resolves what it wraps through this scope. Each link is
    /// therefore published as under construction while the chain is built, so the wrapper receives the execution already
    /// created for its child instead of rebuilding the chain from the start.
    /// </remarks>
    private TExecution Build<TConfiguration, TExecution>(TConfiguration configuration, Func<TConfiguration, ResolutionScope, TExecution> create)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        var chain = Chain(configuration);
        if (chain.IsEmpty)
            return create(configuration, this);

        var links = new List<IConfigurationNode>(chain.Length + 1);
        try
        {
            var current = configuration;
            var execution = create(configuration, this);
            underConstruction.Add(current, execution);
            links.Add(current);

            foreach (var decoration in chain)
            {
                current = RequireStandIn(configuration, decoration.Apply(current));
                execution = create(current, this);
                underConstruction.Add(current, execution);
                links.Add(current);
            }

            return execution;
        }
        finally
        {
            foreach (var link in links)
                underConstruction.Remove(link);
        }
    }

    /// <summary>
    /// Returns a decoration's result as the type the caller creates from, or explains why it cannot stand in for the
    /// configuration it decorates.
    /// </summary>
    private static TConfiguration RequireStandIn<TConfiguration>(TConfiguration configuration, IConfigurationNode decorated)
        where TConfiguration : class, IConfigurationNode
    {
        if (decorated is not TConfiguration typed)
        {
            throw new InvalidOperationException(
                $"{ExecutionSignature.Name(decorated.GetType())} was declared to decorate {ExecutionSignature.Name(configuration.GetType())}, but it is not a {ExecutionSignature.Name(typeof(TConfiguration))} and cannot stand in for it.");
        }

        return typed;
    }

    /// <summary>
    /// Returns an execution already held here as the type the caller asked for, or explains why it is not that type.
    /// </summary>
    /// <remarks>A scope serves one run, so finding an execution of another type here means it was built for a
    /// different search space or problem.</remarks>
    private static TExecution RequireExecutionOf<TExecution>(IConfigurationNode configuration, IExecutionNode execution)
        where TExecution : class, IExecutionNode
    {
        if (execution is not TExecution cached)
        {
            throw new InvalidOperationException(
                $"This scope already holds a {ExecutionSignature.Name(execution.GetType())} for {ExecutionSignature.Name(configuration.GetType())}, which is not a {ExecutionSignature.Name(typeof(TExecution))}. " +
                "A scope serves one run, so resolve over a second search space or problem in its own scope.");
        }

        return cached;
    }
}

/// <summary>
/// A scope that names the candidate, search space and problem once, so that a role binding on that triple does not
/// name them again at every call.
/// </summary>
/// <remarks>Convenience only: every overload forwards to the scope overload of the same name.</remarks>
public class ResolutionScope<TCandidate, TSearchSpace, TProblem>(ResolutionScope scope)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public ResolutionScope Scope => scope;
}

/// <summary>
/// A scope that also names the search state, for an algorithm resolving terminators and interceptors alongside its
/// other operators.
/// </summary>
/// <remarks>
/// Terminators and interceptors name only their candidate, so the state they run over comes from the scope rather
/// than from the call site.
/// </remarks>
public sealed class ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState>(ResolutionScope scope)
    : ResolutionScope<TCandidate, TSearchSpace, TProblem>(scope)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
    where TSearchState : class, ISearchState;

/// <summary>
/// Declares the decorations a scope applies, before that scope resolves anything.
/// </summary>
/// <remarks>
/// The builder is the declaration phase and has no way to resolve; the scope it produces is the resolution phase and
/// has no way to declare. Use it during the declaration callback. The scope receives a snapshot when the callback
/// returns, so later declarations on a retained builder cannot affect that scope.
/// </remarks>
public sealed class ResolutionScopeBuilder
{
    private readonly ResolutionScope? parent;
    private readonly Dictionary<IConfigurationNode, List<Decoration>> decorations = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IExecutionModule> installedModules = new(ReferenceEqualityComparer.Instance);
    private DecorationOrigin origin = DecorationOrigin.Configuration;
    private int declared;

    internal ResolutionScopeBuilder(ResolutionScope? parent)
    {
        this.parent = parent;
    }

    /// <summary>
    /// Declares that the scope wraps whatever it resolves for a configuration.
    /// </summary>
    /// <remarks>
    /// Decorations compose rather than replace one another, so declaring several for one configuration stacks them all, and a
    /// child scope's decorations apply on top of its ancestors' rather than displacing them. Declaring the same
    /// decoration twice stacks it twice.
    /// </remarks>
    public ResolutionScopeBuilder Decorate<TConfiguration>(TConfiguration configuration, Func<TConfiguration, TConfiguration> decorate)
        where TConfiguration : class, IConfigurationNode
    {
        if (!decorations.TryGetValue(configuration, out var declaredHere))
        {
            declaredHere = [];
            decorations[configuration] = declaredHere;
        }

        declaredHere.Add(new Decoration<TConfiguration>(decorate) { Origin = origin, Sequence = declared++ });
        return this;
    }

    /// <summary>
    /// Lets a module declare its decorations, recording them with module origin rather than configuration origin.
    /// </summary>
    /// <remarks>
    /// Origin is a fact about who installed a decoration rather than something the decoration claims, so a module cannot
    /// present itself as configuration and end up inside a wrapper that measures it.
    /// </remarks>
    public ResolutionScopeBuilder Install(IExecutionModule module)
    {
        if (!installedModules.Add(module))
            return this;

        var previousOrigin = origin;
        origin = DecorationOrigin.Module;
        try
        {
            module.Install(this);
        }
        finally
        {
            origin = previousOrigin;
        }

        return this;
    }

    internal ResolutionScope Build()
    {
        var frozen = ImmutableDictionary.CreateBuilder<IConfigurationNode, ImmutableArray<Decoration>>(ReferenceEqualityComparer.Instance);
        foreach (var (configuration, declaredHere) in decorations)
            frozen.Add(configuration, [.. declaredHere]);

        return new ResolutionScope(parent, frozen.ToImmutable());
    }
}

/// <summary>
/// Who declared a decoration, which decides how tightly it binds.
/// </summary>
public enum DecorationOrigin
{
    /// <summary>Declared by the configuration being executed, such as a budget wrapping the operator it limits.</summary>
    Configuration = 0,

    /// <summary>Declared by a module or an analyzer attached to the run, such as a trace observing an operator.</summary>
    Module = 1
}

/// <summary>
/// One declared wrapping of a configuration, kept separate from the others so that its origin and order survive.
/// </summary>
internal abstract class Decoration
{
    /// <summary>Gets the empty declaration set, shared by every scope that declares nothing.</summary>
    internal static ImmutableDictionary<IConfigurationNode, ImmutableArray<Decoration>> None { get; } =
        ImmutableDictionary.Create<IConfigurationNode, ImmutableArray<Decoration>>(ReferenceEqualityComparer.Instance);

    public required DecorationOrigin Origin { get; init; }

    /// <summary>Gets the position among the decorations declared by one builder, counted across all configurations.</summary>
    public required int Sequence { get; init; }

    public abstract IConfigurationNode Apply(IConfigurationNode current);
}

internal sealed class Decoration<TConfiguration>(Func<TConfiguration, TConfiguration> decorate) : Decoration
    where TConfiguration : class, IConfigurationNode
{
    public override IConfigurationNode Apply(IConfigurationNode current)
    {
        if (current is not TConfiguration typed)
        {
            throw new InvalidOperationException(
                $"A decoration declared for '{typeof(TConfiguration)}' cannot be applied to '{current.GetType()}'. " +
                "Decorations composing for one configuration must all accept the type the previous one produces.");
        }

        return decorate(typed);
    }
}

/// <summary>Creates a typed scope over a resolution scope, naming the triple once per creation method.</summary>
public static class ResolutionScopeExtensions
{
    extension(ResolutionScope scope)
    {
        public ResolutionScope<TCandidate, TSearchSpace, TProblem> For<TCandidate, TSearchSpace, TProblem>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> => new(scope);

        /// <summary>
        /// Names the search state as well, which an algorithm knows because it is the state the algorithm produces.
        /// The result still serves every role that binds on the triple alone.
        /// </summary>
        public ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> For<TCandidate, TSearchSpace, TProblem, TSearchState>()
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace>
            where TSearchState : class, ISearchState => new(scope);
    }
}
