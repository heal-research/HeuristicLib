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

        expression.VariableReferences.Select(variable => variable.Name).ShouldBe(["x0", "x1"]);
        expression.Instructions.Select(instruction => instruction.OpCode).ShouldBe([
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.NumericLiteral,
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.Multiply,
          SymbolicExpressionOpCode.Add
        ]);
        expression.Instructions.Where(instruction => instruction.OpCode == SymbolicExpressionOpCode.Variable)
          .Select(instruction => instruction.PayloadIndex)
          .ShouldBe([0, 1]);
    }

    [Fact]
    public void Compile_ReusesVariableReferenceForRepeatedName()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x0"))
          .Compile();

        expression.VariableReferences.Select(variable => variable.Name).ShouldBe(["x0"]);
        expression.Instructions.Where(instruction => instruction.OpCode == SymbolicExpressionOpCode.Variable)
          .Select(instruction => instruction.PayloadIndex)
          .ShouldBe([0, 0]);
    }

    [Fact]
    public void Compile_PreservesNumericLiteralMetadata()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Fixed(1.0), ExpressionDraft.Parameter(2.0))
          .Compile();

        expression.NumericLiterals.ShouldBe([
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
}
