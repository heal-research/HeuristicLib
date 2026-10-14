using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Execution;

/// <remarks>
/// Prepared factories belong to configuration references in a state and are reused from ancestors. Each resolution
/// constructs fresh nodes using the factory owner's state and the requesting aspects. A leaf may return its prepared node.
/// The resolver provides no disposal or concurrency guarantees.
/// </remarks>
public sealed class ResolutionScope
{
    private readonly State state;
    private readonly ImmutableArray<Aspect> aspects;
    // Flattened, ordered cache of individual rule aspects from these aspects and their installed aspects.
    // Aspect identities are shared; prepared advice delegates belong to the owning State.
    private readonly ImmutableArray<Aspect> advisingAspects;

    private ResolutionScope(State state, ImmutableArray<Aspect> aspects)
        : this(state, aspects, Aspect.Collect(aspects)) { }

    private ResolutionScope(State state, ImmutableArray<Aspect> aspects, ImmutableArray<Aspect> advisingAspects)
    {
        this.state = state;
        this.aspects = aspects;
        this.advisingAspects = advisingAspects;
    }

    /// <remarks>Aspects form one layer in the supplied order. An empty collection creates an unadvised scope.</remarks>
    public static ResolutionScope Create(params IReadOnlyList<Aspect> aspects) =>
        new(new State(null), [.. aspects.Distinct<Aspect>(ReferenceEqualityComparer.Instance)]);
    public static ResolutionScope Create(Action<AspectBuilder> declare) => Create(Aspect.Create(declare));

    /// <remarks>Creates child-local state and places the supplied aspect layer before the existing layers.</remarks>
    public ResolutionScope CreateChildScope(params IReadOnlyList<Aspect> added) =>
        new(new State(state), [.. added.Concat(aspects).Distinct<Aspect>(ReferenceEqualityComparer.Instance)]);
    public ResolutionScope CreateChildScope(Action<AspectBuilder> declare) => CreateChildScope(Aspect.Create(declare));

    /// <remarks>Shares state and places the supplied aspects, in their order, before the existing layers.</remarks>
    public ResolutionScope With(params IReadOnlyList<Aspect> added) =>
        new(state, [.. added.Concat(aspects).Distinct<Aspect>(ReferenceEqualityComparer.Instance)]);
    public ResolutionScope With(Action<AspectBuilder> declare) => With(Aspect.Create(declare));

    // Meta-algorithms retain these state-only views, then apply the requesting context to each activation.
    /// <summary>Creates child-local state without retaining the current aspect context.</summary>
    /// <remarks>Retain this view in prepared meta-algorithm state, then use WithAspectsFrom for each activation.</remarks>
    public ResolutionScope CreateChildScopeWithoutAspects() => new(new State(state), [], []);
    /// <summary>Uses this state with the requesting scope's exact aspect collection.</summary>
    /// <remarks>Replaces this view's aspects without adding a layer or changing either scope.</remarks>
    public ResolutionScope WithAspectsFrom(ResolutionScope requestingScope) =>
        new(state, requestingScope.aspects, requestingScope.advisingAspects);

    public TExecution Resolve<TConfiguration, TExecution>(TConfiguration configuration,
        Func<TConfiguration, ExecutionFactory<TExecution>> prepareFactory, Func<IConfigurationNode, Advice<TExecution>> prepareAdvice)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode => Construct(configuration, prepareFactory, prepareAdvice).Advised;

    public TExecution Resolve<TExecution>(IConfigurationNode<TExecution> configuration)
        where TExecution : class, IExecutionNode =>
        Resolve(configuration, static source => source.CreateExecutionFactory(), UnavailableAdvice<TExecution>);

    /// <remarks>
    /// Controls project from the raw execution only after the advised chain succeeds. Invoke operations through the
    /// returned chain. Configured wrappers expose their own controls; projection does not search their children.
    /// </remarks>
    public TExecution Resolve<TConfiguration, TExecution, TControl>(TConfiguration configuration,
        Func<TConfiguration, ExecutionFactory<TExecution>> prepareFactory, Func<IConfigurationNode, Advice<TExecution>> prepareAdvice,
        Func<TExecution, TControl?> selectControl, out TControl? control)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
        where TControl : class
    {
        var constructed = Construct(configuration, prepareFactory, prepareAdvice);
        control = selectControl(constructed.Raw);
        return constructed.Advised;
    }

    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecution? ResolveOptional<TExecution>(IConfigurationNode<TExecution>? configuration)
        where TExecution : class, IExecutionNode => configuration is null ? null : Resolve(configuration);

