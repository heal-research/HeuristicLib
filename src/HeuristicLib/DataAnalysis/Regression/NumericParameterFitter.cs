using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;
using HEAL.HeuristicLib.Numerics.Optimization;

namespace HEAL.HeuristicLib.DataAnalysis.Regression;

internal static class NumericParameterFitter
{
    internal static ExpressionTree Fit(ExpressionTree expression, RegressionData data, int maximumIterations, CancellationToken cancellationToken = default)
    {
        if (TryFit(expression, data, maximumIterations, out var fittedExpression, out var failure, cancellationToken))
            return fittedExpression;

        throw CreateException(failure, nameof(data));
    }

    /// <summary>
    /// Translates a failure into the exception the throwing form reports, so callers that throw for only some failure
    /// kinds do not restate the message and exception-type mapping.
    /// </summary>
    internal static Exception CreateException(NumericParameterFittingFailure failure, string dataParameterName) => failure switch
    {
        NumericParameterFittingFailure.Compilation(var compilation) => new NotSupportedException($"Expression symbol '{compilation.Symbol.Name}' emitted unsupported operation '{compilation.UnsupportedOperation}' during numeric parameter fitting."),
        NumericParameterFittingFailure.VariableBinding({ Reason: VariableBindingFailureReason.Missing } binding) => new ArgumentException($"Regression data does not contain the expression variable '{binding.VariableName}'.", dataParameterName),
        NumericParameterFittingFailure.VariableBinding({ Reason: VariableBindingFailureReason.IncompatibleType } binding) => new ArgumentException($"Regression data variable '{binding.VariableName}' is not a double series.", dataParameterName),
        NumericParameterFittingFailure.NumericalOptimization(var numericalOptimization) => new InvalidOperationException($"Numeric parameter fitting failed: {numericalOptimization.Message}"),
        _ => new InvalidOperationException($"Unsupported numeric parameter fitting failure: {failure.GetType().Name}.")
    };

    internal static bool TryFit(ExpressionTree expression, RegressionData data, int maximumIterations,
        [NotNullWhen(true)] out ExpressionTree? fittedExpression, CancellationToken cancellationToken = default) =>
        TryFit(expression, data, maximumIterations, out fittedExpression, out _, cancellationToken);

    internal static bool TryFit(ExpressionTree expression, RegressionData data, int maximumIterations,
        [NotNullWhen(true)] out ExpressionTree? fittedExpression, [NotNullWhen(false)] out NumericParameterFittingFailure? failure, CancellationToken cancellationToken = default)
    {
        if (maximumIterations < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "The maximum number of iterations must not be negative.");

        if (data.RowCount == 0)
            throw new ArgumentException("Regression data must contain at least one row.", nameof(data));

        cancellationToken.ThrowIfCancellationRequested();
        if (maximumIterations == 0 || !expression.FindNodesOfSymbol<EvolvableConstantSymbol>().Any())
        {
            fittedExpression = expression;
            failure = null;
            return true;
        }

        if (!DifferentiableExpressionCompiler.TryCompile(expression, out var differentiableExpression, out var compilationFailure))
        {
            fittedExpression = null;
            failure = new NumericParameterFittingFailure.Compilation(compilationFailure);
            return false;
        }

        if (!differentiableExpression.TryCreateExecution(data.Inputs, out var execution, out var bindingFailure))
        {
            fittedExpression = null;
            failure = new NumericParameterFittingFailure.VariableBinding(bindingFailure);
            return false;
        }

        using (execution)
        {
            var initialParameters = differentiableExpression.CreateInitialParameterValues();
            if (!LevenbergMarquardt.TryMinimize(execution, initialParameters, data.Target.Values.Span, maximumIterations, out var result, out var optimizationFailure, cancellationToken))
            {
                fittedExpression = null;
                failure = new NumericParameterFittingFailure.NumericalOptimization(optimizationFailure);
                return false;
            }

            fittedExpression = differentiableExpression.WithParameterValues(result.Parameters);
            failure = null;
            return true;
        }
    }
}

internal abstract record NumericParameterFittingFailure
{
    internal sealed record Compilation(ExpressionCompilationFailure Failure) : NumericParameterFittingFailure;
    internal sealed record VariableBinding(VariableBindingFailure Failure) : NumericParameterFittingFailure;
    internal sealed record NumericalOptimization(LevenbergMarquardtFailure Failure) : NumericParameterFittingFailure;
}
