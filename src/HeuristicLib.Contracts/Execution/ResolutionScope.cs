using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Resolves configuration into execution instances for one run, applying the decorations declared for it.
/// </summary>
/// <remarks>
/// <para>
/// A scope carries out the resolution phase of the execution graph. Decorations are declared beforehand, on an
/// <see cref="ResolutionScopeBuilder"/> handed to <see cref="Create(Action{ResolutionScopeBuilder})"/>
/// or <see cref="CreateChildScope(Action{ResolutionScopeBuilder})"/>. The builder never escapes that call,
/// so a decoration cannot arrive after the instance it was meant to wrap.
/// </para>
/// <para>
/// Scopes form a tree. A child sees its ancestors' decorations and instances; an ancestor never sees a child's, and
/// siblings never see each other's. Sibling isolation is what expresses per-cycle freshness in meta-algorithms that
/// recreate their execution instances.
/// </para>
/// <para>
/// The resolution rules are documented on
/// <see cref="Resolve{TConfiguration, TExecutionInstance}(TConfiguration, Func{TConfiguration, ResolutionScope, TExecutionInstance})"/>.
/// </para>
/// </remarks>
public sealed class ResolutionScope
{
    private readonly ResolutionScope? parent;
    private readonly ImmutableDictionary<IExecutionConfiguration, ImmutableArray<Decoration>> decorations;
    private readonly Dictionary<IExecutionConfiguration, IExecutionInstance> instances = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IExecutionConfiguration, IExecutionInstance> underConstruction = new(ReferenceEqualityComparer.Instance);

    internal ResolutionScope(
        ResolutionScope? parent,
        ImmutableDictionary<IExecutionConfiguration, ImmutableArray<Decoration>> decorations)
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
    /// <param name="declare">Declares what the scope decorates. The builder is not valid after this returns.</param>
    public static ResolutionScope Create(Action<ResolutionScopeBuilder> declare) =>
        Declare(null, declare);

    /// <summary>
    /// Creates a child scope that adds no decorations of its own.
    /// </summary>
    /// <remarks>
    /// Use this for a fresh set of execution instances rather than for new behavior. The child still applies every
    /// decoration its ancestors declared, and reuses their instances where the applicable decorations are the same.
    /// </remarks>
    public ResolutionScope CreateChildScope() => new(this, Decoration.None);

    /// <summary>
    /// Creates a child scope, declaring the decorations it adds to its ancestors'.
    /// </summary>
    /// <param name="declare">Declares what the child adds. The builder is not valid after this returns.</param>
    public ResolutionScope CreateChildScope(Action<ResolutionScopeBuilder> declare) =>
        Declare(this, declare);

    private static ResolutionScope Declare(ResolutionScope? parent, Action<ResolutionScopeBuilder> declare)
    {
        var builder = new ResolutionScopeBuilder(parent);
        declare(builder);
        return builder.Build();
    }

