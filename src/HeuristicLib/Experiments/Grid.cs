using System.Collections;

namespace HEAL.HeuristicLib.Experiments;

public sealed record Grid<T> : IEnumerable<T>
{
    public T Prototype { get; }

    public ValueArray<IGridParameter<T>> Parameters { get; }

    public Grid(T prototype)
    {
        Prototype = prototype;
    }

    public Grid(T prototype, IReadOnlyList<IGridParameter<T>> parameters)
    {
        Prototype = prototype;
        Parameters = parameters.ToValueArray();
    }

    public Grid<T> VaryBy<TProblem>(IReadOnlyList<TProblem> values, Func<T, TProblem, T> configurator)
    {
        if (values.Count == 0)
            throw new ArgumentException("A grid dimension must contain at least one value.", nameof(values));

        return new(Prototype, [.. Parameters, new GridParameter<T, TProblem>(values, configurator)]);
    }

    public ImmutableArray<T> GetConfigurations()
    {
        IEnumerable<T> configurations = new List<T> { Prototype };

        return Parameters.Aggregate(configurations, (current, parameter) => parameter.GetConfigurations(current)).ToImmutableArray();
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)GetConfigurations()).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public interface IGridParameter<T>
{
    public int Count { get; }
    IEnumerable<T> GetConfigurations(IEnumerable<T> prototypes);
}

public sealed record GridParameter<T, TParam> : IGridParameter<T>
{
    public ValueArray<TParam> Values { get; }

    public Func<T, TParam, T> Configurator { get; }

    public int Count => Values.Count;

    public GridParameter(IReadOnlyList<TParam> values, Func<T, TParam, T> configurator)
    {
        Values = values.ToValueArray();
        Configurator = configurator;
    }

    public IEnumerable<T> GetConfigurations(IEnumerable<T> prototypes)
    {
        return prototypes.SelectMany(prototype => Values.Select(value => Configurator(prototype, value)));
    }
}

public static class Grid
{
    public static Grid<T> Create<T>(T prototype)
    {
        return new(prototype);
    }

}
