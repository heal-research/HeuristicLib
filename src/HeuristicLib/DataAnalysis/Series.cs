namespace HEAL.HeuristicLib.DataAnalysis;

public abstract class Series
{
    protected Series(string name, int count)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Series name must not be empty.", nameof(name));

        Name = name;
        Count = count;
    }

    public string Name { get; }
    public int Count { get; }
    public abstract Type DataType { get; }
}

public sealed class Series<T> : Series
    where T : notnull
{
    private readonly T[] values;

    public Series(string name, IEnumerable<T> values)
        : this(name, values.ToArray())
    {
    }

    private Series(string name, T[] values)
        : base(name, values.Length)
    {
        this.values = values;
    }

    public override Type DataType => typeof(T);
    public ReadOnlyMemory<T> Values => values;

    /// <summary>
    /// Creates a series backed by <paramref name="values"/> without copying it.
    /// The caller must not modify the array after transferring ownership.
    /// </summary>
    public static Series<T> FromOwnedArray(string name, T[] values) =>
        new(name, values);
}
