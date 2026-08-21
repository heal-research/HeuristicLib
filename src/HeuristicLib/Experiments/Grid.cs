using System.Collections;

namespace HEAL.HeuristicLib.Experiments;

public sealed record Grid<T> : IEnumerable<T>
{
    public ValueArray<T> Configurations { get; }

    public Grid(T prototype)
    {
        Configurations = [prototype];
    }

    private Grid(ValueArray<T> configurations)
    {
        Configurations = configurations;
    }

    public Grid<T> VaryBy<TValue>(IReadOnlyList<TValue> values, Func<T, TValue, T> configurator)
    {
        var configurations = new T[checked(Configurations.Count * values.Count)];
        var index = 0;
        foreach (var configuration in Configurations)
        {
            foreach (var value in values)
            {
                configurations[index++] = configurator(configuration, value);
            }
        }

        return new(ValueArray.FromOwnedArray(configurations));
    }

    public ImmutableArray<T> GetConfigurations() => Configurations.AsImmutableArray();

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Configurations).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class Grid
{
    public static Grid<T> Create<T>(T prototype)
    {
        return new(prototype);
    }
}