    /// <summary>
    /// Resolves configuration into the execution instance that this scope's decorations apply to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The instance handed back was built with exactly the decorations that apply here: every decoration declared by
    /// this scope or any ancestor, and no others. Two scopes share an instance when, and only when, the
    /// decorations applying to them are identical — a decoration does not by itself prevent reuse, only a difference in
    /// decorations does.
    /// </para>
    /// <para>
    /// The search walks from this scope towards the root and stops at the first one that declares a decoration for
    /// the configuration, because every instance above that point was built from a shorter chain and is not a valid answer
    /// here. Reaching an ancestor without stopping proves that nothing in between contributed a decoration, so that
    /// ancestor's instance was built from the same chain and can be reused.
    /// </para>
    /// <para>
    /// A newly built instance is stored here rather than hoisted to the ancestor that owns the decorations. Hoisting
    /// would let sibling scopes share, which is precisely what recreating execution instances must not do.
    /// </para>
    /// <para>
    /// <paramref name="create"/> performs every creation step: first for the configuration passed in, then for each
    /// decoration of it, innermost first. Every decoration therefore has to produce a <typeparamref name="TConfiguration"/>.
    /// Pass a <see langword="static"/> lambda, so the compiler caches the delegate instead of allocating one per
    /// resolution.
    /// </para>
    /// <para>
    /// A scope serves one execution, so an instance of another type found here was built for a different search
    /// space or problem. That is reported rather than cast.
    /// </para>
    /// </remarks>
    public TExecutionInstance Resolve<TConfiguration, TExecutionInstance>(TConfiguration configuration, Func<TConfiguration, ResolutionScope, TExecutionInstance> create)
        where TConfiguration : class, IExecutionConfiguration
        where TExecutionInstance : class, IExecutionInstance
    {
        for (var scope = this; scope is not null; scope = scope.parent)
        {
            // A configuration currently being wrapped answers with the instance built so far, so that a decoration
            // resolving what it wraps receives that rather than starting the chain again.
            if (scope.underConstruction.TryGetValue(configuration, out var partial))
                return RequireInstanceOf<TExecutionInstance>(configuration, partial);

            if (scope.instances.TryGetValue(configuration, out var resolved))
                return RequireInstanceOf<TExecutionInstance>(configuration, resolved);

            if (scope.decorations.ContainsKey(configuration))
                break;
        }

        var instance = Build(configuration, create);
        instances.Add(configuration, instance);
        return instance;
    }

    /// <summary>
    /// Resolves a configuration that creates its own execution instance.
    /// </summary>
    /// <remarks>
    /// Convenience over
    /// <see cref="Resolve{TConfiguration, TExecutionInstance}(TConfiguration, Func{TConfiguration, ResolutionScope, TExecutionInstance})"/>,
    /// which documents the resolution rules.
    /// </remarks>
    public TExecutionInstance Resolve<TExecutionInstance>(IExecutionConfiguration<TExecutionInstance> configuration)
        where TExecutionInstance : class, IExecutionInstance =>
        Resolve(configuration, static (target, scope) => target.CreateExecutionInstance(scope));

    /// <summary>
    /// Resolves an operator that may be absent, returning <see langword="null"/> when it is.
    /// </summary>
    /// <remarks>
    /// Use this for optional slots such as a terminator or a refiner.
    /// <see cref="Resolve{TExecutionInstance}(IExecutionConfiguration{TExecutionInstance})"/> stays strict, so passing a
    /// possibly-null operator to it is a compile-time error rather than a null instance discovered later.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecutionInstance? ResolveOptional<TExecutionInstance>(IExecutionConfiguration<TExecutionInstance>? configuration)
        where TExecutionInstance : class, IExecutionInstance =>
        configuration is null ? null : Resolve(configuration);

