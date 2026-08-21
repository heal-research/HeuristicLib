using HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Tests.Numerics.AutomaticDifferentiation;

public sealed class BatchedExecutionTests
{
    private static readonly Operation[] BinaryOperations = [Operation.Add, Operation.Subtract, Operation.Multiply, Operation.Divide];
    private static readonly Operation[] UnaryOperations = [Operation.Negate, Operation.Exp, Operation.Log, Operation.Sin, Operation.Cos, Operation.Tan, Operation.Tanh];

    [Fact]
    public void EvaluateSupportsEveryBinaryOperationAndOperandShape()
    {
        double[] left = [1.0, 2.0, 3.0, 4.0];
        double[] right = [4.0, 5.0, 6.0, 8.0];

        foreach (var operation in BinaryOperations)
        {
            var expectedVectorVector = left.Zip(right, (x, y) => Apply(operation, x, y)).ToArray();
            var vectorVector = Evaluate(builder => Apply(builder, operation, builder.Input(), builder.Input()), [left, right], []);
            AssertValues(vectorVector, expectedVectorVector);

            var expectedVectorScalar = left.Select(x => Apply(operation, x, 2.0)).ToArray();
            var vectorScalar = Evaluate(builder => Apply(builder, operation, builder.Input(), builder.Parameter()), [left], [2.0]);
            AssertValues(vectorScalar, expectedVectorScalar);

            var expectedScalarVector = right.Select(x => Apply(operation, 2.0, x)).ToArray();
            var scalarVector = Evaluate(builder => Apply(builder, operation, builder.Parameter(), builder.Input()), [right], [2.0]);
            AssertValues(scalarVector, expectedScalarVector);
        }
    }

    [Fact]
    public void EvaluateSupportsEveryUnaryOperation()
    {
        double[] input = [0.25, 0.5, 1.25, 2.0];

        foreach (var operation in UnaryOperations)
        {
            var actual = Evaluate(builder => Apply(builder, operation, builder.Input()), [input], []);
            AssertValues(actual, input.Select(x => Apply(operation, x)).ToArray());
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(257)]
    [InlineData(513)]
    public void EvaluateProcessesCompleteAndPartialBatches(int rowCount)
    {
        var input = Enumerable.Range(0, rowCount).Select(i => 0.01 * (i + 1)).ToArray();
        var actual = Evaluate(builder =>
        {
            var x = builder.Input();
            var p = builder.Parameter();
            return builder.Add(builder.Multiply(p, x), builder.Sin(x));
        }, [input], [2.5], batchCapacity: 256);

        AssertValues(actual, input.Select(x => (2.5 * x) + Math.Sin(x)).ToArray());
    }

    [Fact]
    public void EvaluateMaterializesInputRootWithoutIntermediateStorage()
    {
        double[] input = [1.0, 2.0, 3.0, 4.0, 5.0];
        var actual = Evaluate(builder => builder.Input(), [input], [], batchCapacity: 2);

        actual.ShouldBe(input);
    }

    [Fact]
    public void ExecutionCopiesInputDescriptorsButNotNumericalData()
    {
        double[] source = [1.0, 2.0, 3.0];
        double[] replacement = [4.0, 5.0, 6.0];
        ReadOnlyMemory<double>[] columns = [source];
        var builder = new Builder();
        var program = builder.Build(builder.Input());
        using var execution = program.CreateExecution(columns, source.Length);

        columns[0] = replacement;
        source[0] = 7.0;
        var outputs = new double[source.Length];
        execution.Evaluate([], outputs);

        outputs.ShouldBe([7.0, 2.0, 3.0]);
    }

    [Fact]
    public void EvaluateRecomputesEveryBatchForNewParameters()
    {
        double[] input = [1.0, 2.0, 3.0, 4.0, 5.0];
        var builder = new Builder();
        var root = builder.Multiply(builder.Parameter(), builder.Input());
        var program = builder.Build(root);
        using var execution = program.CreateExecution([input], input.Length, batchCapacity: 2);
        var outputs = new double[input.Length];

        execution.Evaluate([2.0], outputs);
        outputs.ShouldBe([2.0, 4.0, 6.0, 8.0, 10.0]);

        execution.Evaluate([3.0], outputs);
        outputs.ShouldBe([3.0, 6.0, 9.0, 12.0, 15.0]);
    }

    [Fact]
    public void CreateExecutionValidatesInputBindingsAndCapacity()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Input());
        ReadOnlyMemory<double>[] oneColumn = { new double[3] };

        Should.Throw<ArgumentException>(() => program.CreateExecution([], 3)).ParamName.ShouldBe("inputColumns");
        Should.Throw<ArgumentOutOfRangeException>(() => program.CreateExecution(oneColumn, 0)).ParamName.ShouldBe("rowCount");
        Should.Throw<ArgumentOutOfRangeException>(() => program.CreateExecution(oneColumn, 3, 0)).ParamName.ShouldBe("batchCapacity");
        Should.Throw<ArgumentException>(() => program.CreateExecution([new double[2]], 3)).ParamName.ShouldBe("inputColumns");
    }

    [Fact]
    public void InputFreeExecutionRepeatsItsScalarResultForEveryRow()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Add(builder.Parameter(), builder.Constant(1.0)));
        using var execution = program.CreateExecution([], 5, batchCapacity: 2);
        var outputs = new double[5];

        execution.Evaluate([2.0], outputs);

        outputs.ShouldBe([3.0, 3.0, 3.0, 3.0, 3.0]);
    }

    [Fact]
    public void EvaluateRequiresOneOutputPerRow()
    {
        double[] input = [1.0, 2.0, 3.0];
        var builder = new Builder();
        var program = builder.Build(builder.Input());
        using var execution = program.CreateExecution([input], input.Length);

        Should.Throw<ArgumentException>(() => execution.Evaluate([], new double[2])).ParamName.ShouldBe("outputs");
        Should.Throw<ArgumentException>(() => execution.Evaluate([], new double[4])).ParamName.ShouldBe("outputs");
    }

    private static double[] Evaluate(Func<Builder, Value> build, ReadOnlyMemory<double>[] inputs, double[] parameters, int batchCapacity = 256)
    {
        var builder = new Builder();
        var program = builder.Build(build(builder));
        var rowCount = inputs[0].Length;
        using var execution = program.CreateExecution(inputs, rowCount, batchCapacity);
        var outputs = new double[rowCount];
        execution.Evaluate(parameters, outputs);
        return outputs;
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

    private static double Apply(Operation operation, double left, double right) => operation switch
    {
        Operation.Add => left + right,
        Operation.Subtract => left - right,
        Operation.Multiply => left * right,
        Operation.Divide => left / right,
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };

    private static double Apply(Operation operation, double operand) => operation switch
    {
        Operation.Negate => -operand,
        Operation.Exp => Math.Exp(operand),
        Operation.Log => Math.Log(operand),
        Operation.Sin => Math.Sin(operand),
        Operation.Cos => Math.Cos(operand),
        Operation.Tan => Math.Tan(operand),
        Operation.Tanh => Math.Tanh(operand),
        _ => throw new ArgumentOutOfRangeException(nameof(operation)),
    };

    private static void AssertValues(double[] actual, double[] expected)
    {
        actual.Length.ShouldBe(expected.Length);
        for (var i = 0; i < actual.Length; i++)
        {
            actual[i].ShouldBe(expected[i], 1e-12);
        }
    }
}
