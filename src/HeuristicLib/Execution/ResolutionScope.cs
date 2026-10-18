using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Execution;

/// <remarks>
/// Prepared factories belong to configuration references in a state and are reused from ancestors. Each resolution
/// constructs fresh nodes using the factory owner's state and the requesting aspects. A leaf may return its prepared node.
/// A scope applies its own aspects, then those of the scope it was created from, so every scope sees its ancestors'
/// aspects. The resolver provides no disposal or concurrency guarantees.
/// </remarks>
public sealed partial class ResolutionScope
{
    private readonly State state;
    private readonly ResolutionScope? parent;
    private readonly ImmutableArray<Aspect> aspects;

    private ResolutionScope(State state, ResolutionScope? parent, ImmutableArray<Aspect> aspects)
    {
        this.state = state;
        this.parent = parent;
        this.aspects = aspects;
    }

    /// <remarks>Aspects apply in the supplied order. An empty collection creates an unadvised scope.</remarks>
    public static ResolutionScope Create(params IReadOnlyList<Aspect> aspects) =>
        new(new State(null), null, [.. aspects]);

    /// <remarks>Creates child-local state. The supplied aspects apply before this scope's aspects.</remarks>
    public ResolutionScope CreateChildScope(params IReadOnlyList<Aspect> aspects) =>
        new(new State(state), this, [.. aspects]);

    /// <remarks>Shares this scope's state. The supplied aspects apply before this scope's aspects.</remarks>
    public ResolutionScope With(params IReadOnlyList<Aspect> aspects) =>
        new(state, this, [.. aspects]);

    /// <summary>Creates child-local state that a meta-algorithm keeps across activations.</summary>
    /// <remarks>Each activation applies its own scope's aspects to this state through <see cref="WithState"/>.</remarks>
    internal State CreateChildState() => new(state);

    /// <summary>Applies this scope's aspects over another state.</summary>
    internal ResolutionScope WithState(State other) => new(other, parent, aspects);

    public TExecutor Resolve<TConfiguration, TExecutor>(TConfiguration configuration,
        Func<TConfiguration, ExecutorFactory<TExecutor>> createExecutorFactory, Func<Advice, TConfiguration, WrapperExecutorFactory<TExecutor>> createWrapperFactory)
        where TConfiguration : class, IConfigurationNode
        where TExecutor : class, IExecutor => Construct(configuration, createExecutorFactory, createWrapperFactory).Advised;

    public TExecutor Resolve<TExecutor>(IConfigurationNode<TExecutor> configuration)
        where TExecutor : class, IExecutor =>
        Resolve(configuration, static source => source.CreateExecutorFactory(), UnavailableWrapperFactory<TExecutor>);

    /// <remarks>
    /// Controls project from the raw execution only after the advised chain succeeds. Invoke operations through the
    /// returned chain. Configured wrappers expose their own controls; projection does not search their children.
    /// </remarks>
    public TExecutor Resolve<TConfiguration, TExecutor, TControl>(TConfiguration configuration,
        Func<TConfiguration, ExecutorFactory<TExecutor>> createExecutorFactory, Func<Advice, TConfiguration, WrapperExecutorFactory<TExecutor>> createWrapperFactory,
        Func<TExecutor, TControl?> selectControl, out TControl? control)
        where TConfiguration : class, IConfigurationNode
        where TExecutor : class, IExecutor
        where TControl : class
    {
        var constructed = Construct(configuration, createExecutorFactory, createWrapperFactory);
        control = selectControl(constructed.Raw);
        return constructed.Advised;
    }

    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecutor? ResolveOptional<TExecutor>(IConfigurationNode<TExecutor>? configuration)
        where TExecutor : class, IExecutor => configuration is null ? null : Resolve(configuration);

