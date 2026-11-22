using System.Diagnostics.CodeAnalysis;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Optimization;
using AD = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Numerics.LeastSquares;

internal static class LevenbergMarquardt
{
    internal static bool TryMinimize(AD.Execution model, ReadOnlySpan<double> initialParameters, ReadOnlySpan<double> targets, int maximumIterations,
        [NotNullWhen(true)] out LevenbergMarquardtResult? result, CancellationToken cancellationToken = default) =>
        TryMinimize(model, initialParameters, targets, maximumIterations, out result, out _, cancellationToken);

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
            var objective = ObjectiveFunction.NonlinearModel(EvaluateModel, EvaluateJacobian, unusedIndependentVariables, targetVector);
            result = Minimize(objective, new DenseVector(initialParameters.ToArray()), rowCount, maximumIterations, cancellationToken);
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

    private static LevenbergMarquardtResult Minimize(IObjectiveModel objective, Vector<double> parameters, int rowCount, int maximumIterations, CancellationToken cancellationToken)
    {
        // Use the unbounded LM update directly. MathNet's minimizer also computes
        // a covariance by SVD when constructing its result, which can stall on finite data.
        const double tolerance = 1e-15;
        objective.SetParameters(parameters);
        objective.EvaluateAt(parameters);
        var error = objective.Value;
        if (double.IsInfinity(error))
            throw new ArithmeticException("The initial residual sum of squares is infinite.");

        double damping = 0;
        for (var iteration = 0; iteration < maximumIterations && error > tolerance; iteration++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var gradient = objective.Gradient;
            var gradientNorm = gradient.InfinityNorm();
            if (!double.IsFinite(gradientNorm))
                throw new ArithmeticException("The least-squares gradient is non-finite.");
            if (gradientNorm <= tolerance)
                break;

            var system = objective.Hessian.Clone();
            var diagonal = system.Diagonal();
            if (iteration == 0)
                damping = 1e-3 * diagonal.Maximum();

            var accepted = false;
            double multiplier = 2;
            for (var retry = 0; retry < 64 && double.IsFinite(damping); retry++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                system.SetDiagonal(diagonal + damping);
                var step = system.Solve(-gradient);
                if (step.L2Norm() <= tolerance * (parameters.L2Norm() + tolerance))
                    break;

                var trial = parameters + step;
                objective.EvaluateAt(trial);
                var trialError = objective.Value;
                var predictedReduction = step.DotProduct(damping * step - gradient);
                if (trialError < error && predictedReduction > 0)
                {
                    var gain = (error - trialError) / predictedReduction;
                    parameters = trial;
                    error = trialError;
                    damping *= Math.Max(1.0 / 3.0, 1 - Math.Pow(2 * gain - 1, 3));
                    accepted = true;
                    break;
                }

                damping *= multiplier;
                multiplier *= 2;
            }

            if (!accepted)
                break;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new LevenbergMarquardtResult(ValueArray.FromOwnedArray(parameters.ToArray()), error / rowCount);
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

internal sealed record LevenbergMarquardtResult(ValueArray<double> Parameters, double MeanSquaredError);

internal sealed record LevenbergMarquardtFailure(string Message);
