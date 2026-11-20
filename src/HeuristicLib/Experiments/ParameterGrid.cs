using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;

namespace HEAL.HeuristicLib.Experiments;

/// <summary>An algorithm prototype and its ordered dimensions. A grid with no dimensions has the prototype as its only parameter candidate.</summary>
public sealed record ParameterGrid<TCandidate, TAlgorithm, TSearchState>
    where TAlgorithm : class, IAlgorithm<TCandidate, TSearchState>
    where TSearchState : class, ISearchState
{
    private ValueArray<ParameterGridDimension<TAlgorithm>> StoredDimensions { get; init; }

    /// <summary>The configuration to which the grid applies each combination of dimension values.</summary>
    public TAlgorithm Prototype { get; }

    /// <summary>Lists the parameter and values for each dimension in declaration order. Lambda dimensions report a null parameter, and a parameter varied twice appears once per dimension.</summary>
    public ValueArray<(IParameter? Parameter, ValueArray<object?> Values)> Dimensions =>
        StoredDimensions.Select(dimension => (dimension.Parameter, Values: dimension.BoxedValues)).ToValueArray();

    internal ParameterGrid(TAlgorithm prototype)
    {
        Prototype = prototype;
    }

    /// <summary>Adds a dimension using a typed configurator. This dimension contributes no settings label.</summary>
    /// <exception cref="ArgumentException">The value list is empty.</exception>
    public ParameterGrid<TCandidate, TAlgorithm, TSearchState> VaryBy<TValue>(
        IReadOnlyList<TValue> values, Func<TAlgorithm, TValue, TAlgorithm> configurator)
    {
        var snapshot = SnapshotValues(values, "the lambda dimension");
        return AddDimension(new ConfiguratorDimension<TAlgorithm, TValue>(snapshot, configurator));
    }

    /// <summary>Adds a labelled dimension for a top-level parameter declared by the prototype, including inherited parameters.</summary>
    /// <exception cref="ArgumentException">The name does not identify exactly one parameter, or the values are empty or do not fit its type.</exception>
    public ParameterGrid<TCandidate, TAlgorithm, TSearchState> VaryBy<TValue>(string name, IReadOnlyList<TValue> values)
    {
        var matches = Prototype.GetParameters().Where(parameter => parameter.Name == name).ToArray();
        if (matches.Length != 1)
            throw new ArgumentException($"'{name}' must identify exactly one declared parameter of the prototype.", nameof(name));

        var parameter = matches[0];
        var snapshot = SnapshotValues(values, $"parameter '{name}'");
        if (snapshot.Any(value => !parameter.AcceptsValue(value)))
            throw new ArgumentException($"Values for parameter '{name}' must match its declared value type.", nameof(values));
        return AddDimension(new ParameterDimension<TAlgorithm, TValue>(parameter, snapshot));
    }

    internal static ValueArray<TValue> SnapshotValues<TValue>(IReadOnlyList<TValue> values, string label)
    {
        if (values.Count == 0)
            throw new ArgumentException($"Supply at least one value for {label}.", nameof(values));
        return values.ToValueArray();
    }

    internal ParameterGrid<TCandidate, TAlgorithm, TSearchState> AddDimension(ParameterGridDimension<TAlgorithm> dimension) =>
        this with { StoredDimensions = [.. StoredDimensions, dimension] };

    internal int ParameterCandidateCount => StoredDimensions.Aggregate(1, (count, dimension) => checked(count * dimension.Count));

    internal (TAlgorithm Algorithm, ImmutableArray<(IParameter Parameter, object? Value)> Settings) Decode(int position, int parameterCandidateCount)
    {
        // Earlier dimensions vary more slowly. Apply them in declaration order.
        var stride = parameterCandidateCount;
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, stride);
        var algorithm = Prototype;
        foreach (var dimension in StoredDimensions)
        {
            stride /= dimension.Count;
            algorithm = dimension.Apply(algorithm, position / stride);
            position %= stride;
        }

        var settings = ImmutableArray.CreateBuilder<(IParameter Parameter, object? Value)>();
        var labelled = new HashSet<IParameter>(ReferenceEqualityComparer.Instance);
        foreach (var dimension in StoredDimensions)
        {
            if (dimension.Parameter is { } parameter && labelled.Add(parameter))
                settings.Add((parameter, parameter.GetValue(algorithm)));
        }

        return (algorithm, settings.ToImmutable());
    }
}

internal abstract record ParameterGridDimension<TAlgorithm>
{
    internal abstract int Count { get; }
    internal virtual IParameter? Parameter => null;
    internal abstract ValueArray<object?> BoxedValues { get; }
    internal abstract TAlgorithm Apply(TAlgorithm algorithm, int index);
}

internal sealed record ConfiguratorDimension<TAlgorithm, TValue>(ValueArray<TValue> Values, Func<TAlgorithm, TValue, TAlgorithm> Configure)
    : ParameterGridDimension<TAlgorithm>
{
    internal override int Count => Values.Count;
    internal override ValueArray<object?> BoxedValues => Values.Select(value => (object?)value).ToValueArray();
    internal override TAlgorithm Apply(TAlgorithm algorithm, int index) => Configure(algorithm, Values[index]);
}

internal sealed record ParameterDimension<TAlgorithm, TValue>(IParameter Descriptor, ValueArray<TValue> Values)
    : ParameterGridDimension<TAlgorithm>
    where TAlgorithm : class, IConfigurationNode
{
    internal override int Count => Values.Count;
    internal override IParameter Parameter => Descriptor;
    internal override ValueArray<object?> BoxedValues => Values.Select(value => (object?)value).ToValueArray();
    internal override TAlgorithm Apply(TAlgorithm algorithm, int index) => Descriptor.WithValue(algorithm, Values[index]);
}