    public bool TryResolve<TExecutor>(IConfigurationNode<TExecutor> configuration,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TExecutor : class, IExecutor =>
        TryResolve(configuration, static source => source.CreateExecutorFactory(), UnavailableWrapperFactory<TExecutor>, out executor, out reason);

    /// <remarks>Invalid-operation failures become reasons; other exceptions propagate. Failed attempts may be retried.</remarks>
    public bool TryResolve<TConfiguration, TExecutor>(TConfiguration configuration,
        Func<TConfiguration, ExecutorFactory<TExecutor>> createExecutorFactory, Func<Advice, TConfiguration, WrapperExecutorFactory<TExecutor>> createWrapperFactory,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TConfiguration : class, IConfigurationNode
        where TExecutor : class, IExecutor
    {
        try
        {
            executor = Resolve(configuration, createExecutorFactory, createWrapperFactory);
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

    private static WrapperExecutorFactory<TExecutor> UnavailableWrapperFactory<TExecutor>(Advice advice, IConfigurationNode<TExecutor> configuration)
        where TExecutor : class, IExecutor =>
        throw new InvalidOperationException($"{advice.GetType().Name} cannot wrap {typeof(TExecutor).Name}. Resolve through the role's typed resolution extension.");

    // Walks from this scope towards the root and yields each advice instance once, at its first position.
    // Once: an advice instance is the key for its prepared state, so applying it twice would wrap the execution twice
    // over the same state. Aspects share advice on purpose, for example two traces reading one clock; that clock must
    // still count each call once. The same holds when one aspect is added twice.
    // First position: a trace lists its clock advice before its observations, so the clock wraps closer to the source
    // and has counted a call when the trace records it. Keeping a later occurrence, such as the same clock added again
    // after the trace, would move it outside the trace's observations and make the trace record one call behind.
    private IEnumerable<(Pointcut Pointcut, Advice Advice)> Advices()
    {
        var applied = new HashSet<Advice>(ReferenceEqualityComparer.Instance);
        for (var scope = this; scope is not null; scope = scope.parent)
            foreach (var aspect in scope.aspects)
                foreach (var pair in aspect.Advices)
                    if (applied.Add(pair.Advice))
                        yield return pair;
    }

    private (TExecutor Raw, TExecutor Advised) Construct<TConfiguration, TExecutor>(TConfiguration configuration,
        Func<TConfiguration, ExecutorFactory<TExecutor>> createExecutorFactory, Func<Advice, TConfiguration, WrapperExecutorFactory<TExecutor>> createWrapperFactory)
        where TConfiguration : class, IConfigurationNode
        where TExecutor : class, IExecutor
    {
        if (!state.Constructing.Add(configuration))
            throw new InvalidOperationException($"Recursive construction of {configuration.GetType().Name}.");
        try
        {
            var (factory, owner) = state.GetOrAdd(configuration, createExecutorFactory);
            TExecutor raw = factory(WithState(owner));
            TExecutor advised = raw;
            foreach (var (pointcut, advice) in Advices())
                if (pointcut.Matches(configuration))
                    advised = owner.GetOrAddWrapperFactory(advice, configuration, createWrapperFactory)(advised);
            return (raw, advised);
        }
        finally
        {
            state.Constructing.Remove(configuration);
        }
    }

    internal sealed partial class State(State? parent)
    {
        private readonly State? parent = parent;
        private readonly Dictionary<IConfigurationNode, ConfigurationResolutionEntry> owners = new(ReferenceEqualityComparer.Instance);
        private readonly ConditionalWeakTable<Advice, Dictionary<IConfigurationNode, Delegate>> wrapperFactories = new();
        internal HashSet<IConfigurationNode> Constructing { get; } = parent?.Constructing ?? new(ReferenceEqualityComparer.Instance);

        internal (ExecutorFactory<TExecutor> Factory, State Owner) GetOrAdd<TConfiguration, TExecutor>(
            TConfiguration configuration, Func<TConfiguration, ExecutorFactory<TExecutor>> createExecutorFactory)
            where TConfiguration : class, IConfigurationNode
            where TExecutor : class, IExecutor
        {
            var selected = FindOwner(configuration);
            if (selected is { } existing)
            {
                var found = existing.Entry.LegacyFactory ?? throw PreparationPathMismatch(configuration);
                return (found as ExecutorFactory<TExecutor> ?? throw new InvalidOperationException(
                    $"{configuration.GetType().Name} was prepared for {found.GetType().GenericTypeArguments[0].Name}, not {typeof(TExecutor).Name}. Resolve over a second search space or problem in its own scope."), existing.Owner);
            }
            var created = createExecutorFactory(configuration);
            owners.Add(configuration, new ConfigurationResolutionEntry(created));
            return (created, this);
        }

        private (ConfigurationResolutionEntry Entry, State Owner)? FindOwner(IConfigurationNode configuration)
        {
            for (var current = this; current is not null; current = current.parent)
                if (current.owners.TryGetValue(configuration, out var found))
                    return (found, current);
            return null;
        }

        private static InvalidOperationException PreparationPathMismatch(IConfigurationNode configuration) =>
            new($"{configuration.GetType().Name} already has a prepared owner through another preparation path. Resolve through its declared operator contract.");

        private sealed class ConfigurationResolutionEntry
        {
            internal ConfigurationResolutionEntry(Delegate factory) { LegacyFactory = factory; }
            internal ConfigurationResolutionEntry(OperatorExecutorFactory prepared) { Operator = prepared; }

            internal Delegate? LegacyFactory { get; }
            internal OperatorExecutorFactory? Operator { get; }
        }

        /// <remarks>Called once per advice, join point and owner state; the result wraps each construction.</remarks>
        internal WrapperExecutorFactory<TExecutor> GetOrAddWrapperFactory<TConfiguration, TExecutor>(Advice advice,
            TConfiguration target, Func<Advice, TConfiguration, WrapperExecutorFactory<TExecutor>> createWrapperFactory)
            where TConfiguration : class, IConfigurationNode
            where TExecutor : class, IExecutor
        {
            var prepared = wrapperFactories.GetValue(advice, static _ => new(ReferenceEqualityComparer.Instance));
            if (!prepared.TryGetValue(target, out var found))
            {
                found = createWrapperFactory(advice, target);
                prepared.Add(target, found);
            }
            return found as WrapperExecutorFactory<TExecutor> ?? throw new InvalidOperationException($"Advice for {target.GetType().Name} is incompatible with {typeof(TExecutor).Name}.");
        }
    }
}
