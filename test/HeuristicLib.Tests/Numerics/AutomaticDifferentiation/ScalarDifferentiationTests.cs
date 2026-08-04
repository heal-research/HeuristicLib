using HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Tests.Numerics.AutomaticDifferentiation;

public sealed class ScalarDifferentiationTests
{
    [Fact]
    public void EvaluateWithJacobianDifferentiatesEveryBinaryOperation()
    {
        AssertResult(Differentiate(builder => builder.Add(builder.Parameter(), builder.Parameter()), 2.0, 3.0), 5.0, [1.0, 1.0]);
        AssertResult(Differentiate(builder => builder.Subtract(builder.Parameter(), builder.Parameter()), 2.0, 3.0), -1.0, [1.0, -1.0]);
        AssertResult(Differentiate(builder => builder.Multiply(builder.Parameter(), builder.Parameter()), 2.0, 3.0), 6.0, [3.0, 2.0]);
        AssertResult(Differentiate(builder => builder.Divide(builder.Parameter(), builder.Parameter()), 2.0, 3.0), 2.0 / 3.0, [1.0 / 3.0, -2.0 / 9.0]);
    }

    [Fact]
    public void EvaluateWithJacobianDifferentiatesEveryUnaryOperation()
    {
        var exp = Math.Exp(2.0);
        var tan = Math.Tan(2.0);
        var tanh = Math.Tanh(2.0);

        AssertResult(Differentiate(builder => builder.Negate(builder.Parameter()), 2.0), -2.0, [-1.0]);
        AssertResult(Differentiate(builder => builder.Exp(builder.Parameter()), 2.0), exp, [exp]);
        AssertResult(Differentiate(builder => builder.Log(builder.Parameter()), 2.0), Math.Log(2.0), [0.5]);
        AssertResult(Differentiate(builder => builder.Sin(builder.Parameter()), 2.0), Math.Sin(2.0), [Math.Cos(2.0)]);
        AssertResult(Differentiate(builder => builder.Cos(builder.Parameter()), 2.0), Math.Cos(2.0), [-Math.Sin(2.0)]);
        AssertResult(Differentiate(builder => builder.Tan(builder.Parameter()), 2.0), tan, [1.0 + (tan * tan)]);
        AssertResult(Differentiate(builder => builder.Tanh(builder.Parameter()), 2.0), tanh, [1.0 - (tanh * tanh)]);
    }

    [Fact]
    public void EvaluateWithJacobianUsesBoundInputPrimals()
    {
        var builder = new Builder();
        var input = builder.Input();
        var parameter = builder.Parameter();
        var root = builder.Add(builder.Multiply(parameter, input), builder.Sin(parameter));
        var program = builder.Build(root);
        using var execution = program.CreateExecution([new double[] { 3.0 }], 1);
        var output = new double[1];
        var jacobian = new double[1];

        execution.EvaluateWithJacobian([2.0], output, jacobian);

        output[0].ShouldBe(6.0 + Math.Sin(2.0), 1e-12);
        jacobian[0].ShouldBe(3.0 + Math.Cos(2.0), 1e-12);
    }

    [Fact]
    public void EvaluateWithJacobianAccumulatesSharedPaths()
    {
        var result = Differentiate(builder =>
        {
            var parameter = builder.Parameter();
            var square = builder.Multiply(parameter, parameter);
            return builder.Add(square, square);
        }, 3.0);

        AssertResult(result, 18.0, [12.0]);
    }

    [Fact]
    public void EvaluateWithJacobianExtractsParametersInCreationOrder()
    {
        var result = Differentiate(builder =>
        {
            var first = builder.Parameter();
            var second = builder.Parameter();
            return builder.Add(builder.Multiply(first, second), builder.Sin(first));
        }, 2.0, 3.0);

        AssertResult(result, 6.0 + Math.Sin(2.0), [3.0 + Math.Cos(2.0), 2.0]);
    }

    [Fact]
    public void EvaluateWithJacobianHandlesParameterAndParameterFreeRoots()
    {
        AssertResult(Differentiate(builder => builder.Parameter(), 2.0), 2.0, [1.0]);
        AssertResult(Differentiate(builder => builder.Constant(4.0)), 4.0, []);
    }

