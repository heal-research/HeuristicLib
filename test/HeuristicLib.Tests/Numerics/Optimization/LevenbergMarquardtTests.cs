using HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;
using HEAL.HeuristicLib.Numerics.Optimization;
using DifferentiationExecution = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation.Execution;

namespace HEAL.HeuristicLib.Tests.Numerics.Optimization;

public sealed class LevenbergMarquardtTests
{
    [Fact]
    public void TryMinimizeFitsLinearModel()
    {
        double[] input = [-2.0, -1.0, 0.0, 1.0, 2.0];
        double[] targets = [-4.0, -1.0, 2.0, 5.0, 8.0];
        var builder = new Builder();
        var x = builder.Input();
        var intercept = builder.Parameter();
        var slope = builder.Parameter();
        var program = builder.Build(builder.Add(intercept, builder.Multiply(slope, x)));
        using var execution = program.CreateExecution([input], input.Length);

        var success = LevenbergMarquardt.TryMinimize(execution, [0.0, 0.0], targets, maximumIterations: 100, out var result, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        failure.ShouldBeNull();
        result.ShouldNotBeNull();
        result.Parameters[0].ShouldBe(2.0, 1e-8);
        result.Parameters[1].ShouldBe(3.0, 1e-8);
        result.MeanSquaredError.ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void TryMinimizeFitsNonlinearExponentialModel()
    {
        double[] input = [0.0, 0.25, 0.75, 1.25, 2.0];
        var targets = input.Select(value => 2.5 * Math.Exp(-0.7 * value)).ToArray();
        var builder = new Builder();
        var x = builder.Input();
        var scale = builder.Parameter();
        var rate = builder.Parameter();
        var program = builder.Build(builder.Multiply(scale, builder.Exp(builder.Multiply(rate, x))));
        using var execution = program.CreateExecution([input], input.Length);

        var success = LevenbergMarquardt.TryMinimize(execution, [1.0, -0.2], targets, 100, out var result, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        failure.ShouldBeNull();
        result.ShouldNotBeNull();
        result.Parameters[0].ShouldBe(2.5, 1e-8);
        result.Parameters[1].ShouldBe(-0.7, 1e-8);
        result.MeanSquaredError.ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void TryMinimizeReportsMeanSquaredErrorForNoisyData()
    {
        double[] input = [-2.0, -1.0, 0.0, 1.0, 2.0];
        double[] targets = [-3.1, -1.05, 1.2, 2.9, 5.15];
        using var execution = CreateLinearExecution(input);

        var success = LevenbergMarquardt.TryMinimize(execution, [0.0, 0.0], targets, 100, out var result, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        failure.ShouldBeNull();
        result.ShouldNotBeNull();
        var outputs = new double[input.Length];
        execution.Evaluate(result.Parameters, outputs);
        var expectedMeanSquaredError = outputs.Zip(targets, static (output, target) => (output - target) * (output - target)).Average();
        result.MeanSquaredError.ShouldBe(expectedMeanSquaredError, 1e-14);
        result.MeanSquaredError.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public void TryMinimizeUsesParameterMajorJacobianAsSolverColumns()
    {
        double[] input = [0.2, 0.7, 1.5, 2.2, 3.1, 4.0];
        var targets = input.Select(value => 1.2 - (0.75 * value) + (0.4 * value * value)).ToArray();
        var builder = new Builder();
        var x = builder.Input();
        var constant = builder.Parameter();
        var linear = builder.Parameter();
        var quadratic = builder.Parameter();
        var program = builder.Build(builder.Add(constant, builder.Add(builder.Multiply(linear, x), builder.Multiply(quadratic, builder.Multiply(x, x)))));
        using var execution = program.CreateExecution([input], input.Length);

        var success = LevenbergMarquardt.TryMinimize(execution, [0.0, 0.0, 0.0], targets, 100, out var result, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        failure.ShouldBeNull();
        result.ShouldNotBeNull();
        result.Parameters[0].ShouldBe(1.2, 1e-7);
        result.Parameters[1].ShouldBe(-0.75, 1e-7);
        result.Parameters[2].ShouldBe(0.4, 1e-7);
    }

    [Fact]
    public void TryMinimizeReusesExecutionAcrossIndependentSolves()
    {
        double[] input = [-2.0, -1.0, 0.0, 1.0, 2.0];
        using var execution = CreateLinearExecution(input);

        LevenbergMarquardt.TryMinimize(execution, [0.0, 0.0], [-3.0, -1.0, 1.0, 3.0, 5.0], 100, out var first, out _, TestContext.Current.CancellationToken).ShouldBeTrue();
        LevenbergMarquardt.TryMinimize(execution, [4.0, 4.0], [-4.0, -3.5, -3.0, -2.5, -2.0], 100, out var second, out _, TestContext.Current.CancellationToken).ShouldBeTrue();

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.Parameters[0].ShouldBe(1.0, 1e-8);
        first.Parameters[1].ShouldBe(2.0, 1e-8);
        second.Parameters[0].ShouldBe(-3.0, 1e-8);
        second.Parameters[1].ShouldBe(0.5, 1e-8);
    }

    [Fact]
    public void ResultOwnsParametersAfterInputsAndExecutionLifetimeEnd()
    {
        double[] input = [-1.0, 0.0, 1.0];
        double[] initialParameters = [0.0, 0.0];
        double[] targets = [-1.0, 1.0, 3.0];
        var execution = CreateLinearExecution(input);

        LevenbergMarquardt.TryMinimize(execution, initialParameters, targets, 100, out var result, out _, TestContext.Current.CancellationToken).ShouldBeTrue();
        execution.Dispose();
        initialParameters.AsSpan().Fill(double.NaN);
        targets.AsSpan().Fill(double.NaN);
        input.AsSpan().Fill(double.NaN);

        result.ShouldNotBeNull();
        result.Parameters.ShouldNotBeSameAs(initialParameters);
        result.Parameters[0].ShouldBe(1.0, 1e-8);
        result.Parameters[1].ShouldBe(2.0, 1e-8);
        result.MeanSquaredError.ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void TryMinimizeRejectsInvalidArgumentShapesAndIterationCount()
    {
        double[] input = [-1.0, 0.0, 1.0];
        using var execution = CreateLinearExecution(input);

        Should.Throw<ArgumentException>(() => LevenbergMarquardt.TryMinimize(execution, [0.0], [0.0, 1.0, 2.0], 10, out _, out _));
        Should.Throw<ArgumentException>(() => LevenbergMarquardt.TryMinimize(execution, [0.0, 0.0], [0.0, 1.0], 10, out _, out _));
        Should.Throw<ArgumentOutOfRangeException>(() => LevenbergMarquardt.TryMinimize(execution, [0.0, 0.0], [0.0, 1.0, 2.0], -1, out _, out _));
    }

    [Fact]
    public void TryMinimizeRejectsParameterFreeAndDisposedExecutions()
    {
        var builder = new Builder();
        using var parameterFreeExecution = builder.Build(builder.Constant(1.0)).CreateExecution();
        Should.Throw<ArgumentException>(() => LevenbergMarquardt.TryMinimize(parameterFreeExecution, [], [1.0], 10, out _, out _));

        var disposedExecution = CreateLinearExecution([-1.0, 0.0, 1.0]);
        disposedExecution.Dispose();
        Should.Throw<ObjectDisposedException>(() => LevenbergMarquardt.TryMinimize(disposedExecution, [0.0, 0.0], [0.0, 1.0, 2.0], 10, out _, out _));
    }

    [Fact]
    public void TryMinimizePropagatesCancellation()
    {
        using var execution = CreateLinearExecution([-1.0, 0.0, 1.0]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Should.Throw<OperationCanceledException>(() => LevenbergMarquardt.TryMinimize(execution, [0.0, 0.0], [0.0, 1.0, 2.0], 10, out _, out _, cancellation.Token));
    }

    [Fact]
    public void TryMinimizeReturnsTheCurrentPointWhenMaximumIterationsIsZero()
    {
        double[] input = [-2.0, -1.0, 0.0, 1.0, 2.0];
        double[] targets = [-4.0, -1.0, 2.0, 5.0, 8.0];
        using var execution = CreateLinearExecution(input);

        var success = LevenbergMarquardt.TryMinimize(execution, [0.0, 0.0], targets, 0, out var result, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        failure.ShouldBeNull();
        result.ShouldNotBeNull();
        result.Parameters.ShouldBe([0.0, 0.0]);
        result.MeanSquaredError.ShouldBe(22.0, 1e-12);
    }

    [Fact]
    public void TryMinimizePreservesNonFiniteSolverResult()
    {
        var builder = new Builder();
        var parameter = builder.Parameter();
        var program = builder.Build(builder.Divide(parameter, builder.Constant(0.0)));
        using var execution = program.CreateExecution();

        var success = LevenbergMarquardt.TryMinimize(execution, [0.0], [0.0], 10, out var result, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        failure.ShouldBeNull();
        result.ShouldNotBeNull();
        result.Parameters.ShouldBe([0.0]);
        double.IsNaN(result.MeanSquaredError).ShouldBeTrue();
    }

    [Fact]
    public void TryMinimizeConvertsSolverExceptionToFailure()
    {
        var builder = new Builder();
        var parameter = builder.Parameter();
        var program = builder.Build(builder.Divide(builder.Constant(1.0), parameter));
        using var execution = program.CreateExecution();

        var success = LevenbergMarquardt.TryMinimize(execution, [0.0], [1.0], 10, out var result, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        result.ShouldBeNull();
        failure.ShouldNotBeNull();
        failure.Message.ShouldNotBeNullOrWhiteSpace();
    }

    private static DifferentiationExecution CreateLinearExecution(double[] input)
    {
        var builder = new Builder();
        var x = builder.Input();
        var intercept = builder.Parameter();
        var slope = builder.Parameter();
        return builder.Build(builder.Add(intercept, builder.Multiply(slope, x))).CreateExecution([input], input.Length);
    }
}
