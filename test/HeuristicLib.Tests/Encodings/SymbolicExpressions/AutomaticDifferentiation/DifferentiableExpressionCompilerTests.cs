using static HEAL.HeuristicLib.Encodings.SymbolicExpressions.ExpressionDraft;
using AD = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions.AutomaticDifferentiation;

public sealed class DifferentiableExpressionCompilerTests
{
    [Fact]
    public void Compile_LowersEveryInitiallySupportedOperation()
    {
        var x = Variable("x");
        var expression = (Negate(x) / FixedConstant(2.0) * Exp(x) - Log(x + FixedConstant(2.0)) + (Sin(x) + Cos(x)) + (Tan(x / FixedConstant(10.0)) + Tanh(x))).Build();
        double[] input = [-0.5, 0.0, 0.5];
        var data = new DataFrame([Series<double>.FromOwnedArray("x", input)]);

        var lowered = CompileSuccessfully(expression);
        using var execution = lowered.Program.CreateExecution([input], input.Length, batchCapacity: 2);
        var actual = new double[input.Length];
        execution.Evaluate([], actual);

        var expected = ExpressionInterpreter.Interpret(expression.Compile(optimize: false), data);
        actual.ShouldBe(expected, tolerance: 1e-12);
    }

    [Fact]
    public void Compile_InternsVariablesByOrdinalNameInFirstEmissionOrder()
    {
        var expression = (Variable("x1") + Variable("x0") + Variable("x1")).Build();

        var lowered = CompileSuccessfully(expression);

        lowered.Expression.ShouldBeSameAs(expression);
        lowered.VariableNames.ShouldBe(["x1", "x0"]);
        lowered.Program.InputCount.ShouldBe(2);
        lowered.Program.InstructionCount.ShouldBe(4);
    }

    [Fact]
    public void Compile_ExpandsMacroThroughSymbolEmitter()
    {
        var expression = Sigmoid(Variable("x")).Build();

        var lowered = CompileSuccessfully(expression);
        double[] input = [0.0, 1.0];
        using var execution = lowered.Program.CreateExecution([input], input.Length);
        var outputs = new double[input.Length];
        execution.Evaluate([], outputs);

        expression.Length.ShouldBe(2);
        outputs[0].ShouldBe(0.5, 1e-12);
        outputs[1].ShouldBe(1.0 / (1.0 + Math.Exp(-1.0)), 1e-12);
    }

    [Fact]
    public void Compile_ReusesAChildEmittedMoreThanOnceByAMacro()
    {
        var expression = Apply(new SquareChildSymbol(), FixedConstant(3.0)).Build();

        var lowered = CompileSuccessfully(expression);
        using var execution = lowered.Program.CreateExecution();
        var output = new double[1];
        execution.Evaluate([], output);

        lowered.Program.InstructionCount.ShouldBe(2);
        output[0].ShouldBe(9.0);
    }

    [Fact]
    public void Compile_MapsEvolvableConstantsToParametersAndPreservesFixedConstants()
    {
        var symbol = new EvolvableConstantSymbol();
        var expression = (FixedConstant(1.0) + Constant(2.0, symbol) * Variable("x")).Build();
        double[] input = [1.0, 2.0, 3.0];

        var lowered = CompileSuccessfully(expression);
        using var execution = lowered.Program.CreateExecution([input], input.Length, batchCapacity: 2);
        var outputs = new double[input.Length];
        var jacobian = new double[input.Length];
        execution.EvaluateWithJacobian([3.0], outputs, jacobian);

        lowered.ParameterCount.ShouldBe(1);
        lowered.Program.ParameterCount.ShouldBe(1);
        lowered.ParameterBindings.Length.ShouldBe(1);
        lowered.ParameterBindings[0].Symbol.ShouldBeSameAs(symbol);
        lowered.ParameterBindings[0].InitialValue.ShouldBe(2.0);
        outputs.ShouldBe([4.0, 7.0, 10.0], tolerance: 1e-12);
        jacobian.ShouldBe(input, tolerance: 1e-12);
    }

