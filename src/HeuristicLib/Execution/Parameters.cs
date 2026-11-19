namespace HEAL.HeuristicLib.Execution;

/// <summary>One declared parameter of a configuration type: its name and how to read and vary it.</summary>
/// <remarks>
/// A parameter holds metadata per type and no instance data; the configuration's properties stay the source of values.
/// Declare one static member per parameter, list them in a static <c>Parameters</c> collection and override
/// <see cref="IConfigurationNode.GetParameters"/> to add the inherited ones.
/// </remarks>
public interface IParameter
{
    string Name { get; }

    /// <summary>The declared type of the slot, or of one element for a list.</summary>
    Type ValueType { get; }

    /// <summary>Reads this parameter's current value from a configuration of the declaring type.</summary>
    object? GetValue(IConfigurationNode owner);

    /// <summary>The configurations this parameter holds as places in the graph, which a setting, a delegate and a reference list do not.</summary>
    IReadOnlyList<IConfigurationNode> GetChildren(IConfigurationNode owner);

    /// <summary>The execution this parameter's children run under, given the execution of the owner.</summary>
    ExecutionSignature ChildExecution(ExecutionSignature execution) => execution;

    // Returns the owner with this parameter's slots passed through the rewriter, or the same instance when none changed.
    internal IConfigurationNode Rewrite(IConfigurationNode owner, ISlotRewriter rewriter);

    internal bool AcceptsValue<T>(T value);

    internal TNode WithValue<TNode, T>(TNode owner, T value) where TNode : class, IConfigurationNode;
}

/// <summary>Typed access to a declared configuration parameter.</summary>
public interface IParameter<TOwner, TValue> : IParameter
    where TOwner : class, IConfigurationNode
{
    /// <summary>Reads this parameter's value from the supplied configuration.</summary>
    TValue Get(TOwner owner);

    /// <summary>Returns the supplied configuration with this parameter set to the given value.</summary>
    TOwner With(TOwner owner, TValue value);

    bool IParameter.AcceptsValue<T>(T value) => value is TValue || (value is null && default(TValue) is null);

    TNode IParameter.WithValue<TNode, T>(TNode owner, T value)
    {
        if (!AcceptsValue(value))
            throw new ArgumentException($"Value for parameter '{Name}' must match its declared value type.", nameof(value));

        var replacement = value is TValue typedValue ? typedValue : default!;
        return owner is TOwner typedOwner && With(typedOwner, replacement) is TNode configured
            ? configured
            : throw new InvalidOperationException($"Parameter '{Name}' must apply to and preserve the configuration type {typeof(TNode).Name}.");
    }
}

// What a walk supplies for each slot it visits.
internal interface ISlotRewriter
{
    // Returns the node that should occupy a slot of the declared type, which holds child.
    IConfigurationNode Slot(IConfigurationNode owner, string name, Type slotType, IConfigurationNode child);

    // Returns the node a reference list entry should name now.
    IConfigurationNode Reference(string name, IConfigurationNode entry);

    // Records a delegate the walk cannot look into.
    void Boundary(IConfigurationNode owner, string name);
}

/// <summary>One value of the configuration type <typeparamref name="TOwner"/>, whose declared type says what a walk does with it.</summary>
/// <remarks>
/// A value typed by a configuration, such as a mutator, is a place in the graph, and a null value is an empty place. A
/// delegate is reported, because a walk cannot look into what it creates. Everything else is a setting. A list of
/// configurations is a <see cref="ListParameter{TOwner, TChild}"/> or a <see cref="ReferenceListParameter{TOwner}"/>.
/// <para>
/// This is decided on the closed type. A value typed by a type parameter, such as a candidate, can close over a
/// configuration and is still data, so declare it with a <see cref="SettingParameter{TOwner, TValue}"/>.
/// </para>
/// </remarks>
public sealed class Parameter<TOwner, TValue> : IParameter<TOwner, TValue>
    where TOwner : class, IConfigurationNode
{
    private readonly Func<TOwner, TValue> get;
    private readonly Func<TOwner, TValue, TOwner> with;
    private readonly bool holdsConfiguration = typeof(TValue).IsAssignableTo(typeof(IConfigurationNode));
    private readonly bool holdsDelegate = typeof(TValue).IsAssignableTo(typeof(Delegate));

    public Parameter(string name, Func<TOwner, TValue> get, Func<TOwner, TValue, TOwner> with)
    {
        // A list of configurations would otherwise pass as a setting, and a walk would silently skip what it holds.
        if (typeof(TValue).IsAssignableTo(typeof(IReadOnlyList<IConfigurationNode>)))
            throw new ArgumentException($"{name} holds a list of configurations, so declare it with ListParameter, with ReferenceListParameter when it names configurations that live elsewhere, or with SettingParameter when they are data, such as candidates.", nameof(name));
        Name = name;
        this.get = get;
        this.with = with;
    }

    public string Name { get; }

    public Type ValueType => typeof(TValue);

    /// <summary>
    /// Maps the owner's execution to the one a child in this slot runs under, for a composite that resolves the child over
    /// other types. Leave it out for a child that runs under the owner's execution, which is the usual case.
    /// </summary>
    /// <remarks>State the same types the owner passes when it resolves the child.</remarks>
    public Func<ExecutionSignature, ExecutionSignature>? ChildExecution { get; init; }

    public TValue Get(TOwner owner) => get(owner);

    public TOwner With(TOwner owner, TValue value) => with(owner, value);

    public object? GetValue(IConfigurationNode owner) => get((TOwner)owner);

    public IReadOnlyList<IConfigurationNode> GetChildren(IConfigurationNode owner) =>
        holdsConfiguration && get((TOwner)owner) is IConfigurationNode child ? [child] : [];

    ExecutionSignature IParameter.ChildExecution(ExecutionSignature execution) => ChildExecution is { } forward ? forward(execution) : execution;

    IConfigurationNode IParameter.Rewrite(IConfigurationNode owner, ISlotRewriter rewriter)
    {
        if (holdsDelegate)
            rewriter.Boundary(owner, Name);
        if (!holdsConfiguration)
            return owner;
        var typed = (TOwner)owner;
        if (get(typed) is not IConfigurationNode child)
            return owner;
        var slot = rewriter.Slot(owner, Name, typeof(TValue), child);
        return ReferenceEquals(slot, child) ? owner : with(typed, (TValue)(object)slot);
    }
}

