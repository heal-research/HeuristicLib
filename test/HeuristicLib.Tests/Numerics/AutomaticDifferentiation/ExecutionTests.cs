using HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Tests.Numerics.AutomaticDifferentiation;

public sealed class ExecutionTests
{
    [Fact]
    public void EvaluateSupportsEveryScalarOperation()
    {
        Evaluate(builder => builder.Constant(4.0)).ShouldBe(4.0);
        Evaluate(builder => builder.Parameter(), 2.0).ShouldBe(2.0);
        Evaluate(builder => builder.Add(builder.Parameter(), builder.Parameter()), 2.0, 3.0).ShouldBe(5.0);
        Evaluate(builder => builder.Subtract(builder.Parameter(), builder.Parameter()), 2.0, 3.0).ShouldBe(-1.0);
        Evaluate(builder => builder.Multiply(builder.Parameter(), builder.Parameter()), 2.0, 3.0).ShouldBe(6.0);
        Evaluate(builder => builder.Divide(builder.Parameter(), builder.Parameter()), 2.0, 3.0).ShouldBe(2.0 / 3.0);
        Evaluate(builder => builder.Negate(builder.Parameter()), 2.0).ShouldBe(-2.0);
        Evaluate(builder => builder.Exp(builder.Parameter()), 2.0).ShouldBe(Math.Exp(2.0));
        Evaluate(builder => builder.Log(builder.Parameter()), 2.0).ShouldBe(Math.Log(2.0));
        Evaluate(builder => builder.Sin(builder.Parameter()), 2.0).ShouldBe(Math.Sin(2.0));
        Evaluate(builder => builder.Cos(builder.Parameter()), 2.0).ShouldBe(Math.Cos(2.0));
        Evaluate(builder => builder.Tan(builder.Parameter()), 2.0).ShouldBe(Math.Tan(2.0));
        Evaluate(builder => builder.Tanh(builder.Parameter()), 2.0).ShouldBe(Math.Tanh(2.0));
    }

    [Fact]
    public void EvaluateRecomputesScalarPrimalsForEveryParameterVector()
    {
        var builder = new Builder();
        var parameter = builder.Parameter();
        var root = builder.Add(builder.Multiply(parameter, parameter), builder.Constant(1.0));
        var program = builder.Build(root);
        using var execution = program.CreateExecution();
        var output = new double[1];

        execution.Evaluate([2.0], output);
        output[0].ShouldBe(5.0);

        execution.Evaluate([3.0], output);
        output[0].ShouldBe(10.0);
    }

    [Fact]
    public void EvaluateUsesOrdinaryIeee754Propagation()
    {
        Evaluate(builder => builder.Divide(builder.Parameter(), builder.Constant(0.0)), 1.0).ShouldBe(double.PositiveInfinity);
        Evaluate(builder => builder.Log(builder.Parameter()), -1.0).ShouldBe(double.NaN);
    }

    [Fact]
    public void EvaluateRejectsMismatchedBuffers()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Parameter());
        using var execution = program.CreateExecution();
        var output = new double[1];

        Should.Throw<ArgumentException>(() => execution.Evaluate([], output)).ParamName.ShouldBe("parameters");
        Should.Throw<ArgumentException>(() => execution.Evaluate([1.0], [])).ParamName.ShouldBe("outputs");
        Should.Throw<ArgumentException>(() => execution.Evaluate([1.0], new double[2])).ParamName.ShouldBe("outputs");
    }

    [Fact]
    public void CreateExecutionRejectsInputDependentPrograms()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Input());

        Should.Throw<InvalidOperationException>(() => program.CreateExecution());
    }

    [Fact]
    public void DisposeIsIdempotentAndPreventsFurtherEvaluation()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Constant(1.0));
        var execution = program.CreateExecution();

        execution.Dispose();
        execution.Dispose();

        Should.Throw<ObjectDisposedException>(() => execution.Evaluate([], new double[1]));
    }

    [Fact]
    public void EvaluateRejectsOutputOverlappingParameters()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Parameter());
        using var execution = program.CreateExecution();
        double[] storage = [2.0];

        Should.Throw<ArgumentException>(() => execution.Evaluate(storage, storage)).ParamName.ShouldBe("outputs");
    }

    [Fact]
    public void EvaluateRejectsOutputOverlappingBoundInput()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Input());
        double[] storage = [1.0, 2.0, 3.0, 4.0];
        using var execution = program.CreateExecution([storage.AsMemory(0, 3)], 3);

        Should.Throw<ArgumentException>(() => execution.Evaluate([], storage.AsSpan(1, 3))).ParamName.ShouldBe("outputs");
    }

    [Fact]
    public void EvaluateWithJacobianRejectsJacobianOverlappingParameters()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Parameter());
        using var execution = program.CreateExecution();
        double[] parametersAndJacobian = [2.0];

        Should.Throw<ArgumentException>(() => execution.EvaluateWithJacobian(parametersAndJacobian, new double[1], parametersAndJacobian)).ParamName.ShouldBe("jacobian");
    }

    [Fact]
    public void EvaluateWithJacobianRejectsJacobianOverlappingOutputs()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Parameter());
        using var execution = program.CreateExecution();
        double[] outputsAndJacobian = [0.0];

        Should.Throw<ArgumentException>(() => execution.EvaluateWithJacobian([2.0], outputsAndJacobian, outputsAndJacobian)).ParamName.ShouldBe("jacobian");
    }

    [Fact]
    public void EvaluateWithJacobianRejectsJacobianOverlappingBoundInput()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Multiply(builder.Parameter(), builder.Input()));
        double[] inputAndJacobian = [1.0, 2.0, 3.0];
        using var execution = program.CreateExecution([inputAndJacobian], inputAndJacobian.Length);

        Should.Throw<ArgumentException>(() => execution.EvaluateWithJacobian([2.0], new double[3], inputAndJacobian)).ParamName.ShouldBe("jacobian");
    }

    [Fact]
    public void ReadOnlyInputColumnsMayOverlap()
    {
        var builder = new Builder();
        var program = builder.Build(builder.Add(builder.Input(), builder.Input()));
        double[] input = [1.0, 2.0, 3.0];
        using var execution = program.CreateExecution([input, input], input.Length);
        var outputs = new double[input.Length];

        execution.Evaluate([], outputs);

        outputs.ShouldBe([2.0, 4.0, 6.0]);
    }

    [Fact]
    public void DisjointCallerBuffersWithinOneArrayAreAllowed()
    {
        var builder = new Builder();
        var parameter = builder.Parameter();
        var program = builder.Build(builder.Multiply(parameter, parameter));
        using var execution = program.CreateExecution();
        double[] storage = [2.0, 0.0, 0.0];

        execution.EvaluateWithJacobian(storage.AsSpan(0, 1), storage.AsSpan(1, 1), storage.AsSpan(2, 1));

        storage.ShouldBe([2.0, 4.0, 4.0]);
    }

    private static double Evaluate(Func<Builder, Value> build, params double[] parameters)
    {
        var builder = new Builder();
        var program = builder.Build(build(builder));
        using var execution = program.CreateExecution();
        var output = new double[1];
        execution.Evaluate(parameters, output);
        return output[0];
    }
}
