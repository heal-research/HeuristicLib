namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class DataFrame
{
    private readonly Dictionary<string, Series<double>> doubleSeriesByName;

    public DataFrame(IEnumerable<KeyValuePair<string, Series<double>>> doubleSeries)
    {
        doubleSeriesByName = new Dictionary<string, Series<double>>(StringComparer.Ordinal);
        foreach (var (name, series) in doubleSeries)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Series name must not be empty.", nameof(doubleSeries));

            if (series.Name is not null && !StringComparer.Ordinal.Equals(series.Name, name))
            {
                throw new ArgumentException(
                    $"Series name '{series.Name}' does not match data frame column name '{name}'.",
                    nameof(doubleSeries));
            }

            if (!doubleSeriesByName.TryAdd(name, series))
                throw new ArgumentException($"Series '{name}' is specified more than once.", nameof(doubleSeries));

            if (doubleSeriesByName.Count == 1)
            {
                RowCount = series.Count;
            }
            else if (series.Count != RowCount)
            {
                throw new ArgumentException(
                  $"Series '{name}' has {series.Count} rows but the data frame has {RowCount} rows.",
                  nameof(doubleSeries));
            }
        }
    }

    public int RowCount { get; }
    public IReadOnlyCollection<string> DoubleSeriesNames => doubleSeriesByName.Keys;

    public static DataFrame FromColumns(IEnumerable<KeyValuePair<string, Series<double>>> columns) =>
        new(columns);

    public static DataFrame FromOwnedColumns(IEnumerable<KeyValuePair<string, double[]>> columns) =>
        new(columns.Select(column => KeyValuePair.Create(column.Key, Series<double>.FromOwnedArray(column.Value, column.Key))));

    public static DataFrame FromMatrix(IReadOnlyList<string> names, double[,] values)
    {
        var columnCount = values.GetLength(1);
        if (names.Count != columnCount)
            throw new ArgumentException("Series names must match the number of matrix columns.", nameof(names));

        var rowCount = values.GetLength(0);
        var columns = new KeyValuePair<string, double[]>[columnCount];
        for (var column = 0; column < columnCount; column++)
        {
            var columnValues = new double[rowCount];
            for (var row = 0; row < rowCount; row++)
            {
                columnValues[row] = values[row, column];
            }

            columns[column] = KeyValuePair.Create(names[column], columnValues);
        }

        return FromOwnedColumns(columns);
    }

    public Series<double> GetDoubleSeries(string name)
    {
        if (!doubleSeriesByName.TryGetValue(name, out var series))
            throw new ArgumentException($"No double series named '{name}' exists.", nameof(name));

        return series;
    }
}
