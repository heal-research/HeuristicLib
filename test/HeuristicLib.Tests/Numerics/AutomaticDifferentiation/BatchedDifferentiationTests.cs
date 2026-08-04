using System.Numerics;
using HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;
using DifferentiationProgram = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation.Program;

namespace HEAL.HeuristicLib.Tests.Numerics.AutomaticDifferentiation;

public sealed class BatchedDifferentiationTests
{
    public static TheoryData<int> BinaryOperations => new((int)Operation.Add, (int)Operation.Subtract, (int)Operation.Multiply, (int)Operation.Divide);
    public static TheoryData<int> UnaryOperations => new((int)Operation.Negate, (int)Operation.Exp, (int)Operation.Log, (int)Operation.Sin, (int)Operation.Cos, (int)Operation.Tan, (int)Operation.Tanh);

    [Theory]
    [InlineData(1)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(513)]
    public void EvaluateWithJacobianProcessesCompleteAndPartialBatches(int rowCount)
    {
        var input = Enumerable.Range(0, rowCount).Select(i => 0.01 * (i + 1)).ToArray();
        var builder = new Builder();
        var x = builder.Input();
        var first = builder.Parameter();
        var second = builder.Parameter();
        var root = builder.Add(builder.Multiply(first, x), builder.Sin(builder.Multiply(second, x)));
        var program = builder.Build(root);
        using var execution = program.CreateExecution([input], rowCount, batchCapacity: 256);
        var outputs = new double[rowCount];
        var jacobian = new double[2 * rowCount];

        execution.EvaluateWithJacobian([2.0, 0.75], outputs, jacobian);

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var inputValue = input[rowIndex];
            outputs[rowIndex].ShouldBe((2.0 * inputValue) + Math.Sin(0.75 * inputValue), 1e-12);
            jacobian[rowIndex].ShouldBe(inputValue, 1e-12);
            jacobian[rowCount + rowIndex].ShouldBe(Math.Cos(0.75 * inputValue) * inputValue, 1e-12);
        }
    }

    [Theory]
    [MemberData(nameof(BinaryOperations))]
    public void EvaluateWithJacobianDifferentiatesVectorVectorBinaryOperations(int operationValue)
    {
        var operation = (Operation)operationValue;
        AssertJacobianMatchesFiniteDifferences(builder =>
        {
            var x = builder.Input();
            var y = builder.Input();
            var first = builder.Parameter();
            var second = builder.Parameter();
            return Apply(builder, operation, builder.Multiply(first, x), builder.Multiply(second, y));
        }, [new double[] { 0.2, 0.4, 0.6, 0.8, 1.0 }, new double[] { 1.1, 1.2, 1.3, 1.4, 1.5 }], [0.7, 1.3]);
    }

    [Theory]
    [MemberData(nameof(BinaryOperations))]
    public void EvaluateWithJacobianDifferentiatesVectorScalarBinaryOperations(int operationValue)
    {
        var operation = (Operation)operationValue;
        AssertJacobianMatchesFiniteDifferences(builder =>
        {
            var x = builder.Input();
            var first = builder.Parameter();
            var second = builder.Parameter();
            return Apply(builder, operation, builder.Multiply(first, x), second);
        }, [new double[] { 0.2, 0.4, 0.6, 0.8, 1.0 }], [0.7, 1.3]);
    }

    [Theory]
    [MemberData(nameof(BinaryOperations))]
    public void EvaluateWithJacobianDifferentiatesScalarVectorBinaryOperations(int operationValue)
    {
        var operation = (Operation)operationValue;
        AssertJacobianMatchesFiniteDifferences(builder =>
        {
            var x = builder.Input();
            var first = builder.Parameter();
            var second = builder.Parameter();
            return Apply(builder, operation, first, builder.Multiply(second, x));
        }, [new double[] { 0.2, 0.4, 0.6, 0.8, 1.0 }], [0.7, 1.3]);
    }

    [Theory]
    [MemberData(nameof(UnaryOperations))]
    public void EvaluateWithJacobianDifferentiatesUnaryOperations(int operationValue)
    {
        var operation = (Operation)operationValue;
        AssertJacobianMatchesFiniteDifferences(builder =>
        {
            var x = builder.Input();
            var parameter = builder.Parameter();
            return Apply(builder, operation, builder.Add(builder.Multiply(parameter, x), builder.Constant(1.0)));
        }, [new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 }], [0.7]);
    }

    [Fact]
    public void BatchedJacobianMatchesIndependentSingleRowExecutions()
    {
        double[] input = [0.2, 0.4, 0.6, 0.8, 1.0];
        double[] parameters = [0.7, 1.3];
        var builder = new Builder();
        var x = builder.Input();
        var first = builder.Parameter();
        var second = builder.Parameter();
        var numerator = builder.Sin(builder.Multiply(first, x));
        var denominator = builder.Add(builder.Exp(second), builder.Multiply(second, x));
        var program = builder.Build(builder.Divide(numerator, denominator));
        var outputs = new double[input.Length];
        var jacobian = new double[parameters.Length * input.Length];

        using (var execution = program.CreateExecution([input], input.Length, batchCapacity: 2))
        {
            execution.EvaluateWithJacobian(parameters, outputs, jacobian);
        }

        for (var rowIndex = 0; rowIndex < input.Length; rowIndex++)
        {
            using var scalarExecution = program.CreateExecution([input.AsMemory(rowIndex, 1)], 1);
            var scalarOutput = new double[1];
            var scalarJacobian = new double[parameters.Length];
            scalarExecution.EvaluateWithJacobian(parameters, scalarOutput, scalarJacobian);
            outputs[rowIndex].ShouldBe(scalarOutput[0], 1e-12);
            for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                jacobian[(parameterIndex * input.Length) + rowIndex].ShouldBe(scalarJacobian[parameterIndex], 1e-12);
            }
        }
    }

    [Fact]
    public void EvaluateWithJacobianAccumulatesSharedPathsInEveryBatch()
    {
        double[] input = [1.0, 2.0, 3.0, 4.0, 5.0];
        var builder = new Builder();
        var product = builder.Multiply(builder.Parameter(), builder.Input());
        var program = builder.Build(builder.Add(product, product));
        using var execution = program.CreateExecution([input], input.Length, batchCapacity: 2);
        var outputs = new double[input.Length];
        var jacobian = new double[input.Length];

        execution.EvaluateWithJacobian([3.0], outputs, jacobian);

        outputs.ShouldBe([6.0, 12.0, 18.0, 24.0, 30.0]);
        jacobian.ShouldBe([2.0, 4.0, 6.0, 8.0, 10.0]);
    }

    [Fact]
    public void EvaluateWithJacobianClearsAdjointsBetweenBatchedEvaluations()
    {
        double[] input = [1.0, 2.0, 3.0, 4.0, 5.0];
        var builder = new Builder();
        var parameter = builder.Parameter();
        var program = builder.Build(builder.Multiply(builder.Multiply(parameter, parameter), builder.Input()));
        using var execution = program.CreateExecution([input], input.Length, batchCapacity: 2);
        var outputs = new double[input.Length];
        var jacobian = new double[input.Length];

        execution.EvaluateWithJacobian([2.0], outputs, jacobian);
        jacobian.ShouldBe([4.0, 8.0, 12.0, 16.0, 20.0]);

        execution.EvaluateWithJacobian([3.0], outputs, jacobian);
        jacobian.ShouldBe([6.0, 12.0, 18.0, 24.0, 30.0]);
    }

    [Fact]
    public void VectorizedReverseRulesMatchScalarTailsAtHardwareWidthBoundaries()
    {
        var program = CreateProgramCoveringVectorizedReverseRules();
        double[] parameters = [0.4, 0.7, 1.3];
        int[] rowCounts = [Math.Max(1, Vector<double>.Count - 1), Vector<double>.Count, Vector<double>.Count + 1, (2 * Vector<double>.Count) + 1];

        foreach (var rowCount in rowCounts)
        {
            var input = Enumerable.Range(0, rowCount).Select(rowIndex => 0.1 + (0.03 * rowIndex)).ToArray();
            var outputs = new double[rowCount];
            var jacobian = new double[parameters.Length * rowCount];
            using (var execution = program.CreateExecution([input], rowCount, batchCapacity: rowCount))
                execution.EvaluateWithJacobian(parameters, outputs, jacobian);

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                using var scalarExecution = program.CreateExecution([input.AsMemory(rowIndex, 1)], 1);
                var scalarOutput = new double[1];
                var scalarJacobian = new double[parameters.Length];
                scalarExecution.EvaluateWithJacobian(parameters, scalarOutput, scalarJacobian);
                outputs[rowIndex].ShouldBe(scalarOutput[0], 1e-11);
                for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                    jacobian[(parameterIndex * rowCount) + rowIndex].ShouldBe(scalarJacobian[parameterIndex], 1e-11);
            }
        }
    }

    [Fact]
    public void VectorizedReverseRulesPropagateNonFiniteValues()
    {
        var rowCount = Vector<double>.Count + 1;
        var input = Enumerable.Range(0, rowCount).Select(rowIndex => 0.25 + rowIndex).ToArray();
        input[0] = double.PositiveInfinity;
        input[1] = double.NaN;
        input[^1] = double.NegativeInfinity;

        AssertBatchedUnaryNonFinitePropagation(input, Operation.Sin, Math.Sin, Math.Cos);
        AssertBatchedUnaryNonFinitePropagation(input, Operation.Cos, Math.Cos, value => -Math.Sin(value));
        AssertBatchedUnaryNonFinitePropagation(input, Operation.Tan, Math.Tan, value => 1.0 + (Math.Tan(value) * Math.Tan(value)));
        AssertBatchedUnaryNonFinitePropagation(input, Operation.Tanh, Math.Tanh, value => 1.0 - (Math.Tanh(value) * Math.Tanh(value)));
        AssertBatchedDivisionNonFinitePropagation(input);
    }

    [Fact]
    public void MixedForwardAndJacobianEvaluationsRetainNoStaleValues()
    {
        var program = CreateProgramCoveringVectorizedReverseRules();
        var rowCount = (2 * Vector<double>.Count) + 1;
        var input = Enumerable.Range(0, rowCount).Select(rowIndex => 0.1 + (0.02 * rowIndex)).ToArray();
        using var execution = program.CreateExecution([input], rowCount, batchCapacity: Vector<double>.Count + 1);
        var outputs = new double[rowCount];
        var jacobian = new double[3 * rowCount];

        execution.EvaluateWithJacobian([0.4, 0.7, 1.3], outputs, jacobian);
        execution.Evaluate([0.6, 0.9, 1.5], outputs);
        outputs.AsSpan().Fill(double.NaN);
        jacobian.AsSpan().Fill(double.NaN);
        execution.EvaluateWithJacobian([0.8, 1.1, 1.7], outputs, jacobian);

        using var referenceExecution = program.CreateExecution([input], rowCount, batchCapacity: Vector<double>.Count + 1);
        var expectedOutputs = new double[rowCount];
        var expectedJacobian = new double[3 * rowCount];
        referenceExecution.EvaluateWithJacobian([0.8, 1.1, 1.7], expectedOutputs, expectedJacobian);
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            outputs[rowIndex].ShouldBe(expectedOutputs[rowIndex], 1e-12);

        for (var jacobianIndex = 0; jacobianIndex < jacobian.Length; jacobianIndex++)
            jacobian[jacobianIndex].ShouldBe(expectedJacobian[jacobianIndex], 1e-12);
    }

    [Fact]
    public void ReusedPooledBuffersRetainNoValuesFromDisposedExecutions()
    {
        var builder = new Builder();
        var inputValue = builder.Input();
        var parameter = builder.Parameter();
        var program = builder.Build(builder.Multiply(builder.Multiply(parameter, parameter), inputValue));
        var rowCount = Vector<double>.Count + 1;
        var input = Enumerable.Range(1, rowCount).Select(value => (double)value).ToArray();
        var outputs = new double[rowCount];
        var jacobian = new double[rowCount];
        var parameters = new double[1];

        for (var iteration = 1; iteration <= 50; iteration++)
        {
            parameters[0] = iteration;
            outputs.AsSpan().Fill(double.NaN);
            jacobian.AsSpan().Fill(double.NaN);
            using var execution = program.CreateExecution([input], rowCount, batchCapacity: Vector<double>.Count);
            execution.EvaluateWithJacobian(parameters, outputs, jacobian);

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                outputs[rowIndex].ShouldBe(iteration * iteration * input[rowIndex], 1e-12);
                jacobian[rowIndex].ShouldBe(2.0 * iteration * input[rowIndex], 1e-12);
            }
        }
    }

    [Fact]
    public void ParameterFreeProgramAcceptsAnEmptyJacobian()
    {
        double[] input = [1.0, 2.0, 3.0];
        var builder = new Builder();
        var program = builder.Build(builder.Sin(builder.Input()));
        using var execution = program.CreateExecution([input], input.Length, batchCapacity: 2);
        var outputs = new double[input.Length];

        execution.EvaluateWithJacobian([], outputs, []);

        for (var i = 0; i < input.Length; i++)
        {
            outputs[i].ShouldBe(Math.Sin(input[i]), 1e-12);
        }
    }

    private static DifferentiationProgram CreateProgramCoveringVectorizedReverseRules()
    {
        var builder = new Builder();
        var input = builder.Input();
        var first = builder.Parameter();
        var second = builder.Parameter();
        var third = builder.Parameter();
        var firstProduct = builder.Multiply(first, input);
        var secondProduct = builder.Multiply(second, input);
        var shiftedInput = builder.Add(input, builder.Constant(1.0));
        var vectorDenominator = builder.Multiply(third, shiftedInput);
        var trigonometric = builder.Add(builder.Add(builder.Sin(firstProduct), builder.Cos(secondProduct)), builder.Add(builder.Tan(builder.Multiply(builder.Constant(0.1), firstProduct)), builder.Tanh(builder.Multiply(third, input))));
        var divisions = builder.Add(builder.Add(builder.Divide(second, shiftedInput), builder.Divide(firstProduct, third)), builder.Add(builder.Divide(second, vectorDenominator), builder.Divide(firstProduct, vectorDenominator)));
        return builder.Build(builder.Add(trigonometric, divisions));
    }

    private static void AssertBatchedUnaryNonFinitePropagation(double[] input, Operation operation, Func<double, double> function, Func<double, double> derivative)
    {
        var builder = new Builder();
        var parameter = builder.Parameter();
        var argument = builder.Multiply(parameter, builder.Input());
        var root = operation switch
        {
            Operation.Sin => builder.Sin(argument),
            Operation.Cos => builder.Cos(argument),
            Operation.Tan => builder.Tan(argument),
            Operation.Tanh => builder.Tanh(argument),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
        var program = builder.Build(root);
        using var execution = program.CreateExecution([input], input.Length, batchCapacity: input.Length);
        var outputs = new double[input.Length];
        var jacobian = new double[input.Length];

        execution.EvaluateWithJacobian([1.0], outputs, jacobian);

        for (var rowIndex = 0; rowIndex < input.Length; rowIndex++)
        {
            AssertValue(outputs[rowIndex], function(input[rowIndex]));
            AssertValue(jacobian[rowIndex], derivative(input[rowIndex]) * input[rowIndex]);
        }
    }

    private static void AssertBatchedDivisionNonFinitePropagation(double[] input)
    {
        var builder = new Builder();
        var inputValue = builder.Input();
        var parameter = builder.Parameter();
        var denominator = builder.Multiply(parameter, inputValue);
        var program = builder.Build(builder.Divide(inputValue, denominator));
        using var execution = program.CreateExecution([input], input.Length, batchCapacity: input.Length);
        var outputs = new double[input.Length];
        var jacobian = new double[input.Length];

        execution.EvaluateWithJacobian([0.0], outputs, jacobian);

        for (var rowIndex = 0; rowIndex < input.Length; rowIndex++)
        {
            var denominatorValue = 0.0 * input[rowIndex];
            AssertValue(outputs[rowIndex], input[rowIndex] / denominatorValue);
            AssertValue(jacobian[rowIndex], (-input[rowIndex] / (denominatorValue * denominatorValue)) * input[rowIndex]);
        }
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

    private static void AssertJacobianMatchesFiniteDifferences(Func<Builder, Value> build, ReadOnlyMemory<double>[] inputs, double[] parameters)
    {
        var builder = new Builder();
        var program = builder.Build(build(builder));
        var rowCount = inputs[0].Length;
        using var execution = program.CreateExecution(inputs, rowCount, batchCapacity: 3);
        var outputs = new double[rowCount];
        var jacobian = new double[parameters.Length * rowCount];
        execution.EvaluateWithJacobian(parameters, outputs, jacobian);

        const double step = 1e-6;
        var lowerParameters = parameters.ToArray();
        var upperParameters = parameters.ToArray();
        var lowerOutputs = new double[rowCount];
        var upperOutputs = new double[rowCount];
        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
        {
            lowerParameters[parameterIndex] -= step;
            upperParameters[parameterIndex] += step;
            execution.Evaluate(lowerParameters, lowerOutputs);
            execution.Evaluate(upperParameters, upperOutputs);

            for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var expected = (upperOutputs[rowIndex] - lowerOutputs[rowIndex]) / (2.0 * step);
                var tolerance = Math.Max(1e-8, Math.Abs(expected) * 1e-6);
                jacobian[(parameterIndex * rowCount) + rowIndex].ShouldBe(expected, tolerance);
            }

            lowerParameters[parameterIndex] = parameters[parameterIndex];
            upperParameters[parameterIndex] = parameters[parameterIndex];
        }
    }

    private static Value Apply(Builder builder, Operation operation, Value left, Value right) => operation switch
    {
        Operation.Add => builder.Add(left, right),
        Operation.Subtract => builder.Subtract(left, right),
        Operation.Multiply => builder.Multiply(left, right),
        Operation.Divide => builder.Divide(left, right),
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };

    private static Value Apply(Builder builder, Operation operation, Value operand) => operation switch
    {
        Operation.Negate => builder.Negate(operand),
        Operation.Exp => builder.Exp(operand),
        Operation.Log => builder.Log(operand),
        Operation.Sin => builder.Sin(operand),
        Operation.Cos => builder.Cos(operand),
        Operation.Tan => builder.Tan(operand),
        Operation.Tanh => builder.Tanh(operand),
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };
}