    /// <summary>
    /// Gathers every decoration that applies to a configuration here, ordered innermost first.
    /// </summary>
    /// <remarks>
    /// Ordering is by specificity, so that the more specific wrapper sits closer to what it decorates. Configuration
    /// binds tighter than a module, so that a wrapper which measures never measures an observer. A deeper scope binds
    /// tighter than a shallower one, so that the most local budget is the least disturbed. Within one scope, the
    /// first decoration declared binds tightest and therefore observes first, which is what lets a trace install the
    /// clocks it reads before installing itself.
    /// </remarks>
    private ImmutableArray<Decoration> Chain(IExecutionConfiguration configuration)
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
    /// Builds the instance by folding the applicable decorations over the configuration, innermost first.
    /// </summary>
    /// <remarks>
    /// A decoration produces a configuration, and that wrapper resolves what it wraps through this scope. Each link is
    /// therefore published as under construction while the chain is built, so the wrapper receives the instance already
    /// created for its child instead of rebuilding the chain from the start.
    /// </remarks>
    private TExecutionInstance Build<TConfiguration, TExecutionInstance>(TConfiguration configuration, Func<TConfiguration, ResolutionScope, TExecutionInstance> create)
        where TConfiguration : class, IExecutionConfiguration
        where TExecutionInstance : class, IExecutionInstance
    {
        var chain = Chain(configuration);
        if (chain.IsEmpty)
            return create(configuration, this);

        var links = new List<IExecutionConfiguration>(chain.Length + 1);
        try
        {
            var current = configuration;
            var instance = create(configuration, this);
            underConstruction.Add(current, instance);
            links.Add(current);

            foreach (var decoration in chain)
            {
                current = RequireStandIn(configuration, decoration.Apply(current));
                instance = create(current, this);
                underConstruction.Add(current, instance);
                links.Add(current);
            }

            return instance;
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
    private static TConfiguration RequireStandIn<TConfiguration>(TConfiguration configuration, IExecutionConfiguration decorated)
        where TConfiguration : class, IExecutionConfiguration
    {
        if (decorated is not TConfiguration typed)
        {
            throw new InvalidOperationException(
                $"{ExecutionSignature.Name(decorated.GetType())} was declared to decorate {ExecutionSignature.Name(configuration.GetType())}, but it is not a {ExecutionSignature.Name(typeof(TConfiguration))} and cannot stand in for it.");
        }

        return typed;
    }

    /// <summary>
    /// Returns an instance already held here as the type the caller asked for, or explains why it is not that type.
    /// </summary>
    /// <remarks>A scope serves one run, so finding an instance of another type here means it was built for a
    /// different search space or problem.</remarks>
    private static TExecutionInstance RequireInstanceOf<TExecutionInstance>(IExecutionConfiguration configuration, IExecutionInstance instance)
        where TExecutionInstance : class, IExecutionInstance
    {
        if (instance is not TExecutionInstance cached)
        {
            throw new InvalidOperationException(
                $"This scope already holds a {ExecutionSignature.Name(instance.GetType())} for {ExecutionSignature.Name(configuration.GetType())}, which is not a {ExecutionSignature.Name(typeof(TExecutionInstance))}. " +
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
/// has no way to declare. It is handed to a declaration callback and is not valid once that callback returns, so
/// decorating something already resolved is not an error to detect but a statement that cannot be written.
/// </remarks>
public sealed class ResolutionScopeBuilder
{
    private readonly ResolutionScope? parent;
    private readonly Dictionary<IExecutionConfiguration, List<Decoration>> decorations = new(ReferenceEqualityComparer.Instance);
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
        where TConfiguration : class, IExecutionConfiguration
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
    /// Lets a module declare its decorations, recording them as observation rather than configuration.
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
        var frozen = ImmutableDictionary.CreateBuilder<IExecutionConfiguration, ImmutableArray<Decoration>>(ReferenceEqualityComparer.Instance);
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

    /// <summary>Declared by a module installed on the run, such as an analyzer observing an operator.</summary>
    Module = 1
}

/// <summary>
/// One declared wrapping of a configuration, kept separate from the others so that its origin and order survive.
/// </summary>
internal abstract class Decoration
{
    /// <summary>Gets the empty declaration set, shared by every scope that declares nothing.</summary>
    internal static ImmutableDictionary<IExecutionConfiguration, ImmutableArray<Decoration>> None { get; } =
        ImmutableDictionary.Create<IExecutionConfiguration, ImmutableArray<Decoration>>(ReferenceEqualityComparer.Instance);

    public required DecorationOrigin Origin { get; init; }

    /// <summary>Gets the position among the decorations declared by one builder, counted across all configurations.</summary>
    public required int Sequence { get; init; }

    public abstract IExecutionConfiguration Apply(IExecutionConfiguration current);
}

internal sealed class Decoration<TConfiguration>(Func<TConfiguration, TConfiguration> decorate) : Decoration
    where TConfiguration : class, IExecutionConfiguration
{
    public override IExecutionConfiguration Apply(IExecutionConfiguration current)
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
