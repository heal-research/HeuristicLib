using System.Numerics;
using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Regression;

namespace HEAL.HeuristicLib.PythonInterop;

internal static class PythonRegressionData
{
    public static RegressionData ReadCsv(string path, int trainingRowCount)
    {
        var frame = DataFrame.ReadCsv(path, DetectDelimiter(path));
        if (trainingRowCount <= 0 || trainingRowCount > frame.RowCount)
            throw new ArgumentOutOfRangeException(nameof(trainingRowCount));

        return Create(frame, trainingRowCount);
    }

    public static RegressionData ReadCsv(string path, double trainingFraction)
    {
        if (double.IsNaN(trainingFraction) || trainingFraction is <= 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(trainingFraction));

        var frame = DataFrame.ReadCsv(path, DetectDelimiter(path));
        var trainingRowCount = (int)(frame.RowCount * trainingFraction);
        if (trainingRowCount == 0)
            throw new ArgumentException("The training fraction does not select any rows.", nameof(trainingFraction));

        return Create(frame, trainingRowCount);
    }

    private static RegressionData Create(DataFrame frame, int trainingRowCount)
    {
        if (frame.ColumnCount < 2)
            throw new InvalidDataException("Regression data must contain at least one input column and one target column.");

        var target = ToDoubleSeries(frame[frame.ColumnCount - 1], trainingRowCount);
        var inputs = new List<Series>(frame.ColumnCount - 1);
        for (var columnIndex = 0; columnIndex < frame.ColumnCount - 1; columnIndex++)
        {
            var input = ToDoubleSeries(frame[columnIndex], trainingRowCount);
            if (!IsConstant(input.Values.Span))
                inputs.Add(input);
        }

        if (inputs.Count == 0)
            throw new InvalidDataException("Regression data must contain at least one non-constant input column.");

        return new RegressionData(new DataFrame(inputs), target);
    }

    private static Series<double> ToDoubleSeries(Series series, int count)
    {
        var values = series switch
        {
            Series<double> doubles => doubles.Values.Span[..count].ToArray(),
            Series<float> singles => Convert(singles.Values.Span[..count]),
            Series<int> integers => Convert(integers.Values.Span[..count]),
            Series<long> integers => Convert(integers.Values.Span[..count]),
            Series<decimal> decimals => Convert(decimals.Values.Span[..count]),
            _ => throw new InvalidDataException(
                $"Regression column '{series.Name}' has unsupported type {series.DataType.Name}.")
        };

        return Series<double>.FromOwnedArray(series.Name, values);
    }

    private static double[] Convert<T>(ReadOnlySpan<T> source)
        where T : INumber<T>
    {
        var values = new double[source.Length];
        for (var i = 0; i < source.Length; i++)
            values[i] = double.CreateChecked(source[i]);

        return values;
    }

    private static bool IsConstant(ReadOnlySpan<double> values)
    {
        if (values.Length < 2)
            return false;

        var first = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            if (!values[i].Equals(first))
                return false;
        }

        return true;
    }

    private static char DetectDelimiter(string path) =>
        string.Equals(Path.GetExtension(path), ".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ',';
}