/// <summary>One value of the configuration type <typeparamref name="TOwner"/> that is data whatever its type is.</summary>
/// <remarks>
/// A walk neither enters it nor reports it, even when <typeparamref name="TValue"/> closes over a configuration or a
/// delegate. Use it for a value typed by a type parameter, such as a candidate. A value of a concrete setting type needs
/// only a <see cref="Parameter{TOwner, TValue}"/>, and a delegate that creates configurations should stay one, so that a
/// substitution can report it.
/// </remarks>
public sealed class SettingParameter<TOwner, TValue>(string name, Func<TOwner, TValue> get, Func<TOwner, TValue, TOwner> with) : IParameter<TOwner, TValue>
    where TOwner : class, IConfigurationNode
{
    public string Name { get; } = name;

    public Type ValueType => typeof(TValue);

    public TValue Get(TOwner owner) => get(owner);

    public TOwner With(TOwner owner, TValue value) => with(owner, value);

    public object? GetValue(IConfigurationNode owner) => get((TOwner)owner);

    public IReadOnlyList<IConfigurationNode> GetChildren(IConfigurationNode owner) => [];

    IConfigurationNode IParameter.Rewrite(IConfigurationNode owner, ISlotRewriter rewriter) => owner;
}

/// <summary>An ordered list of configurations, declared on the configuration type <typeparamref name="TOwner"/>.</summary>
public sealed class ListParameter<TOwner, TChild>(string name, Func<TOwner, IReadOnlyList<TChild>> get, Func<TOwner, IReadOnlyList<TChild>, TOwner> with) : IParameter<TOwner, ValueArray<TChild>>
    where TOwner : class, IConfigurationNode
    where TChild : class, IConfigurationNode
{
    public string Name { get; } = name;

    public Type ValueType => typeof(TChild);

    public IReadOnlyList<TChild> Get(TOwner owner) => get(owner);

    public TOwner With(TOwner owner, IReadOnlyList<TChild> value) => with(owner, value);

    ValueArray<TChild> IParameter<TOwner, ValueArray<TChild>>.Get(TOwner owner) => Get(owner).ToValueArray();

    TOwner IParameter<TOwner, ValueArray<TChild>>.With(TOwner owner, ValueArray<TChild> value) => With(owner, value);

    public object? GetValue(IConfigurationNode owner) => get((TOwner)owner);

    public IReadOnlyList<IConfigurationNode> GetChildren(IConfigurationNode owner) => get((TOwner)owner);

    IConfigurationNode IParameter.Rewrite(IConfigurationNode owner, ISlotRewriter rewriter)
    {
        var typed = (TOwner)owner;
        var children = get(typed);
        TChild[]? rewritten = null;
        for (var index = 0; index < children.Count; index++)
        {
            var slot = rewriter.Slot(owner, $"{Name}[{index}]", typeof(TChild), children[index]);
            if (ReferenceEquals(slot, children[index]))
                continue;
            rewritten ??= [.. children];
            rewritten[index] = (TChild)slot;
        }
        return rewritten is null ? owner : with(typed, rewritten);
    }
}

/// <summary>A list naming configurations that live elsewhere in the graph, declared on the configuration type <typeparamref name="TOwner"/>.</summary>
/// <remarks>
/// An entry is not a place in the graph. A substitution keeps it naming the configuration it named while the
/// replacement still contains that configuration, and otherwise lets it follow the replacement.
/// </remarks>
public sealed class ReferenceListParameter<TOwner>(string name, Func<TOwner, IReadOnlyList<IConfigurationNode>> get, Func<TOwner, IReadOnlyList<IConfigurationNode>, TOwner> with) : IParameter<TOwner, ValueArray<IConfigurationNode>>
    where TOwner : class, IConfigurationNode
{
    public string Name { get; } = name;

    public Type ValueType => typeof(IConfigurationNode);

    public IReadOnlyList<IConfigurationNode> Get(TOwner owner) => get(owner);

    public TOwner With(TOwner owner, IReadOnlyList<IConfigurationNode> value) => with(owner, value);

    ValueArray<IConfigurationNode> IParameter<TOwner, ValueArray<IConfigurationNode>>.Get(TOwner owner) => Get(owner).ToValueArray();

    TOwner IParameter<TOwner, ValueArray<IConfigurationNode>>.With(TOwner owner, ValueArray<IConfigurationNode> value) => With(owner, value);

    public object? GetValue(IConfigurationNode owner) => get((TOwner)owner);

    public IReadOnlyList<IConfigurationNode> GetChildren(IConfigurationNode owner) => [];

    IConfigurationNode IParameter.Rewrite(IConfigurationNode owner, ISlotRewriter rewriter)
    {
        var typed = (TOwner)owner;
        var entries = get(typed);
        IConfigurationNode[]? rewritten = null;
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = rewriter.Reference($"{Name}[{index}]", entries[index]);
            if (ReferenceEquals(entry, entries[index]))
                continue;
            rewritten ??= [.. entries];
            rewritten[index] = entry;
        }
        return rewritten is null ? owner : with(typed, rewritten);
    }
}