    [Fact]
    public void Compile_GivesReferenceSharedOccurrencesSeparateParameters()
    {
        var symbol = new EvolvableConstantSymbol();
        var shared = new NumericConstantExpressionNode(symbol, 2.0);
        var expression = new ExpressionTree(new BinaryExpressionNode(Symbols.Subtraction, shared, shared));

        var lowered = CompileSuccessfully(expression);
        using var execution = lowered.Program.CreateExecution();
        var output = new double[1];
        var jacobian = new double[2];
        execution.EvaluateWithJacobian([5.0, 3.0], output, jacobian);

        lowered.ParameterBindings.Length.ShouldBe(2);
        lowered.ParameterBindings[0].Point.Node.ShouldBeSameAs(shared);
        lowered.ParameterBindings[1].Point.Node.ShouldBeSameAs(shared);
        lowered.ParameterBindings[0].Point.ChildIndex.ShouldBe(0);
        lowered.ParameterBindings[1].Point.ChildIndex.ShouldBe(1);
        output[0].ShouldBe(2.0);
        jacobian.ShouldBe([1.0, -1.0], tolerance: 1e-12);
    }

    [Fact]
    public void Compile_ReusesOneParameterWhenMacroEmitsAChildMoreThanOnce()
    {
        var symbol = new EvolvableConstantSymbol();
        var expression = Apply(new SquareChildSymbol(), Constant(3.0, symbol)).Build();

        var lowered = CompileSuccessfully(expression);
        using var execution = lowered.Program.CreateExecution();
        var output = new double[1];
        var jacobian = new double[1];
        execution.EvaluateWithJacobian([4.0], output, jacobian);

        lowered.Program.ParameterCount.ShouldBe(1);
        lowered.ParameterBindings.Length.ShouldBe(1);
        lowered.Program.InstructionCount.ShouldBe(2);
        output[0].ShouldBe(16.0);
        jacobian[0].ShouldBe(8.0);
    }

    [Fact]
    public void Compile_OrdersParametersByFirstSemanticEmission()
    {
        var firstSymbol = new EvolvableConstantSymbol();
        var secondSymbol = new EvolvableConstantSymbol();
        var expression = Apply(new ReverseSubtractSymbol(), Constant(1.0, firstSymbol), Constant(2.0, secondSymbol)).Build();

        var lowered = CompileSuccessfully(expression);

        lowered.CreateInitialParameterValues().ShouldBe([2.0, 1.0]);
        lowered.ParameterBindings[0].Point.ChildIndex.ShouldBe(1);
        lowered.ParameterBindings[1].Point.ChildIndex.ShouldBe(0);
    }

    [Fact]
    public void WithParameterValuesRebuildsOccurrencesWithoutChangingTheSourceExpression()
    {
        var firstSymbol = new EvolvableConstantSymbol();
        var secondSymbol = new EvolvableConstantSymbol();
        var expression = (Constant(1.0, firstSymbol) + FixedConstant(3.0) * Constant(2.0, secondSymbol)).Build();
        var lowered = CompileSuccessfully(expression);

        var rebuilt = lowered.WithParameterValues([10.0, 20.0]);

        rebuilt.ShouldNotBeSameAs(expression);
        GetNumericConstant(expression.RootPoint.Child(0)).Value.ShouldBe(1.0);
        GetNumericConstant(expression.RootPoint.Child(1).Child(1)).Value.ShouldBe(2.0);
        var rebuiltFirst = GetNumericConstant(rebuilt.RootPoint.Child(0));
        var rebuiltSecond = GetNumericConstant(rebuilt.RootPoint.Child(1).Child(1));
        rebuiltFirst.Value.ShouldBe(10.0);
        rebuiltSecond.Value.ShouldBe(20.0);
        rebuiltFirst.Symbol.ShouldBeSameAs(firstSymbol);
        rebuiltSecond.Symbol.ShouldBeSameAs(secondSymbol);
        rebuilt.RootPoint.Child(1).Child(0).Node.ShouldBeSameAs(expression.RootPoint.Child(1).Child(0).Node);
    }

    [Fact]
    public void WithParameterValuesRebuildsReferenceSharedOccurrencesIndependently()
    {
        var symbol = new EvolvableConstantSymbol();
        var shared = new NumericConstantExpressionNode(symbol, 2.0);
        var expression = new ExpressionTree(new BinaryExpressionNode(Symbols.Addition, shared, shared));
        var lowered = CompileSuccessfully(expression);

        var rebuilt = lowered.WithParameterValues([10.0, 20.0]);

        GetNumericConstant(rebuilt.RootPoint.Child(0)).Value.ShouldBe(10.0);
        GetNumericConstant(rebuilt.RootPoint.Child(1)).Value.ShouldBe(20.0);
        GetNumericConstant(expression.RootPoint.Child(0)).Value.ShouldBe(2.0);
        GetNumericConstant(expression.RootPoint.Child(1)).Value.ShouldBe(2.0);
    }