    [Fact]
    public void EvaluateWithJacobianClearsAdjointsBetweenEvaluations()
    {
        var builder = new Builder();
        var parameter = builder.Parameter();
        var program = builder.Build(builder.Multiply(parameter, parameter));
        using var execution = program.CreateExecution();
        var output = new double[1];
        var jacobian = new double[1];

        execution.EvaluateWithJacobian([2.0], output, jacobian);
        AssertResult((output[0], jacobian), 4.0, [4.0]);

        execution.EvaluateWithJacobian([3.0], output, jacobian);
        AssertResult((output[0], jacobian), 9.0, [6.0]);
    }

    [Fact]
    public void EvaluateWithJacobianValidatesJacobianShape()
    {
        var builder = new Builder();
        var input = builder.Input();
        var parameter = builder.Parameter();
        var program = builder.Build(builder.Multiply(parameter, input));
        using var scalarExecution = program.CreateExecution([new double[] { 2.0 }], 1);
        using var batchedExecution = program.CreateExecution([new double[] { 2.0, 3.0 }], 2);

        Should.Throw<ArgumentException>(() => scalarExecution.EvaluateWithJacobian([1.0], new double[1], [])).ParamName.ShouldBe("jacobian");
        Should.Throw<ArgumentException>(() => batchedExecution.EvaluateWithJacobian([1.0], new double[2], new double[1])).ParamName.ShouldBe("jacobian");
        Should.Throw<ArgumentException>(() => batchedExecution.EvaluateWithJacobian([1.0], new double[2], new double[3])).ParamName.ShouldBe("jacobian");
    }

    [Fact]
    public void EvaluateWithJacobianUsesOrdinaryIeee754Propagation()
    {
        AssertResult(Differentiate(builder => builder.Add(builder.Parameter(), builder.Parameter()), double.PositiveInfinity, double.NegativeInfinity), double.NaN, [1.0, 1.0]);
        AssertResult(Differentiate(builder => builder.Subtract(builder.Parameter(), builder.Parameter()), double.PositiveInfinity, double.PositiveInfinity), double.NaN, [1.0, -1.0]);
        AssertResult(Differentiate(builder => builder.Multiply(builder.Parameter(), builder.Parameter()), double.PositiveInfinity, 0.0), double.NaN, [0.0, double.PositiveInfinity]);
        AssertResult(Differentiate(builder => builder.Divide(builder.Parameter(), builder.Parameter()), 1.0, 0.0), double.PositiveInfinity, [double.PositiveInfinity, double.NegativeInfinity]);
        AssertResult(Differentiate(builder => builder.Negate(builder.Parameter()), double.PositiveInfinity), double.NegativeInfinity, [-1.0]);
        AssertResult(Differentiate(builder => builder.Exp(builder.Parameter()), 1_000.0), double.PositiveInfinity, [double.PositiveInfinity]);
        AssertResult(Differentiate(builder => builder.Log(builder.Parameter()), 0.0), double.NegativeInfinity, [double.PositiveInfinity]);
        AssertResult(Differentiate(builder => builder.Sin(builder.Parameter()), double.PositiveInfinity), double.NaN, [double.NaN]);
        AssertResult(Differentiate(builder => builder.Cos(builder.Parameter()), double.PositiveInfinity), double.NaN, [double.NaN]);
        AssertResult(Differentiate(builder => builder.Tan(builder.Parameter()), double.PositiveInfinity), double.NaN, [double.NaN]);
        AssertResult(Differentiate(builder => builder.Tanh(builder.Parameter()), double.PositiveInfinity), 1.0, [0.0]);
    }

    private static (double Value, double[] Jacobian) Differentiate(Func<Builder, Value> build, params double[] parameters)
    {
        var builder = new Builder();
        var program = builder.Build(build(builder));
        using var execution = program.CreateExecution();
        var output = new double[1];
        var jacobian = new double[parameters.Length];
        execution.EvaluateWithJacobian(parameters, output, jacobian);
        return (output[0], jacobian);
    }

    private static void AssertResult((double Value, double[] Jacobian) actual, double expectedValue, double[] expectedJacobian)
    {
        AssertValue(actual.Value, expectedValue);
        actual.Jacobian.Length.ShouldBe(expectedJacobian.Length);
        for (var parameterIndex = 0; parameterIndex < actual.Jacobian.Length; parameterIndex++)
            AssertValue(actual.Jacobian[parameterIndex], expectedJacobian[parameterIndex]);
    }

    private static void AssertValue(double actual, double expected)
    {
        if (double.IsNaN(expected))
        {
            double.IsNaN(actual).ShouldBeTrue();
            return;
        }

        if (double.IsInfinity(expected))
        {
            actual.ShouldBe(expected);
            return;
        }

        actual.ShouldBe(expected, 1e-12);
    }
}
