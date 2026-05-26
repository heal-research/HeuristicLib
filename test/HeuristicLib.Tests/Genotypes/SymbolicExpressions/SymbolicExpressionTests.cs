using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionTests
{
    [Fact]
    public void Create_RejectsEmptyInstructions()
    {
        Should.Throw<ArgumentException>(() =>
          SymbolicExpression.Create([], [], []));
    }

    [Fact]
    public void Create_RejectsInvalidOpcode()
    {
        Should.Throw<ArgumentException>(() =>
          SymbolicExpression.Create(
            [new ExpressionInstruction(SymbolicExpressionOpCode.Invalid, Arity: 0, SubtreeLength: 1)],
            [],
            []));
    }

    [Fact]
    public void Create_RejectsStackUnderflow()
    {
        Should.Throw<ArgumentException>(() =>
          SymbolicExpression.Create(
            [new ExpressionInstruction(SymbolicExpressionOpCode.Add, Arity: 2, SubtreeLength: 3)],
            [],
            []));
    }

    [Fact]
    public void Create_RejectsMultipleRootExpressions()
    {
        Should.Throw<ArgumentException>(() =>
          SymbolicExpression.Create(
            [
                ExpressionInstruction.NumericLiteral(0),
                ExpressionInstruction.NumericLiteral(1)
            ],
            [
                new NumericLiteral(1.0, NumericLiteralKind.Fixed),
                new NumericLiteral(2.0, NumericLiteralKind.Fixed)
            ],
            []));
    }

    [Fact]
    public void Create_RejectsInvalidPayloadIndex()
    {
        Should.Throw<ArgumentException>(() =>
          SymbolicExpression.Create(
            [ExpressionInstruction.Variable(0)],
            [],
            []));
    }

    [Fact]
    public void Create_RejectsVariableReferenceWithMismatchedIndex()
    {
        Should.Throw<ArgumentException>(() =>
          SymbolicExpression.Create(
            [ExpressionInstruction.Variable(0)],
            [],
            [new VariableReference("x0", 1)]));
    }

    [Fact]
    public void Create_RejectsInvalidSubtreeLength()
    {
        Should.Throw<ArgumentException>(() =>
          SymbolicExpression.Create(
            [
                ExpressionInstruction.Variable(0),
                new ExpressionInstruction(SymbolicExpressionOpCode.Sqrt, Arity: 1, SubtreeLength: 3)
            ],
            [],
            [new VariableReference("x0", 0)]));
    }

    [Fact]
    public void Create_CalculatesLengthDepthAndComplexity()
    {
        var expression = SymbolicExpression.Create(
          [
              ExpressionInstruction.Variable(0),
              ExpressionInstruction.NumericLiteral(0),
              ExpressionInstruction.Variable(1),
              ExpressionInstruction.Binary(SymbolicExpressionOpCode.Multiply, 1, 1),
              ExpressionInstruction.Binary(SymbolicExpressionOpCode.Add, 1, 3)
          ],
          [new NumericLiteral(2.0, NumericLiteralKind.Fixed)],
          [new VariableReference("x0", 0), new VariableReference("x1", 1)]);

        expression.Length.ShouldBe(5);
        expression.Complexity.ShouldBe(5);
        expression.Depth.ShouldBe(3);
    }

    [Fact]
    public void Create_CopiesPublicFactoryInputs()
    {
        var instructions = new[]
        {
            ExpressionInstruction.Variable(0)
        };
        var variables = new[]
        {
            new VariableReference("x0", 0)
        };

        var expression = SymbolicExpression.Create(instructions, [], variables);

        instructions[0] = ExpressionInstruction.Variable(1);
        variables[0] = new VariableReference("changed", 0);

        expression.Instructions[0].PayloadIndex.ShouldBe(0);
        expression.VariableReferences[0].Name.ShouldBe("x0");
    }

    [Fact]
    public void Equals_IncludesNumericLiteralKind()
    {
        var fixedExpression = SymbolicExpression.Create(
          [ExpressionInstruction.NumericLiteral(0)],
          [new NumericLiteral(1.0, NumericLiteralKind.Fixed)],
          []);
        var optimizableExpression = SymbolicExpression.Create(
          [ExpressionInstruction.NumericLiteral(0)],
          [new NumericLiteral(1.0, NumericLiteralKind.Optimizable)],
          []);

        fixedExpression.ShouldNotBe(optimizableExpression);
    }

    [Fact]
    public void Evaluate_UsesVariableTableOrder()
    {
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();

        expression.Evaluate([3.0, 5.0]).ShouldBe(13.0);
    }

    [Fact]
    public void Evaluate_UsesVariableNames()
    {
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();

        expression.Evaluate(["x1", "x0"], [5.0, 3.0]).ShouldBe(13.0);
    }

    [Fact]
    public void Evaluate_AppliesUnaryAndBinaryOperations()
    {
        var expression = ExpressionDraft
          .Sqrt(
            ExpressionDraft.Log(
              ExpressionDraft.Divide(
                ExpressionDraft.Variable("x0"),
                ExpressionDraft.Fixed(Math.E))))
          .Compile();

        expression.Evaluate(["x0"], [Math.E * Math.E]).ShouldBe(1.0, tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_RejectsWrongVariableValueCount()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();

        Should.Throw<ArgumentException>(() => expression.Evaluate([]));
    }

    [Fact]
    public void Evaluate_RejectsMissingNamedVariable()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();

        Should.Throw<ArgumentException>(() => expression.Evaluate(["x1"], [1.0]));
    }

    [Fact]
    public void Evaluate_RejectsDuplicateNamedVariable()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();

        Should.Throw<ArgumentException>(() => expression.Evaluate(["x0", "x0"], [1.0, 2.0]));
    }

    [Fact]
    public void Evaluate_UsesVariableTableColumnOrder()
    {
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();

        var values = new double[,]
        {
            { 1.0, 3.0 },
            { 2.0, 4.0 },
            { 3.0, 5.0 }
        };

        expression.Evaluate(values).ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Evaluate_UsesVariableNamesOnceForAllRows()
    {
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();

        var values = new double[,]
        {
            { 3.0, 1.0, 100.0 },
            { 4.0, 2.0, 100.0 },
            { 5.0, 3.0, 100.0 }
        };

        expression.Evaluate(["x1", "x0", "unused"], values).ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Evaluate_WritesIntoDestination()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();
        var values = new double[,]
        {
            { 1.0 },
            { 2.0 }
        };
        var destination = new[] { double.NaN, double.NaN, 42.0 };

        expression.Evaluate(values, destination);

        destination.ShouldBe([1.0, 2.0, 42.0]);
    }

    [Fact]
    public void Evaluate_RejectsWrongVariableColumnCountForTableOrder()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();

        Should.Throw<ArgumentException>(() => expression.Evaluate(new double[,] { { 1.0, 2.0 } }));
    }

    [Fact]
    public void Evaluate_RejectsTooSmallDestination()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();
        var values = new double[,]
        {
            { 1.0 },
            { 2.0 }
        };

        Should.Throw<ArgumentException>(() => expression.Evaluate(values, new double[1]));
    }

    [Fact]
    public void Evaluate_RejectsMissingNamedColumn()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();
        var values = new double[,] { { 1.0 } };

        Should.Throw<ArgumentException>(() => expression.Evaluate(["x1"], values));
    }

    [Fact]
    public void Evaluate_RejectsDuplicateNamedColumn()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();
        var values = new double[,] { { 1.0, 2.0 } };

        Should.Throw<ArgumentException>(() => expression.Evaluate(["x0", "x0"], values));
    }
}
