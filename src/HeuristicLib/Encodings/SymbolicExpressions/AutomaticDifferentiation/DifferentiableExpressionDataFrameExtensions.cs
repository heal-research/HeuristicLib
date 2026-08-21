using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Data;
using AD = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

internal static class DifferentiableExpressionDataFrameExtensions
{
    internal static bool TryCreateExecution(this DifferentiableExpression expression, DataFrame dataFrame, [NotNullWhen(true)] out AD.Execution? execution) =>
        expression.TryCreateExecution(dataFrame, out execution, out _);

    internal static bool TryCreateExecution(this DifferentiableExpression expression, DataFrame dataFrame, [NotNullWhen(true)] out AD.Execution? execution, [NotNullWhen(false)] out VariableBindingFailure? failure)
    {
        if (expression.VariableNames.IsEmpty)
        {
            execution = expression.Program.CreateExecution([], dataFrame.RowCount);
            failure = null;
            return true;
        }

        var inputColumns = new ReadOnlyMemory<double>[expression.VariableNames.Length];
        for (var inputIndex = 0; inputIndex < inputColumns.Length; inputIndex++)
        {
            var variableName = expression.VariableNames[inputIndex];
            if (!dataFrame.TryGet(variableName, out var series))
            {
                execution = null;
                failure = new VariableBindingFailure(variableName, VariableBindingFailureReason.Missing);
                return false;
            }

            if (series is not Series<double> doubleSeries)
            {
                execution = null;
                failure = new VariableBindingFailure(variableName, VariableBindingFailureReason.IncompatibleType);
                return false;
            }

            inputColumns[inputIndex] = doubleSeries.Values;
        }

        execution = expression.Program.CreateExecution(inputColumns, dataFrame.RowCount);
        failure = null;
        return true;
    }
}
