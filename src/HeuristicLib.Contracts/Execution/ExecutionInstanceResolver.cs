using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Execution;

/// <summary>
/// Resolves configuration into execution instances for one run, applying the decorations declared for it.
/// </summary>
/// <remarks>
/// <para>
/// A resolver is the resolution phase of the execution graph. Decorations are declared beforehand, on an
/// <see cref="ExecutionInstanceResolverBuilder"/> handed to <see cref="Create(Action{ExecutionInstanceResolverBuilder})"/>
/// or <see cref="CreateChildResolver(Action{ExecutionInstanceResolverBuilder})"/>. The builder never escapes that call,
/// so a decoration cannot arrive after the instance it was meant to wrap.
/// </para>
/// <para>
/// Resolvers form a tree. A child sees its ancestors' decorations and instances; an ancestor never sees a child's, and
/// siblings never see each other's. Sibling isolation is what expresses per-cycle freshness in meta-algorithms that
/// recreate their execution instances.
/// </para>
/// <para>
/// The resolution rules are documented on <see cref="Resolve"/>.
/// </para>
/// </remarks>
public sealed class ExecutionInstanceResolver
{
    private readonly ExecutionInstanceResolver? parent;
    private readonly ImmutableDictionary<IExecutionInstanceResolvable<IExecutionInstance>, ImmutableArray<Decoration>> decorations;
    private readonly Dictionary<IExecutionInstanceResolvable<IExecutionInstance>, IExecutionInstance> instances = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IExecutionInstanceResolvable<IExecutionInstance>, IExecutionInstance> underConstruction = new(ReferenceEqualityComparer.Instance);

    internal ExecutionInstanceResolver(
        ExecutionInstanceResolver? parent,
        ImmutableDictionary<IExecutionInstanceResolvable<IExecutionInstance>, ImmutableArray<Decoration>> decorations)
    {
        this.parent = parent;
        this.decorations = decorations;
        Depth = parent is null ? 0 : parent.Depth + 1;
    }

    /// <summary>
    /// Gets the distance from the root resolver. A deeper decoration binds more tightly than a shallower one.
    /// </summary>
    internal int Depth { get; }

    /// <summary>
    /// Creates a root resolver that applies no decorations.
    /// </summary>
    public static ExecutionInstanceResolver Create() => new(null, Decoration.None);

    /// <summary>
    /// Creates a root resolver, declaring its decorations first.
    /// </summary>
    /// <param name="declare">Declares what the resolver decorates. The builder is not valid after this returns.</param>
    public static ExecutionInstanceResolver Create(Action<ExecutionInstanceResolverBuilder> declare) =>
        Declare(null, declare);

    /// <summary>
    /// Creates a child resolver that adds no decorations of its own.
    /// </summary>
    /// <remarks>
    /// Use this for a fresh set of execution instances rather than for new behavior. The child still applies every
    /// decoration its ancestors declared, and reuses their instances where the applicable decorations are the same.
    /// </remarks>
    public ExecutionInstanceResolver CreateChildResolver() => new(this, Decoration.None);

    /// <summary>
    /// Creates a child resolver, declaring the decorations it adds to its ancestors'.
    /// </summary>
    /// <param name="declare">Declares what the child adds. The builder is not valid after this returns.</param>
    public ExecutionInstanceResolver CreateChildResolver(Action<ExecutionInstanceResolverBuilder> declare) =>
        Declare(this, declare);

    private static ExecutionInstanceResolver Declare(ExecutionInstanceResolver? parent, Action<ExecutionInstanceResolverBuilder> declare)
    {
        var builder = new ExecutionInstanceResolverBuilder(parent);
        declare(builder);
        return builder.Build();
    }

