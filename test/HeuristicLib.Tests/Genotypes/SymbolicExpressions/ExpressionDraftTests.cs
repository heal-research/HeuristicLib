using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class ExpressionDraftTests
{
    [Fact]
    public void Compile_BuildsPostorderExpressionAndInternsVariablesByName()
    {
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();

        expression.TraversePostOrder().Select(node => node.OpCode).ShouldBe([
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.NumericLiteral,
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.Multiply,
          SymbolicExpressionOpCode.Add
        ]);
        GetVariableNames(expression).ShouldBe(["x0", "x1"]);
    }

    [Fact]
    public void Compile_PreservesRepeatedVariableOccurrences()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x0"))
          .Compile();

        GetVariableNames(expression).ShouldBe(["x0", "x0"]);
    }

    [Fact]
    public void Compile_PreservesNumericLiteralMetadata()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Fixed(1.0), ExpressionDraft.Parameter(2.0))
          .Compile();

        GetNumericLiterals(expression).ShouldBe([
          new NumericLiteral(1.0, NumericLiteralKind.Fixed),
          new NumericLiteral(2.0, NumericLiteralKind.Optimizable)
        ]);
    }

    [Fact]
    public void ToInfixString_FormatsBinaryExpression()
    {
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
    }

    private static string[] GetVariableNames(SymbolicExpression expression)
    {
        var names = new List<string>();
        foreach (var node in expression.TraversePostOrder().Where(node => node.OpCode == SymbolicExpressionOpCode.Variable))
        {
            expression.GetSubExpression(node.Location).TryGetVariableReference(out var variable).ShouldBeTrue();
            names.Add(variable.Name);
        }

        return [.. names];
    }

    private static NumericLiteral[] GetNumericLiterals(SymbolicExpression expression)
    {
        var literals = new List<NumericLiteral>();
        foreach (var node in expression.TraversePostOrder().Where(node => node.OpCode == SymbolicExpressionOpCode.NumericLiteral))
        {
            expression.GetSubExpression(node.Location).TryGetNumericLiteral(out var literal).ShouldBeTrue();
            literals.Add(literal);
        }

        return [.. literals];
    }
}