    [Fact]
    public void WithParameterValuesRebuildsAnEvolvableRootConstant()
    {
        var symbol = new EvolvableConstantSymbol();
        var expression = Constant(2.0, symbol).Build();
        var lowered = CompileSuccessfully(expression);

        var rebuilt = lowered.WithParameterValues([5.0]);

        var rebuiltConstant = GetNumericConstant(rebuilt.RootPoint);
        rebuiltConstant.Value.ShouldBe(5.0);
        rebuiltConstant.Symbol.ShouldBeSameAs(symbol);
        GetNumericConstant(expression.RootPoint).Value.ShouldBe(2.0);
    }

    [Fact]
    public void WithParameterValuesPreservesANonFiniteParameterValue()
    {
        var expression = Constant(2.0).Build();
        var lowered = CompileSuccessfully(expression);

        var rebuilt = lowered.WithParameterValues([double.NaN]);

        double.IsNaN(GetNumericConstant(rebuilt.RootPoint).Value).ShouldBeTrue();
        GetNumericConstant(expression.RootPoint).Value.ShouldBe(2.0);
    }

    [Fact]
    public void FixedOnlyExpressionHasNoParametersAndRebuildsToTheSourceExpression()
    {
        var expression = (FixedConstant(1.0) + FixedConstant(2.0)).Build();
        var lowered = CompileSuccessfully(expression);

        lowered.ParameterBindings.ShouldBeEmpty();
        lowered.CreateInitialParameterValues().ShouldBeEmpty();
        lowered.WithParameterValues([]).ShouldBeSameAs(expression);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void WithParameterValuesRejectsTheWrongParameterCount(int parameterCount)
    {
        var lowered = CompileSuccessfully(Constant(1.0).Build());

        Should.Throw<ArgumentException>(() => lowered.WithParameterValues(new double[parameterCount]));
    }

    [Fact]
    public void Compile_ThrowsWhenSymbolDoesNotEmitExactlyOneValue()
    {
        var expression = new ExpressionTree(new PayloadlessTerminalExpressionNode(new EmptySymbol()));

        var exception = Should.Throw<InvalidOperationException>(() => DifferentiableExpressionCompiler.TryCompile(expression, out _, out _));

        exception.Message.ShouldContain("must emit exactly one value");
    }

    [Fact]
    public void Compile_PropagatesExceptionsThrownBySymbols()
    {
        var expression = new ExpressionTree(new PayloadlessTerminalExpressionNode(new ThrowingSymbol()));

        Should.Throw<NotSupportedException>(() => DifferentiableExpressionCompiler.TryCompile(expression, out _, out _));
    }

    [Fact]
    public void DifferentiableExpressionRejectsInconsistentRetainedMappings()
    {
        var expression = FixedConstant(1.0).Build();
        var inputBuilder = new AD.Builder();
        var inputProgram = inputBuilder.Build(inputBuilder.Input());
        var parameterBuilder = new AD.Builder();
        var parameterProgram = parameterBuilder.Build(parameterBuilder.Parameter());

        Should.Throw<ArgumentException>(() => new DifferentiableExpression(expression, inputProgram, [], [])).ParamName.ShouldBe("variableNames");
        Should.Throw<ArgumentException>(() => new DifferentiableExpression(expression, parameterProgram, [], [])).ParamName.ShouldBe("parameterBindings");
    }

    private static DifferentiableExpression CompileSuccessfully(ExpressionTree expression)
    {
        DifferentiableExpressionCompiler.TryCompile(expression, out var differentiableExpression).ShouldBeTrue();
        return differentiableExpression;
    }

    private static NumericConstantExpressionNode GetNumericConstant(ExpressionPoint point) => point.Node.ShouldBeOfType<NumericConstantExpressionNode>();

    private sealed record SquareChildSymbol() : OperationSymbol("square-child", 1)
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            emitter.EmitChild(0);
            emitter.EmitChild(0);
            emitter.EmitOperation(Operation.Multiply);
        }
    }

    private sealed record ReverseSubtractSymbol() : OperationSymbol("reverse-subtract", 2)
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            emitter.EmitChild(1);
            emitter.EmitChild(0);
            emitter.EmitOperation(Operation.Subtract);
        }
    }

    private sealed record EmptySymbol() : PayloadlessTerminalSymbol("empty")
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
        }
    }

    private sealed record ThrowingSymbol() : PayloadlessTerminalSymbol("throwing")
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter) => throw new NotSupportedException();
    }
}
