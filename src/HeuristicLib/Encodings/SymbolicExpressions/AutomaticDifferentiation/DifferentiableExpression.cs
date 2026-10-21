using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Data;
using AD = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

internal sealed class DifferentiableExpression
{
    internal DifferentiableExpression(ExpressionTree expression, AD.Program program, ImmutableArray<string> variableNames, ImmutableArray<ParameterBinding> parameterBindings)
    {
        if (program.InputCount != variableNames.Length)
            throw new ArgumentException($"The program has {program.InputCount} inputs but {variableNames.Length} variable names were provided.", nameof(variableNames));
        if (program.ParameterCount != parameterBindings.Length)
            throw new ArgumentException($"The program has {program.ParameterCount} parameters but {parameterBindings.Length} parameter bindings were provided.", nameof(parameterBindings));

        Expression = expression;
        Program = program;
        VariableNames = variableNames;
        ParameterBindings = parameterBindings;
    }

    internal ExpressionTree Expression { get; }
    internal AD.Program Program { get; }
    internal ImmutableArray<string> VariableNames { get; }
    internal ImmutableArray<ParameterBinding> ParameterBindings { get; }
    internal int ParameterCount => ParameterBindings.Length;

    internal double[] CreateInitialParameterValues()
    {
        var parameterValues = new double[ParameterBindings.Length];
        for (var parameterIndex = 0; parameterIndex < parameterValues.Length; parameterIndex++)
            parameterValues[parameterIndex] = ParameterBindings[parameterIndex].InitialValue;

        return parameterValues;
    }

    internal ExpressionTree WithParameterValues(ReadOnlySpan<double> parameterValues)
    {
        if (parameterValues.Length != ParameterBindings.Length)
            throw new ArgumentException($"Expected {ParameterBindings.Length} parameter values but received {parameterValues.Length}.", nameof(parameterValues));
        if (parameterValues.IsEmpty)
            return Expression;

        var replacements = ParameterBindings.Zip(parameterValues.ToArray(), static (binding, value) => binding.CreateReplacement(value));
        return Expression.ReplaceMany(replacements);
    }
}

internal static class DifferentiableExpressionDataFrameExtensions
{
    extension(DifferentiableExpression expression)
    {
        internal bool TryCreateExecution(DataFrame dataFrame, [NotNullWhen(true)] out AD.Execution? execution) =>
            expression.TryCreateExecution(dataFrame, out execution, out _);

        internal bool TryCreateExecution(DataFrame dataFrame, [NotNullWhen(true)] out AD.Execution? execution, [NotNullWhen(false)] out VariableBindingFailure? failure)
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
}
