using System.Diagnostics.CodeAnalysis;

namespace HEAL.HeuristicLib.Data;

public sealed partial class DataFrame
{
    private readonly ImmutableArray<Series> columns;
    private readonly Dictionary<string, int> columnIndexByName;

    public DataFrame(IEnumerable<Series> columns)
    {
        this.columns = columns.ToImmutableArray();
        columnIndexByName = new Dictionary<string, int>(this.columns.Length, StringComparer.Ordinal);

        for (var i = 0; i < this.columns.Length; i++)
        {
            var column = this.columns[i];
            if (!columnIndexByName.TryAdd(column.Name, i))
                throw new ArgumentException($"Series '{column.Name}' is specified more than once.", nameof(columns));

            if (i == 0)
            {
                RowCount = column.Count;
            }
            else if (column.Count != RowCount)
            {
                throw new ArgumentException($"Series '{column.Name}' has {column.Count} rows but the data frame has {RowCount} rows.", nameof(columns));
            }
        }
    }

    public int RowCount { get; }
    public int ColumnCount => columns.Length;
    public IReadOnlyList<Series> Columns => columns;

    public Series this[string name] =>
        columnIndexByName.TryGetValue(name, out var index)
            ? columns[index]
            : throw new KeyNotFoundException($"No series named '{name}' exists.");

    public Series this[int index] => columns[index];

    public Series<T> Get<T>(string name)
        where T : notnull
    {
        var series = this[name];
        if (series is Series<T> typedSeries)
            return typedSeries;

        throw new InvalidOperationException(
            $"Series '{name}' contains values of type {series.DataType.Name}, not {typeof(T).Name}.");
    }

    public bool TryGet(string name, [NotNullWhen(true)] out Series? series)
    {
        if (columnIndexByName.TryGetValue(name, out var index))
        {
            series = columns[index];
            return true;
        }

        series = null;
        return false;
    }

    public bool TryGet<T>(string name, [NotNullWhen(true)] out Series<T>? series)
        where T : notnull
    {
        if (TryGet(name, out var candidate) && candidate is Series<T> typedSeries)
        {
            series = typedSeries;
            return true;
        }

        series = null;
        return false;
    }

    public static DataFrame FromMatrix<T>(IReadOnlyList<string> names, T[,] values)
        where T : notnull
    {
        var columnCount = values.GetLength(1);
        if (names.Count != columnCount)
            throw new ArgumentException("Series names must match the number of matrix columns.", nameof(names));

        var rowCount = values.GetLength(0);
        var columns = new Series[columnCount];
        for (var column = 0; column < columnCount; column++)
        {
            var columnValues = new T[rowCount];
            for (var row = 0; row < rowCount; row++)
                columnValues[row] = values[row, column];

            columns[column] = Series<T>.FromOwnedArray(names[column], columnValues);
        }

        return new DataFrame(columns);
    }
}
