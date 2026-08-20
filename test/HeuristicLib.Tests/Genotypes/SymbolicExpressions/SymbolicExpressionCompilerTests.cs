using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Numerics;
using HEAL.HeuristicLib.Random.Distributions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionCompilerTests
{
    [Fact]
    public void Compile_LowersNodesWithoutSymbolMetadata()
    {
        var symbol = new EvolvableConstantSymbol(new UniformDoubleDistribution(10, 20), new ResampleInitialNumericPerturbation());
        var expression = (Variable("x0") + Constant(12.0, symbol)).Build();

        var compiled = expression.Compile(optimize: false);

        compiled.TraversePostOrder().Select(node => node.Operation).ShouldBe([Operation.Variable, Operation.Constant, Operation.Add]);
        expression.EvaluateSingleRow(("x0", 3.0)).ShouldBe(15.0);
    }

    [Fact]
    public void Compile_ExpandsMacroOnlyInCompiledExpression()
    {
        var expression = Sigmoid(Variable("x0")).Build();

        expression.Length.ShouldBe(2);
        expression.Compile(optimize: false).ToInfixString().ShouldBe("(1 / (1 + exp(negate(x0))))");
    }

    [Fact]
    public void Compile_FoldsFixedAndEvolvableConstants()
    {
        var fixedExpression = (FixedConstant(2.0) + FixedConstant(3.0)).Build();
        var evolvableExpression = (Constant(2.0) + Constant(3.0)).Build();

        fixedExpression.Compile().ToInfixString().ShouldBe("5");
        evolvableExpression.Compile().ToInfixString().ShouldBe("5");
        fixedExpression.Compile().ShouldBe(evolvableExpression.Compile());
        GetConstants(evolvableExpression.Compile()).ShouldBe([5.0]);
    }

    [Fact]
    public void Compile_FoldsMixedConstants()
    {
        var compiled = (FixedConstant(1.0) + Constant(2.0)).Build().Compile();

        GetConstants(compiled).ShouldBe([3.0]);
    }

    [Fact]
    public void Compile_FoldsExtendedNumericOperations()
    {
        Sin(FixedConstant(Math.PI / 2.0)).Build().Compile().ToInfixString().ShouldBe("1");
        Abs(FixedConstant(-3.0)).Build().Compile().ToInfixString().ShouldBe("3");
        Square(FixedConstant(-3.0)).Build().Compile().ToInfixString().ShouldBe("9");
        Cube(FixedConstant(-2.0)).Build().Compile().ToInfixString().ShouldBe("-8");
        CubeRoot(FixedConstant(-8.0)).Build().Compile().ToInfixString().ShouldBe("-2");
        Power(FixedConstant(9.0), FixedConstant(0.5)).Build().Compile().ToInfixString().ShouldBe("3");
        Root(FixedConstant(32.0), FixedConstant(2.5)).Build().Compile().ToInfixString().ShouldBe("4");
        GetConstants(AnalyticQuotient(FixedConstant(6.0), FixedConstant(Math.Sqrt(3.0))).Build().Compile())[0]
            .ShouldBe(3.0, tolerance: 1e-12);
    }

    [Fact]
    public void Compile_FoldsConstantsIntoOnePayload()
    {
        var compiled = ((FixedConstant(2.0) + FixedConstant(3.0)) * Variable("x0")).Build().Compile();

        compiled.ToInfixString().ShouldBe("(5 * x0)");
        GetConstants(compiled).ShouldBe([5.0]);
    }

    [Fact]
    public void Compile_MacroSymbolEmitsMultipleBuiltInOpcodesWithoutExpandingTheTree()
    {
        var expression = Sigmoid(Variable("x0")).Build();

        expression.Length.ShouldBe(2);
        expression.EvaluateSingleRow(("x0", 0.0)).ShouldBe(0.5);
        expression.Compile(optimize: false).ToInfixString().ShouldBe("(1 / (1 + exp(negate(x0))))");
    }

    [Fact]
    public void Compile_ExternalMacroSymbolCanEmitMultipleBuiltInOpcodes()
    {
        var expression = ExpressionDraft.Apply(new DoubleSymbol(), Variable("x0")).Build();

        expression.Compile(optimize: false).ToInfixString().ShouldBe("(x0 * 2)");
    }

    [Fact]
    public void Compile_InternsRepeatedVariablesByName()
    {
        var compiled = (Variable("x0") + Variable("x0")).Build().Compile(optimize: false);

        compiled.VariableReferenceCount.ShouldBe(1);
        compiled.GetInstruction(0).PayloadIndex.ShouldBe(0);
        compiled.GetInstruction(1).PayloadIndex.ShouldBe(0);
    }

    [Fact]
    public void Compile_EliminatesConstantIdentityOperations()
    {
        ((Variable("x0") + FixedConstant(0.0)) * FixedConstant(1.0)).Build().Compile().ToInfixString().ShouldBe("x0");
        ((Variable("x0") + Constant(0.0)) * Constant(1.0)).Build().Compile().ToInfixString().ShouldBe("x0");
    }

    [Fact]
    public void Compile_EliminatesLeftIdentityBeforeComplexRightOperand()
    {
        var product = Variable("x0") * Variable("x1");

        (FixedConstant(0.0) + product).Build().Compile().ToInfixString().ShouldBe("(x0 * x1)");
        (FixedConstant(1.0) * product).Build().Compile().ToInfixString().ShouldBe("(x0 * x1)");
    }

    [Fact]
    public void Compile_DoesNotApplyUnsafeAnnihilatorOrDivisionRewrites()
    {
        (Variable("x0") * FixedConstant(0.0)).Build().Compile().ToInfixString().ShouldBe("(x0 * 0)");
        (FixedConstant(0.0) / Variable("x0")).Build().Compile().ToInfixString().ShouldBe("(0 / x0)");
        (Variable("x0") / Variable("x0")).Build().Compile().ToInfixString().ShouldBe("(x0 / x0)");
    }

    [Fact]
    public void Compile_CanDisableOptimization()
    {
        var expression = ((FixedConstant(2.0) + FixedConstant(3.0)) * (Variable("x0") + FixedConstant(0.0))).Build();

        expression.Compile(optimize: false).ToInfixString().ShouldBe("((2 + 3) * (x0 + 0))");
        expression.Compile(optimize: true).ToInfixString().ShouldBe("(5 * x0)");
    }

    [Fact]
    public void Compile_OptimizedAndUnoptimizedExpressionsEvaluateIdentically()
    {
        var expression = ((FixedConstant(2.0) + FixedConstant(3.0)) * (Variable("x0") + FixedConstant(0.0)) / FixedConstant(1.0)).Build();
        var optimized = expression.Compile(optimize: true);
        var unoptimized = expression.Compile(optimize: false);

        foreach (var x0 in new[] { -2.0, 0.0, 4.5, double.PositiveInfinity, double.NaN })
        {
            var data = new DataFrame([Series<double>.FromOwnedArray("x0", [x0])]);
            ExpressionInterpreter.Interpret(optimized, data)[0].ShouldBe(ExpressionInterpreter.Interpret(unoptimized, data)[0]);
        }
    }

    [Fact]
    public void CompiledExpression_CanBeRetainedAndEvaluatedRepeatedly()
    {
        var compiled = (Variable("x0") + FixedConstant(2.0)).Build().Compile();
        var first = new DataFrame([Series<double>.FromOwnedArray("x0", [1.0, 2.0])]);
        var second = new DataFrame([Series<double>.FromOwnedArray("x0", [3.0, 4.0])]);

        ExpressionInterpreter.Interpret(compiled, first).ShouldBe([3.0, 4.0]);
        ExpressionInterpreter.Interpret(compiled, second).ShouldBe([5.0, 6.0]);
    }

    private static double[] GetConstants(CompiledExpression expression)
    {
        var constants = new List<double>();
        foreach (var node in expression.TraversePostOrder().Where(node => node.Operation == Operation.Constant))
        {
            node.TryGetConstantValue(out var value).ShouldBeTrue();
            constants.Add(value);
        }

        return [.. constants];
    }

    private sealed record DoubleSymbol() : OperationSymbol("double", 1)
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            emitter.EmitChild(0);
            emitter.EmitConstant(2.0);
            emitter.EmitOperation(Operation.Multiply);
        }
    }
}
