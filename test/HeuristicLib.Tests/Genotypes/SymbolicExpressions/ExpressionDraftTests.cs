using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class ExpressionDraftTests
{
    [Fact]
    public void Build_CreatesPostorderSymbolExpression()
    {
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Build();

        expression.TraversePostOrder().Select(node => node.Symbol).ShouldBe([
          new VariableSymbol("x0"),
          new NumericLiteralSymbol(new NumericLiteral(2.0, NumericLiteralKind.Fixed)),
          new VariableSymbol("x1"),
          new MultiplySymbol(),
          new AddSymbol()
        ]);
        GetVariableNames(expression).ShouldBe(["x0", "x1"]);
    }

    [Fact]
    public void Build_PreservesRepeatedVariableOccurrences()
    {
        var expression = (Variable("x0") + Variable("x0")).Build();

        GetVariableNames(expression).ShouldBe(["x0", "x0"]);
    }

    [Fact]
    public void Build_PreservesNumericLiteralMetadata()
    {
        var expression = (Fixed(1.0) + Parameter(2.0)).Build();

        GetNumericLiterals(expression).ShouldBe([
          new NumericLiteral(1.0, NumericLiteralKind.Fixed),
          new NumericLiteral(2.0, NumericLiteralKind.Optimizable)
        ]);
    }

    [Fact]
    public void ToInfixString_FormatsBinaryExpression()
    {
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Build();

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
    }

    [Fact]
    public void Operators_MapToExpectedSymbolicExpressionOperations()
    {
        (Variable("left") - Variable("right")).Build().ToInfixString().ShouldBe("(left - right)");
        (Variable("left") / Variable("right")).Build().ToInfixString().ShouldBe("(left / right)");
    }

    private static string[] GetVariableNames(SymbolicExpression expression)
    {
        var names = new List<string>();
        foreach (var node in expression.TraversePostOrder().Where(node => node.Symbol is VariableSymbol))
        {
            names.Add(((VariableSymbol)node.Symbol).VariableName);
        }

        return [.. names];
    }

    private static NumericLiteral[] GetNumericLiterals(SymbolicExpression expression)
    {
        var literals = new List<NumericLiteral>();
        foreach (var node in expression.TraversePostOrder().Where(node => node.Symbol is NumericLiteralSymbol))
        {
            literals.Add(((NumericLiteralSymbol)node.Symbol).Literal);
        }

        return [.. literals];
    }
}
