using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;
using HEAL.HeuristicLib.Numerics.Optimization;

namespace HEAL.HeuristicLib.DataAnalysis.Regression;

internal static class ConstantOptimizer
{
    internal static ExpressionTree Optimize(ExpressionTree expression, RegressionData data, int maximumIterations, CancellationToken cancellationToken = default)
    {
        if (TryOptimize(expression, data, maximumIterations, out var optimizedExpression, out var failure, cancellationToken))
            return optimizedExpression;

        throw failure switch
        {
            ConstantOptimizationFailure.Compilation(var compilation) => new NotSupportedException($"Expression symbol '{compilation.Symbol.Name}' emitted unsupported operation '{compilation.UnsupportedOperation}' during constant optimization."),
            ConstantOptimizationFailure.VariableBinding({ Reason: VariableBindingFailureReason.Missing } binding) => new ArgumentException($"Regression data does not contain the expression variable '{binding.VariableName}'.", nameof(data)),
            ConstantOptimizationFailure.VariableBinding({ Reason: VariableBindingFailureReason.IncompatibleType } binding) => new ArgumentException($"Regression data variable '{binding.VariableName}' is not a double series.", nameof(data)),
            ConstantOptimizationFailure.NumericalOptimization(var numericalOptimization) => new InvalidOperationException($"Numerical constant optimization failed: {numericalOptimization.Message}"),
            _ => new InvalidOperationException($"Unsupported constant-optimization failure: {failure.GetType().Name}.")
        };
    }

    internal static bool TryOptimize(ExpressionTree expression, RegressionData data, int maximumIterations,
        [NotNullWhen(true)] out ExpressionTree? optimizedExpression, CancellationToken cancellationToken = default) =>
        TryOptimize(expression, data, maximumIterations, out optimizedExpression, out _, cancellationToken);

    internal static bool TryOptimize(ExpressionTree expression, RegressionData data, int maximumIterations,
        [NotNullWhen(true)] out ExpressionTree? optimizedExpression, [NotNullWhen(false)] out ConstantOptimizationFailure? failure, CancellationToken cancellationToken = default)
    {
        if (maximumIterations < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "The maximum number of iterations must not be negative.");

        if (data.RowCount == 0)
            throw new ArgumentException("Regression data must contain at least one row.", nameof(data));

        cancellationToken.ThrowIfCancellationRequested();
        if (maximumIterations == 0 || !expression.FindNodesOfSymbol<EvolvableConstantSymbol>().Any())
        {
            optimizedExpression = expression;
            failure = null;
            return true;
        }

        if (!DifferentiableExpressionCompiler.TryCompile(expression, out var differentiableExpression, out var compilationFailure))
        {
            optimizedExpression = null;
            failure = new ConstantOptimizationFailure.Compilation(compilationFailure);
            return false;
        }

        if (!differentiableExpression.TryCreateExecution(data.Inputs, out var execution, out var bindingFailure))
        {
            optimizedExpression = null;
            failure = new ConstantOptimizationFailure.VariableBinding(bindingFailure);
            return false;
        }

        using (execution)
        {
            var initialParameters = differentiableExpression.CreateInitialParameterValues();
            if (!LevenbergMarquardt.TryMinimize(execution, initialParameters, data.Target.Values.Span, maximumIterations, out var result, out var optimizationFailure, cancellationToken))
            {
                optimizedExpression = null;
                failure = new ConstantOptimizationFailure.NumericalOptimization(optimizationFailure);
                return false;
            }

            optimizedExpression = differentiableExpression.WithParameterValues(result.Parameters);
            failure = null;
            return true;
        }
    }
}

internal abstract record ConstantOptimizationFailure
{
    internal sealed record Compilation(ExpressionCompilationFailure Failure) : ConstantOptimizationFailure;
    internal sealed record VariableBinding(VariableBindingFailure Failure) : ConstantOptimizationFailure;
    internal sealed record NumericalOptimization(LevenbergMarquardtFailure Failure) : ConstantOptimizationFailure;
}
