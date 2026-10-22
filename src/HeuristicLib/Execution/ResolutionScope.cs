using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HEAL.HeuristicLib.Execution;

/// <remarks>
/// Prepared factories belong to configuration references in a state and are reused from ancestors. Each resolution
/// constructs fresh nodes using the factory owner's state and the requesting aspects. A leaf may return its prepared node.
/// A scope applies its own aspects, then those of the scope it was created from, so every scope sees its ancestors'
/// aspects. The resolver provides no disposal or concurrency guarantees.
/// </remarks>
public sealed class ResolutionScope
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

    /// <summary>Creates a peer scope with independent copies of explicitly forkable local prepared state.</summary>
    /// <remarks>
    /// Ancestor state remains shared. Other local factories and advice factories are prepared afresh when needed.
    /// All copies are installed before any child is resolved. Fork only between invocations; concurrent mutation
    /// and forking are not supported. The resulting scope inherits this scope's observation context.
    /// </remarks>
    public ResolutionScope CreateForkedScope() => new(state.Fork(), parent, aspects);

    /// <summary>Retains this scope's prepared state while using the supplied scope's observation context.</summary>
    /// <remarks>Existing executions keep their original children and advice. Resolve again to obtain new bindings.</remarks>
    public ResolutionScope WithContext(ResolutionScope context) => context.WithState(state);

    /// <summary>Creates child-local state that a meta-algorithm keeps across activations.</summary>
    /// <remarks>Each activation applies its own scope's aspects to this state through <see cref="WithState"/>.</remarks>
    internal State CreateChildState() => new(state);

    /// <summary>Applies this scope's aspects over another state.</summary>
    internal ResolutionScope WithState(State other) => new(other, parent, aspects);

    public TExecution Resolve<TConfiguration, TExecution>(TConfiguration configuration,
        Func<TConfiguration, ExecutionFactory<TExecution>> createExecutionFactory, Func<Advice, TConfiguration, WrapperExecutionFactory<TExecution>> createWrapperFactory)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode => Construct(configuration, createExecutionFactory, createWrapperFactory).Advised;

    public TExecution Resolve<TExecution>(IConfigurationNode<TExecution> configuration)
        where TExecution : class, IExecutionNode =>
        Resolve(configuration, static source => source.CreateExecutionFactory(), UnavailableWrapperFactory<TExecution>);

    /// <remarks>
    /// Controls project from the raw execution only after the advised chain succeeds. Invoke operations through the
    /// returned chain. Configured wrappers expose their own controls; projection does not search their children.
    /// </remarks>
    public TExecution Resolve<TConfiguration, TExecution, TControl>(TConfiguration configuration,
        Func<TConfiguration, ExecutionFactory<TExecution>> createExecutionFactory, Func<Advice, TConfiguration, WrapperExecutionFactory<TExecution>> createWrapperFactory,
        Func<TExecution, TControl?> selectControl, out TControl? control)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
        where TControl : class
    {
        var constructed = Construct(configuration, createExecutionFactory, createWrapperFactory);
        control = selectControl(constructed.Raw);
        return constructed.Advised;
    }

    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecution? ResolveOptional<TExecution>(IConfigurationNode<TExecution>? configuration)
        where TExecution : class, IExecutionNode => configuration is null ? null : Resolve(configuration);

    public bool TryResolve<TExecution>(IConfigurationNode<TExecution> configuration,
        [NotNullWhen(true)] out TExecution? execution, [NotNullWhen(false)] out string? reason)
        where TExecution : class, IExecutionNode =>
        TryResolve(configuration, static source => source.CreateExecutionFactory(), UnavailableWrapperFactory<TExecution>, out execution, out reason);

    /// <remarks>Invalid-operation failures become reasons; other exceptions propagate. Failed attempts may be retried.</remarks>
    public bool TryResolve<TConfiguration, TExecution>(TConfiguration configuration,
        Func<TConfiguration, ExecutionFactory<TExecution>> createExecutionFactory, Func<Advice, TConfiguration, WrapperExecutionFactory<TExecution>> createWrapperFactory,
        [NotNullWhen(true)] out TExecution? execution, [NotNullWhen(false)] out string? reason)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        try
        {
            execution = Resolve(configuration, createExecutionFactory, createWrapperFactory);
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

    private static WrapperExecutionFactory<TExecution> UnavailableWrapperFactory<TExecution>(Advice advice, IConfigurationNode<TExecution> configuration)
        where TExecution : class, IExecutionNode =>
        throw new InvalidOperationException($"{advice.GetType().Name} cannot wrap {typeof(TExecution).Name}. Resolve through the role's typed resolution extension.");

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

    private (TExecution Raw, TExecution Advised) Construct<TConfiguration, TExecution>(TConfiguration configuration,
        Func<TConfiguration, ExecutionFactory<TExecution>> createExecutionFactory, Func<Advice, TConfiguration, WrapperExecutionFactory<TExecution>> createWrapperFactory)
        where TConfiguration : class, IConfigurationNode
        where TExecution : class, IExecutionNode
    {
        if (!state.Constructing.Add(configuration))
            throw new InvalidOperationException($"Recursive construction of {configuration.GetType().Name}.");
        try
        {
            var (factory, owner) = state.GetOrAdd(configuration, createExecutionFactory);
            var raw = factory(WithState(owner));
            var advised = raw;
            foreach (var (pointcut, advice) in Advices())
                if (pointcut.Matches(configuration))
                    advised = owner.GetOrAddWrapperFactory(advice, configuration, createWrapperFactory)(advised);
            return (raw, advised);
        }
        finally { state.Constructing.Remove(configuration); }
    }

    internal sealed class State(State? parent)
    {
        private readonly State? parent = parent;
        private readonly Dictionary<IConfigurationNode, Delegate> factories = new(ReferenceEqualityComparer.Instance);
        private readonly ConditionalWeakTable<Advice, Dictionary<IConfigurationNode, Delegate>> wrapperFactories = new();
        internal HashSet<IConfigurationNode> Constructing { get; } = parent?.Constructing ?? new(ReferenceEqualityComparer.Instance);

        internal State Fork()
        {
            if (Constructing.Count != 0)
                throw new InvalidOperationException("Cannot fork state during execution graph construction.");

            var fork = new State(parent);
            var copied = new Dictionary<Delegate, Delegate>(ReferenceEqualityComparer.Instance);
            foreach (var (configuration, factory) in factories)
            {
                if (factory.Target is not IForkableFactoryPreparation preparation)
                    continue;
                if (!copied.TryGetValue(factory, out var copy))
                {
                    copy = preparation.Fork();
                    copied.Add(factory, copy);
                }
                fork.factories.Add(configuration, copy);
            }
            return fork;
        }

        internal (ExecutionFactory<TExecution> Factory, State Owner) GetOrAdd<TConfiguration, TExecution>(
            TConfiguration configuration, Func<TConfiguration, ExecutionFactory<TExecution>> createExecutionFactory)
            where TConfiguration : class, IConfigurationNode
            where TExecution : class, IExecutionNode
        {
            for (var current = this; current is not null; current = current.parent)
                if (current.factories.TryGetValue(configuration, out var found))
                    return (found as ExecutionFactory<TExecution> ?? throw new InvalidOperationException(
                        $"{configuration.GetType().Name} was prepared for {found.GetType().GenericTypeArguments[0].Name}, not {typeof(TExecution).Name}. Resolve over a second search space or problem in its own scope."), current);
            var created = createExecutionFactory(configuration);
            factories.Add(configuration, created);
            return (created, this);
        }

        /// <remarks>Called once per advice, join point and owner state; the result wraps each construction.</remarks>
        internal WrapperExecutionFactory<TExecution> GetOrAddWrapperFactory<TConfiguration, TExecution>(Advice advice,
            TConfiguration target, Func<Advice, TConfiguration, WrapperExecutionFactory<TExecution>> createWrapperFactory)
            where TConfiguration : class, IConfigurationNode
            where TExecution : class, IExecutionNode
        {
            var prepared = wrapperFactories.GetValue(advice, static _ => new(ReferenceEqualityComparer.Instance));
            if (!prepared.TryGetValue(target, out var found))
            {
                found = createWrapperFactory(advice, target);
                prepared.Add(target, found);
            }
            return found as WrapperExecutionFactory<TExecution> ?? throw new InvalidOperationException($"Advice for {target.GetType().Name} is incompatible with {typeof(TExecution).Name}.");
        }
    }
}
