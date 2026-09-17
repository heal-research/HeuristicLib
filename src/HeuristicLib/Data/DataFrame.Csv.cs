using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace HEAL.HeuristicLib.Data;

public sealed partial class DataFrame
{
    /// <summary>
    /// Reads a delimited text file into a data frame.
    /// When <paramref name="dataTypes"/> is omitted, each complete column is inferred as <see cref="bool"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/>, <see cref="DateTime"/>, or <see cref="string"/>, in that order.
    /// </summary>
    public static DataFrame ReadCsv(string path, char delimiter = ',', bool hasHeader = true, IReadOnlyList<Type>? dataTypes = null, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.InvariantCulture;

        var configuration = new CsvConfiguration(culture)
        {
            Delimiter = delimiter.ToString(),
            HasHeaderRecord = hasHeader
        };

        using var textReader = File.OpenText(path);
        using var reader = new CsvReader(textReader, configuration);

        var hasRecord = reader.Read();
        if (!hasRecord)
            throw new InvalidDataException("The CSV file does not contain any columns.");

        string[] names;
        if (hasHeader)
        {
            reader.ReadHeader();
            names = reader.HeaderRecord ?? throw new InvalidDataException("The CSV file does not contain a header.");
            hasRecord = reader.Read();
        }
        else
        {
            names = new string[reader.Parser.Count];
            for (var columnIndex = 0; columnIndex < names.Length; columnIndex++)
                names[columnIndex] = $"Column{columnIndex}";
        }

        var columnCount = names.Length;
        if (dataTypes is not null && dataTypes.Count != columnCount)
            throw new ArgumentException($"The CSV file has {columnCount} columns but {dataTypes.Count} data types were supplied.", nameof(dataTypes));

        var values = new List<string>[columnCount];
        for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            values[columnIndex] = [];

        while (hasRecord)
        {
            if (reader.Parser.Count != columnCount)
                throw new FormatException($"CSV row {reader.Parser.Row} has {reader.Parser.Count} fields; expected {columnCount}.");

            for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
            {
                var value = reader.GetField(columnIndex) ?? throw new FormatException($"CSV row {reader.Parser.Row}, column {columnIndex} contains a null value.");
                values[columnIndex].Add(value);
            }

            hasRecord = reader.Read();
        }

        var columns = new Series[columnCount];
        for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
        {
            var dataType = dataTypes?[columnIndex] ?? InferDataType(values[columnIndex], culture);
            columns[columnIndex] = CreateSeries(names[columnIndex], values[columnIndex], dataType, culture);
        }

        return new DataFrame(columns);
    }

    private static Type InferDataType(IReadOnlyList<string> values, CultureInfo culture)
    {
        if (values.Count == 0)
            return typeof(string);

        if (AllValuesParse<bool>(values, culture))
            return typeof(bool);
        if (AllValuesParse<int>(values, culture))
            return typeof(int);
        if (AllValuesParse<long>(values, culture))
            return typeof(long);
        if (AllValuesParse<double>(values, culture))
            return typeof(double);
        if (AllValuesParse<DateTime>(values, culture))
            return typeof(DateTime);

        return typeof(string);
    }

    private static bool AllValuesParse<T>(IReadOnlyList<string> values, IFormatProvider formatProvider)
        where T : IParsable<T>
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (!T.TryParse(values[i], formatProvider, out _))
                return false;
        }

        return true;
    }

    private static Series CreateSeries(string name, IReadOnlyList<string> values, Type dataType, IFormatProvider formatProvider)
    {
        if (dataType == typeof(string))
            return Series<string>.FromOwnedArray(name, values.ToArray());
        if (dataType == typeof(bool))
            return ParseSeries<bool>(name, values, formatProvider);
        if (dataType == typeof(int))
            return ParseSeries<int>(name, values, formatProvider);
        if (dataType == typeof(long))
            return ParseSeries<long>(name, values, formatProvider);
        if (dataType == typeof(float))
            return ParseSeries<float>(name, values, formatProvider);
        if (dataType == typeof(double))
            return ParseSeries<double>(name, values, formatProvider);
        if (dataType == typeof(decimal))
            return ParseSeries<decimal>(name, values, formatProvider);
        if (dataType == typeof(DateTime))
            return ParseSeries<DateTime>(name, values, formatProvider);

        throw new NotSupportedException($"CSV columns of type {dataType.Name} are not supported.");
    }

    private static Series<T> ParseSeries<T>(string name, IReadOnlyList<string> values, IFormatProvider formatProvider)
        where T : IParsable<T>
    {
        var parsedValues = new T[values.Count];
        for (var rowIndex = 0; rowIndex < values.Count; rowIndex++)
        {
            var value = values[rowIndex];
            if (!T.TryParse(value, formatProvider, out var parsedValue))
                throw new FormatException($"Value '{value}' in column '{name}' at data row {rowIndex} cannot be parsed as {typeof(T).Name}.");

            parsedValues[rowIndex] = parsedValue;
        }

        return Series<T>.FromOwnedArray(name, parsedValues);
    }
}
