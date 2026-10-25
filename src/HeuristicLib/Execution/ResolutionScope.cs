using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Execution;

/// <remarks>
/// Configurations belong to the state that first resolved them and are reused from ancestors, together with their
/// scoped state. Each resolution builds fresh executors in the owner's state with the requesting aspects. A scope
/// applies its own aspects, then those of the scope it was created from, so every scope sees its ancestors' aspects.
/// The resolver provides no disposal or concurrency guarantees.
/// <para>
/// The typed scopes derive from this class only to name a run's types for resolution; outside the library it cannot be
/// derived from.
/// </para>
/// </remarks>
public partial class ResolutionScope
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

    /// <summary>Creates a scope that resolves exactly like <paramref name="scope"/>, for a typed scope over it.</summary>
    private protected ResolutionScope(ResolutionScope scope)
        : this(scope.state, scope.parent, scope.aspects)
    {
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

    /// <summary>Returns the scoped state of a configuration being built, creating it on the first request.</summary>
    /// <remarks>
    /// A configuration passes itself from its build, as in <c>scope.GetScopedState(this, create)</c>, and receives its
    /// own state, such as a counter or a cache. The state belongs to the scope that owns the configuration:
    /// every later build there receives the same object, across operations and run types, while a fresh child state
    /// starts with new state. A configuration has one state object, so every request asks for the same type. A
    /// configuration that is not being built in this scope has no scoped state to request.
    /// </remarks>
    public TState GetScopedState<TState>(IConfigurationNode configuration, Func<TState> create) where TState : class =>
        state.GetScopedState(configuration, create);

    /// <summary>Builds a configuration's executor and applies the matching advice for its role.</summary>
    /// <remarks>Matching advice must derive from <see cref="Advice{TConfiguration, TExecutor}"/> for this executor type.</remarks>
    public TExecutor Resolve<TExecutor>(IConfigurationNode<TExecutor> configuration)
        where TExecutor : class, IExecutor
    {
        if (!state.Constructing.Add(configuration))
            throw new InvalidOperationException($"Recursive construction of {configuration.GetType().Name}.");
        try
        {
            var owner = state.GetOrAddOwner(configuration);
            var executor = configuration.CreateExecutor(WithState(owner));
            foreach (var (pointcut, advice) in Advices())
                if (pointcut.Matches(configuration))
                    executor = advice.WrapNode(executor, configuration) as TExecutor
                        ?? throw new InvalidOperationException($"{advice.GetType().Name} cannot wrap {typeof(TExecutor).Name} of {configuration.GetType().Name}. " +
                            $"Advice for this role derives from Advice<TConfiguration, {typeof(TExecutor).Name}> for a TConfiguration the configuration implements.");
            return executor;
        }
        finally
        {
            state.Constructing.Remove(configuration);
        }
    }

    [return: NotNullIfNotNull(nameof(configuration))]
    public TExecutor? ResolveOptional<TExecutor>(IConfigurationNode<TExecutor>? configuration)
        where TExecutor : class, IExecutor => configuration is null ? null : Resolve(configuration);

    /// <remarks>Invalid-operation failures become reasons; other exceptions propagate. Failed attempts may be retried.</remarks>
    public bool TryResolve<TExecutor>(IConfigurationNode<TExecutor> configuration,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TExecutor : class, IExecutor =>
        Attempt(() => Resolve(configuration), out executor, out reason);

    // Runs a resolution and reports an invalid-operation failure as a reason; other exceptions propagate.
    private static bool Attempt<TExecutor>(Func<TExecutor> resolve,
        [NotNullWhen(true)] out TExecutor? executor, [NotNullWhen(false)] out string? reason)
        where TExecutor : class, IExecutor
    {
        try
        {
            executor = resolve();
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

    // Walks from this scope towards the root and yields each advice instance once, at its first position.
    // Once: aspects share advice on purpose, for example two traces reading one clock; that clock must still count each
    // call once, so applying it twice would wrap the executor twice around the same data. The same holds when one aspect
    // is added twice.
    // First position: a trace lists its clock advice before its observations, so the clock wraps closer to the source
    // and has counted a call when the trace records it. Keeping a later occurrence, such as the same clock added again
    // after the trace, would move it outside the trace's observations and make the trace record one call behind.
    private IEnumerable<(Pointcut Pointcut, Advice Advice)> Advices()
    {
        var applied = new HashSet<Advice>(ReferenceEqualityComparer.Instance);
        for (var scope = this; scope is not null; scope = scope.parent)
            foreach (var aspect in scope.aspects)
                foreach (var pair in aspect)
                    if (applied.Add(pair.Advice))
                        yield return pair;
    }

    internal sealed class State(State? parent)
    {
        private readonly State? parent = parent;
        private readonly Dictionary<IConfigurationNode, ConfigurationResolutionEntry> owners = new(ReferenceEqualityComparer.Instance);
        internal HashSet<IConfigurationNode> Constructing { get; } = parent?.Constructing ?? new(ReferenceEqualityComparer.Instance);

        // Finds the state that owns the configuration, or makes this state its owner.
        internal State GetOrAddOwner(IConfigurationNode configuration)
        {
            if (FindOwner(configuration) is { Owner: var owner })
                return owner;
            owners.Add(configuration, new ConfigurationResolutionEntry());
            return this;
        }

        private (ConfigurationResolutionEntry Entry, State Owner)? FindOwner(IConfigurationNode configuration)
        {
            for (var current = this; current is not null; current = current.parent)
                if (current.owners.TryGetValue(configuration, out var found))
                    return (found, current);
            return null;
        }

        // Only a configuration under construction reaches its entry, which its owner holds.
        internal TState GetScopedState<TState>(IConfigurationNode configuration, Func<TState> create) where TState : class
        {
            if (!Constructing.Contains(configuration) || FindOwner(configuration) is not { Entry: var entry })
                throw new InvalidOperationException(
                    $"{configuration.GetType().Name} is not being built in this scope. Request scoped state from its own build, passing the configuration itself.");
            entry.ScopedState ??= create() ?? throw new InvalidOperationException($"The scoped state of {configuration.GetType().Name} must not be null.");
            return entry.ScopedState as TState ?? throw new InvalidOperationException(
                $"{configuration.GetType().Name} already has scoped state of type {entry.ScopedState.GetType().Name}, not {typeof(TState).Name}. A configuration has one state object.");
        }

        private sealed class ConfigurationResolutionEntry
        {
            // Created on the configuration's first request and shared by every later build in the owning scope.
            internal object? ScopedState { get; set; }
        }
    }
}