    public bool TryResolve<TExecution>(IConfigurationNode<TExecution> configuration,
        [NotNullWhen(true)] out TExecution? execution, [NotNullWhen(false)] out string? reason)
        where TExecution : class, IExecutionNode =>
        TryResolve(configuration, static source => source.CreateExecutionFactory(), UnavailableAdvice<TExecution>, out execution, out reason);

    /// <remarks>Invalid-operation failures become reasons; other exceptions propagate. Failed attempts may be retried.</remarks>
    public bool TryResolve<TConfiguration, TExecution>(TConfiguration configuration,
        Func<TConfiguration, ExecutionFactory<TExecution>> prepareFactory, Func<IConfigurationNode, Advice<TExecution>> prepareAdvice,
        [NotNullWhen(true)] out TExecution? execution, [NotNullWhen(false)] out string? reason)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        try
        {
            execution = Resolve(configuration, prepareFactory, prepareAdvice);
            reason = null;
            return true;
        }
        catch (InvalidOperationException exception)
        {
            execution = null;
            reason = exception.Message;
            return false;
        }
    }

    private static Advice<TExecution> UnavailableAdvice<TExecution>(IConfigurationNode wrapper)
        where TExecution : class, IExecutionNode =>
        throw new InvalidOperationException($"{wrapper.GetType().Name} is not a single-child wrapper producing {typeof(TExecution).Name}. Supply its typed wrapper preparation callback.");

    private (TExecution Raw, TExecution Advised) Construct<TConfiguration, TExecution>(TConfiguration configuration,
        Func<TConfiguration, ExecutionFactory<TExecution>> prepareFactory, Func<IConfigurationNode, Advice<TExecution>> prepareAdvice)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        if (!state.Constructing.Add(configuration))
            throw new InvalidOperationException($"Recursive construction of {configuration.GetType().Name}.");
        try
        {
            var (factory, owner) = state.GetOrAdd(configuration, prepareFactory);
            var raw = factory(new ResolutionScope(owner, aspects, advisingAspects));
            var advised = raw;
            foreach (var matching in advisingAspects.Where(matching => matching.Matches(configuration)))
                advised = owner.GetOrAddAdvice(matching, configuration, prepareAdvice)(advised);
            return (raw, advised);
        }
        finally { state.Constructing.Remove(configuration); }
    }

    private sealed class State(State? parent)
    {
        private readonly State? parent = parent;
        private readonly Dictionary<IConfigurationNode, Delegate> factories = new(ReferenceEqualityComparer.Instance);
        private readonly ConditionalWeakTable<Aspect, Dictionary<IConfigurationNode, Delegate>> preparedAdvice = new();
        internal HashSet<IConfigurationNode> Constructing { get; } = parent?.Constructing ?? new(ReferenceEqualityComparer.Instance);

        internal (ExecutionFactory<TExecution> Factory, State Owner) GetOrAdd<TConfiguration, TExecution>(
            TConfiguration configuration, Func<TConfiguration, ExecutionFactory<TExecution>> prepareFactory)
            where TConfiguration : class, IConfigurationNode
            where TExecution : class, IExecutionNode
        {
            for (var current = this; current is not null; current = current.parent)
                if (current.factories.TryGetValue(configuration, out var found))
                    return (found as ExecutionFactory<TExecution> ?? throw new InvalidOperationException(
                        $"{configuration.GetType().Name} was prepared for {found.GetType().GenericTypeArguments[0].Name}, not {typeof(TExecution).Name}. Resolve over a second search space or problem in its own scope."), current);
            var created = prepareFactory(configuration);
            factories.Add(configuration, created);
            return (created, this);
        }

        internal Advice<TExecution> GetOrAddAdvice<TConfiguration, TExecution>(Aspect matching,
            TConfiguration target, Func<IConfigurationNode, Advice<TExecution>> prepareAdvice)
            where TConfiguration : class, IConfigurationNode
            where TExecution : class, IExecutionNode
        {
            var prepared = preparedAdvice.GetValue(matching, static _ => new(ReferenceEqualityComparer.Instance));
            if (!prepared.TryGetValue(target, out var found))
            {
                found = matching.Prepare(target, prepareAdvice);
                prepared.Add(target, found);
            }
            return found as Advice<TExecution> ?? throw new InvalidOperationException($"Advice for {target.GetType().Name} is incompatible with {typeof(TExecution).Name}.");
        }
    }
}
