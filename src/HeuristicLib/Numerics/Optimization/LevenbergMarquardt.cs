using System.Diagnostics.CodeAnalysis;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Optimization;
using AD = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Numerics.Optimization;

internal static class LevenbergMarquardt
{
    internal static bool TryMinimize(AD.Execution model, ReadOnlySpan<double> initialParameters, ReadOnlySpan<double> targets, int maximumIterations,
        [NotNullWhen(true)] out LevenbergMarquardtResult? result, [NotNullWhen(false)] out LevenbergMarquardtFailure? failure, CancellationToken cancellationToken = default)
    {
        ValidateArguments(model, initialParameters, targets, maximumIterations);
        cancellationToken.ThrowIfCancellationRequested();
        var rowCount = model.RowCount;
        var parameterCount = model.ParameterCount;
        var modelValues = new double[rowCount];
        var jacobianModelValues = new double[rowCount];
        var jacobianValues = new double[checked(rowCount * parameterCount)];
        var modelVector = new DenseVector(modelValues);
        var jacobianMatrix = new DenseMatrix(rowCount, parameterCount, jacobianValues);

        Vector<double> EvaluateModel(Vector<double> parameters, Vector<double> _)
        {
            cancellationToken.ThrowIfCancellationRequested();
            model.Evaluate(GetParameterValues(parameters), modelValues);
            return modelVector;
        }

        Matrix<double> EvaluateJacobian(Vector<double> parameters, Vector<double> _)
        {
            cancellationToken.ThrowIfCancellationRequested();
            model.EvaluateWithJacobian(GetParameterValues(parameters), jacobianModelValues, jacobianValues);
            return jacobianMatrix;
        }

        try
        {
            var targetVector = new DenseVector(targets.ToArray());
            var unusedIndependentVariables = new DenseVector(rowCount);
            var objective = ObjectiveFunction.NonlinearModel(EvaluateModel, EvaluateJacobian, unusedIndependentVariables, targetVector, null);
            var minimizer = new LevenbergMarquardtMinimizer(maximumIterations: maximumIterations);
            var minimum = minimizer.FindMinimum(objective, new DenseVector(initialParameters.ToArray()));
            result = new LevenbergMarquardtResult(minimum.MinimizingPoint.ToArray(), minimum.ModelInfoAtMinimum.Value / rowCount);
            failure = null;
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            result = null;
            failure = new LevenbergMarquardtFailure(exception.Message);
            return false;
        }
    }

    private static double[] GetParameterValues(Vector<double> parameters) => parameters is DenseVector denseParameters ? denseParameters.Values : parameters.ToArray();

    private static void ValidateArguments(AD.Execution model, ReadOnlySpan<double> initialParameters, ReadOnlySpan<double> targets, int maximumIterations)
    {
        model.ThrowIfDisposed();
        if (model.ParameterCount == 0)
            throw new ArgumentException("The model must have at least one parameter.", nameof(model));

        if (initialParameters.Length != model.ParameterCount)
            throw new ArgumentException($"Expected {model.ParameterCount} initial parameters but received {initialParameters.Length}.", nameof(initialParameters));

        if (targets.Length != model.RowCount)
            throw new ArgumentException($"Expected {model.RowCount} target values but received {targets.Length}.", nameof(targets));

        if (maximumIterations < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumIterations), maximumIterations, "The maximum number of iterations must not be negative.");
    }
}

internal sealed record LevenbergMarquardtResult(double[] Parameters, double MeanSquaredError);

internal sealed record LevenbergMarquardtFailure(string Message);
