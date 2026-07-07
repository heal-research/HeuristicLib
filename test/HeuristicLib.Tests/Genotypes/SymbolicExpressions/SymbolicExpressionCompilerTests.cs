using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionCompilerTests
{
    [Fact]
    public void Compile_LowersBuiltInSymbolsToPostOrderInstructions()
    {
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Build();

        var compiled = expression.Compile(optimize: false);

        compiled.TraversePostOrder().Select(node => node.OpCode).ShouldBe([
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.NumericLiteral,
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.Multiply,
          SymbolicExpressionOpCode.Add
        ]);
    }

    [Fact]
    public void Compile_InternsRepeatedVariablesByName()
    {
        var expression = (Variable("x0") + Variable("x0")).Build();

        var compiled = expression.Compile(optimize: false);

        compiled.VariableReferenceCount.ShouldBe(1);
        compiled.GetInstruction(0).PayloadIndex.ShouldBe(0);
        compiled.GetInstruction(1).PayloadIndex.ShouldBe(0);
    }

    [Fact]
    public void Compile_PreservesOptimizableNumericLiterals()
    {
        var expression = (Fixed(1.0) + Parameter(2.0)).Build();

        var compiled = expression.Compile();

        GetNumericLiterals(compiled).ShouldBe([
          new NumericLiteral(1.0, NumericLiteralKind.Fixed),
          new NumericLiteral(2.0, NumericLiteralKind.Optimizable)
        ]);
    }

    [Fact]
    public void Compile_AllowsMacroSymbolsToEmitMultipleBuiltInOpcodes()
    {
        var expression = SymbolicExpression.Create([
          new VariableSymbol("x0"),
          new SigmoidSymbol()
        ]);

        expression.EvaluateSingleRow(("x0", 0.0)).ShouldBe(0.5);
        expression.Compile(optimize: false).ToInfixString().ShouldBe("(1 / (1 + exp(negate(x0))))");
    }

    [Fact]
    public void Compile_FoldsFixedConstants()
    {
        var expression = ((Fixed(2.0) + Fixed(3.0)) * Variable("x0")).Build();

        var compiled = expression.Compile();

        compiled.ToInfixString().ShouldBe("(5 * x0)");
        GetNumericLiterals(compiled).ShouldBe([new NumericLiteral(5.0, NumericLiteralKind.Fixed)]);
    }

    [Fact]
    public void Compile_EliminatesFixedIdentityOperations()
    {
        var expression = ((Variable("x0") + Fixed(0.0)) * Fixed(1.0)).Build();

        var compiled = expression.Compile();

        compiled.ToInfixString().ShouldBe("x0");
    }

    [Fact]
    public void Compile_DoesNotApplyUnsafeAnnihilatorOrDivisionRewrites()
    {
        var multiplyByZero = (Variable("x0") * Fixed(0.0)).Build().Compile();
        var zeroDividedByVariable = (Fixed(0.0) / Variable("x0")).Build().Compile();
        var variableDividedByItself = (Variable("x0") / Variable("x0")).Build().Compile();

        multiplyByZero.ToInfixString().ShouldBe("(x0 * 0)");
        zeroDividedByVariable.ToInfixString().ShouldBe("(0 / x0)");
        variableDividedByItself.ToInfixString().ShouldBe("(x0 / x0)");
    }

    [Fact]
    public void Compile_CanDisableOptimization()
    {
        var expression = ((Fixed(2.0) + Fixed(3.0)) * (Variable("x0") + Fixed(0.0))).Build();

        expression.Compile(optimize: false).ToInfixString().ShouldBe("((2 + 3) * (x0 + 0))");
        expression.Compile(optimize: true).ToInfixString().ShouldBe("(5 * x0)");
    }

    [Fact]
    public void Compile_OptimizedAndUnoptimizedExpressionsEvaluateIdenticallyForRepresentativeValues()
    {
        var expression = ((Fixed(2.0) + Fixed(3.0)) * (Variable("x0") + Fixed(0.0)) / Fixed(1.0)).Build();
        var optimized = expression.Compile(optimize: true);
        var unoptimized = expression.Compile(optimize: false);

        foreach (var x0 in new[] { -2.0, 0.0, 4.5, double.PositiveInfinity, double.NaN })
        {
            var data = DataFrame.FromOwnedColumns([KeyValuePair.Create("x0", new[] { x0 })]);
            var optimizedValue = SymbolicExpressionInterpreter.Interpret(optimized, data)[0];
            var unoptimizedValue = SymbolicExpressionInterpreter.Interpret(unoptimized, data)[0];
            optimizedValue.ShouldBe(unoptimizedValue);
        }
    }

    private static NumericLiteral[] GetNumericLiterals(CompiledSymbolicExpression expression)
    {
        var literals = new List<NumericLiteral>();
        foreach (var node in expression.TraversePostOrder().Where(node => node.OpCode == SymbolicExpressionOpCode.NumericLiteral))
        {
            node.TryGetNumericLiteral(out var literal).ShouldBeTrue();
            literals.Add(literal);
        }

        return [.. literals];
    }

}