    /// <summary>
    /// Resolves configuration into the execution instance that this resolver's decorations apply to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The instance handed back was built with exactly the decorations that apply here: every decoration declared by
    /// this resolver or any ancestor, and no others. Two resolvers share an instance when, and only when, the
    /// decorations applying to them are identical — a decoration does not by itself prevent reuse, only a difference in
    /// decorations does.
    /// </para>
    /// <para>
    /// The search walks from this resolver towards the root and stops at the first one that declares a decoration for
    /// the resolvable, because every instance above that point was built from a shorter chain and is not a valid answer
    /// here. Reaching an ancestor without stopping proves that nothing in between contributed a decoration, so that
    /// ancestor's instance was built from the same chain and can be reused.
    /// </para>
    /// <para>
    /// A newly built instance is stored here rather than hoisted to the ancestor that owns the decorations. Hoisting
    /// would let sibling resolvers share, which is precisely what recreating execution instances must not do.
    /// </para>
    /// </remarks>
    public TExecutionInstance Resolve<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance> resolvable)
        where TExecutionInstance : class, IExecutionInstance
    {
        for (var resolver = this; resolver is not null; resolver = resolver.parent)
        {
            // A resolvable currently being wrapped answers with the instance built so far, so that a decoration
            // resolving what it wraps receives that rather than starting the chain again.
            if (resolver.underConstruction.TryGetValue(resolvable, out var partial))
                return (TExecutionInstance)partial;

            if (resolver.instances.TryGetValue(resolvable, out var resolved))
                return (TExecutionInstance)resolved;

            if (resolver.decorations.ContainsKey(resolvable))
                break;
        }

        var instance = Build(resolvable);
        instances.Add(resolvable, instance);
        return (TExecutionInstance)instance;
    }

    /// <summary>
    /// Resolves an operator that may be absent, returning <see langword="null"/> when it is.
    /// </summary>
    /// <remarks>
    /// Use this for optional slots such as a terminator or a refiner. <see cref="Resolve"/> stays strict, so passing a
    /// possibly-null operator to it is a compile-time error rather than a null instance discovered later.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(resolvable))]
    public TExecutionInstance? ResolveOptional<TExecutionInstance>(IExecutionInstanceResolvable<TExecutionInstance>? resolvable)
        where TExecutionInstance : class, IExecutionInstance =>
        resolvable is null ? null : Resolve(resolvable);

    /// <summary>
    /// Gathers every decoration that applies to a resolvable here, ordered innermost first.
    /// </summary>
    /// <remarks>
    /// Ordering is by specificity, so that the more specific wrapper sits closer to what it decorates. Configuration
    /// binds tighter than a hook, so that a wrapper which measures never measures an observer. A deeper resolver binds
    /// tighter than a shallower one, so that the most local budget is the least disturbed. Within one resolver, the
    /// first decoration declared binds tightest and therefore observes first, which is what lets a trace install the
    /// clocks it reads before installing itself.
    /// </remarks>
    private ImmutableArray<Decoration> Chain(IExecutionInstanceResolvable<IExecutionInstance> resolvable)
    {
        var gathered = new List<(Decoration Decoration, int Depth)>();
        for (var resolver = this; resolver is not null; resolver = resolver.parent)
        {
            if (resolver.decorations.TryGetValue(resolvable, out var declared))
            {
                foreach (var decoration in declared)
                    gathered.Add((decoration, resolver.Depth));
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
    /// Builds the instance by folding the applicable decorations over the resolvable, innermost first.
    /// </summary>
    /// <remarks>
    /// A decoration produces a resolvable, and that wrapper resolves what it wraps through this resolver. Each link is
    /// therefore published as under construction while the chain is built, so the wrapper receives the instance already
    /// created for its child instead of rebuilding the chain from the start.
    /// </remarks>
    private IExecutionInstance Build(IExecutionInstanceResolvable<IExecutionInstance> resolvable)
    {
        var chain = Chain(resolvable);
        if (chain.IsEmpty)
            return resolvable.CreateExecutionInstance(this);

        var links = new List<IExecutionInstanceResolvable<IExecutionInstance>>(chain.Length + 1);
        try
        {
            var current = resolvable;
            var instance = resolvable.CreateExecutionInstance(this);
            underConstruction.Add(current, instance);
            links.Add(current);

            foreach (var decoration in chain)
            {
                current = decoration.Apply(current);
                instance = current.CreateExecutionInstance(this);
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
}

/// <summary>
/// Declares the decorations a resolver applies, before that resolver resolves anything.
/// </summary>
/// <remarks>
/// The builder is the declaration phase and has no way to resolve; the resolver it produces is the resolution phase and
/// has no way to declare. It is handed to a declaration callback and is not valid once that callback returns, so
/// decorating something already resolved is not an error to detect but a statement that cannot be written.
/// </remarks>
public sealed class ExecutionInstanceResolverBuilder
{
    private readonly ExecutionInstanceResolver? parent;
    private readonly Dictionary<IExecutionInstanceResolvable<IExecutionInstance>, List<Decoration>> decorations = new(ReferenceEqualityComparer.Instance);
    private DecorationOrigin origin = DecorationOrigin.Configuration;
    private int declared;

    internal ExecutionInstanceResolverBuilder(ExecutionInstanceResolver? parent)
    {
        this.parent = parent;
    }

    /// <summary>
    /// Declares that the resolver wraps whatever it resolves for a resolvable.
    /// </summary>
    /// <remarks>
    /// Decorations compose rather than replace one another, so declaring several at one anchor stacks them all, and a
    /// child resolver's decorations apply on top of its ancestors' rather than displacing them. Declaring the same
    /// decoration twice stacks it twice.
    /// </remarks>
    public ExecutionInstanceResolverBuilder Decorate<TResolvable>(TResolvable resolvable, Func<TResolvable, TResolvable> decorate)
        where TResolvable : class, IExecutionInstanceResolvable<IExecutionInstance>
    {
        if (!decorations.TryGetValue(resolvable, out var declaredHere))
        {
            declaredHere = [];
            decorations[resolvable] = declaredHere;
        }

        declaredHere.Add(new Decoration<TResolvable>(decorate) { Origin = origin, Sequence = declared++ });
        return this;
    }

    /// <summary>
    /// Lets a hook declare its decorations, recording them as observation rather than configuration.
    /// </summary>
    /// <remarks>
    /// Origin is a fact about who installed a decoration rather than something the decoration claims, so a hook cannot
    /// present itself as configuration and end up inside a wrapper that measures it.
    /// </remarks>
    public ExecutionInstanceResolverBuilder Install(IExecutionHook hook)
    {
        origin = DecorationOrigin.Hook;
        try
        {
            hook.Install(this);
        }
        finally
        {
            origin = DecorationOrigin.Configuration;
        }

        return this;
    }

    internal ExecutionInstanceResolver Build()
    {
        var frozen = ImmutableDictionary.CreateBuilder<IExecutionInstanceResolvable<IExecutionInstance>, ImmutableArray<Decoration>>(ReferenceEqualityComparer.Instance);
        foreach (var (resolvable, declaredHere) in decorations)
            frozen.Add(resolvable, [.. declaredHere]);

        return new ExecutionInstanceResolver(parent, frozen.ToImmutable());
    }
}

/// <summary>
/// Who declared a decoration, which decides how tightly it binds.
/// </summary>
public enum DecorationOrigin
{
    /// <summary>Declared by the configuration being executed, such as a budget wrapping the operator it limits.</summary>
    Configuration = 0,

    /// <summary>Declared by a hook installed on the run, such as an analyzer observing an operator.</summary>
    Hook = 1
}

/// <summary>
/// One declared wrapping of a resolvable, kept separate from the others so that its origin and order survive.
/// </summary>
internal abstract class Decoration
{
    /// <summary>Gets the empty declaration set, shared by every resolver that declares nothing.</summary>
    internal static ImmutableDictionary<IExecutionInstanceResolvable<IExecutionInstance>, ImmutableArray<Decoration>> None { get; } =
        ImmutableDictionary.Create<IExecutionInstanceResolvable<IExecutionInstance>, ImmutableArray<Decoration>>(ReferenceEqualityComparer.Instance);

    public required DecorationOrigin Origin { get; init; }

    /// <summary>Gets the position among the decorations declared by one builder, counted across all resolvables.</summary>
    public required int Sequence { get; init; }

    public abstract IExecutionInstanceResolvable<IExecutionInstance> Apply(IExecutionInstanceResolvable<IExecutionInstance> current);
}

internal sealed class Decoration<TResolvable>(Func<TResolvable, TResolvable> decorate) : Decoration
    where TResolvable : class, IExecutionInstanceResolvable<IExecutionInstance>
{
    public override IExecutionInstanceResolvable<IExecutionInstance> Apply(IExecutionInstanceResolvable<IExecutionInstance> current)
    {
        if (current is not TResolvable typed)
        {
            throw new InvalidOperationException(
                $"A decoration declared for '{typeof(TResolvable)}' cannot be applied to '{current.GetType()}'. " +
                "Decorations composing at one anchor must all accept the type the previous one produces.");
        }

        return decorate(typed);
    }
}
