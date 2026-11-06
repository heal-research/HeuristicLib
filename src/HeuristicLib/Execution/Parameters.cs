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

    // Returns the owner with this parameter's slots passed through the rewriter, or the same instance when none changed.
    internal IConfigurationNode Rewrite(IConfigurationNode owner, ISlotRewriter rewriter);
}

// What a walk supplies for each slot it visits.
internal interface ISlotRewriter
{
    // Returns the node that should occupy a slot of the declared type, which holds child.
    IConfigurationNode Slot(IConfigurationNode owner, string name, Type slotType, IConfigurationNode child);

    // Returns the node a reference list entry should name now.
    IConfigurationNode Reference(IConfigurationNode entry);

    // Records a delegate the walk cannot look into.
    void Boundary(IConfigurationNode owner, string name);
}

/// <summary>One value of the configuration type <typeparamref name="TOwner"/>, whose declared type says what a walk does with it.</summary>
/// <remarks>
/// A value typed by a configuration, such as a mutator, is a place in the graph, and a null value is an empty place. A
/// delegate is reported, because a walk cannot look into what it creates. Everything else is a setting. A list of
/// configurations is a <see cref="ListParameter{TOwner, TChild}"/> or a <see cref="ReferenceListParameter{TOwner}"/>.
/// </remarks>
public sealed class Parameter<TOwner, TValue> : IParameter
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
            throw new ArgumentException($"{name} holds a list of configurations, so declare it with ListParameter, or with ReferenceListParameter when it names configurations that live elsewhere.", nameof(name));
        Name = name;
        this.get = get;
        this.with = with;
    }

    public string Name { get; }

    public Type ValueType => typeof(TValue);

    public TValue Get(TOwner owner) => get(owner);

    public TOwner With(TOwner owner, TValue value) => with(owner, value);

    public object? GetValue(IConfigurationNode owner) => get((TOwner)owner);

    public IReadOnlyList<IConfigurationNode> GetChildren(IConfigurationNode owner) =>
        holdsConfiguration && get((TOwner)owner) is IConfigurationNode child ? [child] : [];

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

/// <summary>An ordered list of configurations, declared on the configuration type <typeparamref name="TOwner"/>.</summary>
public sealed class ListParameter<TOwner, TChild>(string name, Func<TOwner, IReadOnlyList<TChild>> get, Func<TOwner, IReadOnlyList<TChild>, TOwner> with) : IParameter
    where TOwner : class, IConfigurationNode
    where TChild : class, IConfigurationNode
{
    public string Name { get; } = name;

    public Type ValueType => typeof(TChild);

    public IReadOnlyList<TChild> Get(TOwner owner) => get(owner);

    public TOwner With(TOwner owner, IReadOnlyList<TChild> value) => with(owner, value);

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
public sealed class ReferenceListParameter<TOwner>(string name, Func<TOwner, IReadOnlyList<IConfigurationNode>> get, Func<TOwner, IReadOnlyList<IConfigurationNode>, TOwner> with) : IParameter
    where TOwner : class, IConfigurationNode
{
    public string Name { get; } = name;

    public Type ValueType => typeof(IConfigurationNode);

    public IReadOnlyList<IConfigurationNode> Get(TOwner owner) => get(owner);

    public TOwner With(TOwner owner, IReadOnlyList<IConfigurationNode> value) => with(owner, value);

    public object? GetValue(IConfigurationNode owner) => get((TOwner)owner);

    public IReadOnlyList<IConfigurationNode> GetChildren(IConfigurationNode owner) => [];

    IConfigurationNode IParameter.Rewrite(IConfigurationNode owner, ISlotRewriter rewriter)
    {
        var typed = (TOwner)owner;
        var entries = get(typed);
        IConfigurationNode[]? rewritten = null;
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = rewriter.Reference(entries[index]);
            if (ReferenceEquals(entry, entries[index]))
                continue;
            rewritten ??= [.. entries];
            rewritten[index] = entry;
        }
        return rewritten is null ? owner : with(typed, rewritten);
    }
}
