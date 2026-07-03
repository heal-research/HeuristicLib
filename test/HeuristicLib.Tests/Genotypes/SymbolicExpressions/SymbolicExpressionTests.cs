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
    public void Root_ReturnsSubExpressionOverWholeExpression()
    {
        var expression = CreateLinearExpression();

        var root = expression.Root;

        root.Length.ShouldBe(5);
        root.OpCode.ShouldBe(SymbolicExpressionOpCode.Add);
        root.Arity.ShouldBe(2);
    }

    [Fact]
    public void Child_NavigatesToSubExpression()
    {
        var expression = CreateLinearExpression();

        var root = expression.Root;
        var left = root.Child(0);
        var right = root.Child(1);

        left.OpCode.ShouldBe(SymbolicExpressionOpCode.Variable);
        left.TryGetVariableReference(out var leftVariable).ShouldBeTrue();
        leftVariable.Name.ShouldBe("x0");
        right.OpCode.ShouldBe(SymbolicExpressionOpCode.Multiply);
        right.Length.ShouldBe(3);
        var rightLeft = right.Child(0);
        var rightRight = right.Child(1);
        rightLeft.TryGetNumericLiteral(out var literal).ShouldBeTrue();
        rightRight.TryGetVariableReference(out var rightVariable).ShouldBeTrue();
        literal.Value.ShouldBe(2.0);
        rightVariable.Name.ShouldBe("x1");
    }

    [Fact]
    public void Child_PreservesLeftToRightOrder()
    {
        var expression = ExpressionDraft
          .Subtract(ExpressionDraft.Variable("left"), ExpressionDraft.Variable("right"))
          .Compile();

        var root = expression.Root;

        root.OpCode.ShouldBe(SymbolicExpressionOpCode.Subtract);
        var left = root.Child(0);
        var right = root.Child(1);
        left.TryGetVariableReference(out var leftVariable).ShouldBeTrue();
        right.TryGetVariableReference(out var rightVariable).ShouldBeTrue();
        leftVariable.Name.ShouldBe("left");
        rightVariable.Name.ShouldBe("right");
    }

    [Fact]
    public void Child_NavigatesUnarySubExpression()
    {
        var expression = ExpressionDraft.Sqrt(ExpressionDraft.Variable("x0")).Compile();

        var root = expression.Root;
        var child = root.Child(0);

        root.OpCode.ShouldBe(SymbolicExpressionOpCode.Sqrt);
        root.Arity.ShouldBe(1);
        child.OpCode.ShouldBe(SymbolicExpressionOpCode.Variable);
        child.TryGetVariableReference(out var variable).ShouldBeTrue();
        variable.Name.ShouldBe("x0");
    }

    [Fact]
    public void Children_EnumeratesDirectChildrenFromLeftToRight()
    {
        var expression = CreateLinearExpression();

        var children = expression.Root.TraverseChildren().ToArray();

        children.Select(child => child.OpCode).ShouldBe([
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.Multiply
        ]);
        children[0].TryGetVariableReference(out var leftVariable).ShouldBeTrue();
        leftVariable.Name.ShouldBe("x0");
        children[1].TraverseChildren().Select(child => child.OpCode).ShouldBe([
          SymbolicExpressionOpCode.NumericLiteral,
          SymbolicExpressionOpCode.Variable
        ]);
    }

    [Fact]
    public void TraversePostOrder_EnumeratesEditableExpressionLocations()
    {
        var expression = CreateLinearExpression();

        var nodes = expression.TraversePostOrder().ToArray();

        nodes.Select(node => node.OpCode).ShouldBe([
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.NumericLiteral,
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.Multiply,
          SymbolicExpressionOpCode.Add
        ]);
        nodes.Select(node => node.SubtreeLength).ShouldBe([1, 1, 1, 3, 5]);
        expression.WithOpCode(nodes[^1].Location, SymbolicExpressionOpCode.Subtract)
          .ToInfixString()
          .ShouldBe("(x0 - (2 * x1))");
    }

    [Fact]
    public void TraversePreOrder_EnumeratesRootBeforeChildren()
    {
        var expression = CreateLinearExpression();

        expression.TraversePreOrder().Select(node => node.OpCode).ShouldBe([
          SymbolicExpressionOpCode.Add,
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.Multiply,
          SymbolicExpressionOpCode.NumericLiteral,
          SymbolicExpressionOpCode.Variable
        ]);
    }

    [Fact]
    public void TraverseBreadthFirst_EnumeratesLevelByLevel()
    {
        var expression = CreateLinearExpression();

        expression.TraverseBreadthFirst().Select(node => node.OpCode).ShouldBe([
          SymbolicExpressionOpCode.Add,
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.Multiply,
          SymbolicExpressionOpCode.NumericLiteral,
          SymbolicExpressionOpCode.Variable
        ]);
    }

    [Fact]
    public void SubExpressionTraversal_EnumeratesOnlySelectedSubtree()
    {
        var expression = CreateLinearExpression();
        var rightBranch = expression.Root.Child(1);

        rightBranch.TraversePostOrder().Select(node => node.OpCode).ShouldBe([
          SymbolicExpressionOpCode.NumericLiteral,
          SymbolicExpressionOpCode.Variable,
          SymbolicExpressionOpCode.Multiply
        ]);
        rightBranch.TraversePreOrder().Select(node => node.OpCode).ShouldBe([
          SymbolicExpressionOpCode.Multiply,
          SymbolicExpressionOpCode.NumericLiteral,
          SymbolicExpressionOpCode.Variable
        ]);
    }

    [Fact]
    public void Child_RejectsIndexOutsideArity()
    {
        var expression = CreateLinearExpression();

        Should.Throw<ArgumentOutOfRangeException>(() => ChildAtRoot(expression, 2));
    }

    [Fact]
    public void Child_RejectsChildAccessOnLeaf()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();

        Should.Throw<ArgumentOutOfRangeException>(() => ChildAtRoot(expression, 0));
    }

    [Fact]
    public void TryGetNumericLiteral_ReturnsFalseForNonLiteralSubExpression()
    {
        var expression = CreateLinearExpression();
        var root = expression.Root;

        root.TryGetNumericLiteral(out _).ShouldBeFalse();
    }

    [Fact]
    public void TryGetVariableReference_ReturnsFalseForNonVariableSubExpression()
    {
        var expression = CreateLinearExpression();
        var literal = expression.Root.Child(1).Child(0);

        literal.TryGetVariableReference(out _).ShouldBeFalse();
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

        expression.Root.TryGetVariableReference(out var variable).ShouldBeTrue();
        variable.Name.ShouldBe("x0");
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
        var data = DataFrame.FromMatrix(
          ["x0", "x1"],
          new double[,]
          {
              { 1.0, 3.0 },
              { 2.0, 4.0 },
              { 3.0, 5.0 }
          });

        expression.Evaluate(data).ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Interpreter_UsesDataFrameColumns()
    {
        var expression = ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();
        var data = new DataFrame([
          KeyValuePair.Create("x0", Series<double>.Create([1.0, 2.0, 3.0])),
          KeyValuePair.Create("x1", Series<double>.Create([3.0, 4.0, 5.0]))
        ]);

        SymbolicExpressionInterpreter.Interpret(expression, data).ShouldBe([7.0, 10.0, 13.0]);
        expression.Evaluate(data).ShouldBe([7.0, 10.0, 13.0]);
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
        var data = DataFrame.FromMatrix(
          ["x1", "x0", "unused"],
          new double[,]
          {
              { 3.0, 1.0, 100.0 },
              { 4.0, 2.0, 100.0 },
              { 5.0, 3.0, 100.0 }
          });

        expression.Evaluate(data).ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Evaluate_WritesIntoDestination()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();
        var data = DataFrame.FromMatrix(
          ["x0"],
          new double[,]
          {
              { 1.0 },
              { 2.0 }
          });
        var destination = new[] { double.NaN, double.NaN, 42.0 };

        expression.Evaluate(data, destination);

        destination.ShouldBe([1.0, 2.0, 42.0]);
    }

    [Fact]
    public void Evaluate_UsesCallerProvidedWorkspace()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Fixed(2.0))
          .Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0 })
        ]);
        var destination = new[] { double.NaN, double.NaN };
        var workspace = new double[SymbolicExpressionInterpreter.GetWorkspaceLength(expression, data)];

        expression.Evaluate(data, destination, workspace);

        destination.ShouldBe([3.0, 4.0]);
    }

    [Fact]
    public void Evaluate_ReturnsEmptyResultForEmptyDataFrame()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Fixed(2.0))
          .Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", Array.Empty<double>())
        ]);

        expression.Evaluate(data).ShouldBeEmpty();
    }

    [Fact]
    public void Evaluate_RejectsTooSmallWorkspace()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0 })
        ]);

        Should.Throw<ArgumentException>(() => expression.Evaluate(data, new double[2], new double[1]));
    }

    [Fact]
    public void Evaluate_RejectsTooSmallDestination()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0 })
        ]);

        Should.Throw<ArgumentException>(() => expression.Evaluate(data, new double[1]));
    }

    [Fact]
    public void Evaluate_RejectsMissingNamedColumn()
    {
        var expression = ExpressionDraft.Variable("x0").Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x1", new[] { 1.0 })
        ]);

        Should.Throw<ArgumentException>(() => expression.Evaluate(data));
    }

    [Fact]
    public void WithOpCode_ReplacesSameArityOperationAndKeepsOriginalExpression()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile();

        var location = expression.TraversePostOrder().Single(node => node.OpCode == SymbolicExpressionOpCode.Add).Location;

        var edited = expression.WithOpCode(location, SymbolicExpressionOpCode.Subtract);

        expression.ToInfixString().ShouldBe("(x0 + x1)");
        edited.ToInfixString().ShouldBe("(x0 - x1)");
        edited.Evaluate(["x0", "x1"], [5.0, 2.0]).ShouldBe(3.0);
    }

    [Fact]
    public void WithOpCode_RejectsArityChangingOperation()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x1"))
          .Compile();

        var location = expression.TraversePostOrder().Single(node => node.OpCode == SymbolicExpressionOpCode.Add).Location;

        Should.Throw<ArgumentException>(() => expression.WithOpCode(location, SymbolicExpressionOpCode.Sqrt));
    }

    [Fact]
    public void WithNumericLiteral_ReplacesSingleLiteralOccurrence()
    {
        var expression = SymbolicExpression.Create(
          [
              ExpressionInstruction.NumericLiteral(0),
              ExpressionInstruction.NumericLiteral(0),
              ExpressionInstruction.Binary(SymbolicExpressionOpCode.Add, 1, 1)
          ],
          [new NumericLiteral(1.0, NumericLiteralKind.Fixed)],
          []);
        var secondLiteral = expression.TraversePostOrder()
          .Where(node => node.OpCode == SymbolicExpressionOpCode.NumericLiteral)
          .Skip(1)
          .Single()
          .Location;

        var edited = expression.WithNumericLiteral(secondLiteral, new NumericLiteral(4.0, NumericLiteralKind.Fixed));

        expression.ToInfixString().ShouldBe("(1 + 1)");
        edited.ToInfixString().ShouldBe("(1 + 4)");
        GetNumericLiterals(edited).ShouldBe([
          new NumericLiteral(1.0, NumericLiteralKind.Fixed),
          new NumericLiteral(4.0, NumericLiteralKind.Fixed)
        ]);
    }

    [Fact]
    public void WithNumericLiteral_WithValueCreatesOptimizableLiteral()
    {
        var expression = ExpressionDraft.Fixed(1.0).Compile();

        var edited = expression.WithNumericLiteral(expression.RootLocation, 2.0);

        edited.Root.TryGetNumericLiteral(out var literal).ShouldBeTrue();
        literal.ShouldBe(new NumericLiteral(2.0, NumericLiteralKind.Optimizable));
    }

    [Fact]
    public void WithVariable_ReplacesSingleVariableOccurrenceAndCompactsVariableReferences()
    {
        var expression = ExpressionDraft
          .Add(ExpressionDraft.Variable("x0"), ExpressionDraft.Variable("x0"))
          .Compile();
        var secondVariable = expression.TraversePostOrder()
          .Where(node => node.OpCode == SymbolicExpressionOpCode.Variable)
          .Skip(1)
          .Single()
          .Location;

        var edited = expression.WithVariable(secondVariable, "x1");

        expression.ToInfixString().ShouldBe("(x0 + x0)");
        edited.ToInfixString().ShouldBe("(x0 + x1)");
        GetVariableNames(edited).ShouldBe(["x0", "x1"]);
    }

    [Fact]
    public void ReplaceSubExpression_ReplacesSubtreeAndCompactsPayloadTables()
    {
        var expression = CreateLinearExpression();
        var replacement = ExpressionDraft.Variable("x2").Compile();

        var multiply = expression.TraversePostOrder().Single(node => node.OpCode == SymbolicExpressionOpCode.Multiply).Location;

        var edited = expression.ReplaceSubExpression(multiply, replacement);

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        edited.ToInfixString().ShouldBe("(x0 + x2)");
        GetVariableNames(edited).ShouldBe(["x0", "x2"]);
        GetNumericLiterals(edited).ShouldBeEmpty();
        edited.Evaluate(["x0", "x2"], [3.0, 4.0]).ShouldBe(7.0);
    }

    private static SymbolicExpression CreateLinearExpression()
    {
        return ExpressionDraft
          .Add(
            ExpressionDraft.Variable("x0"),
            ExpressionDraft.Multiply(
              ExpressionDraft.Fixed(2.0),
              ExpressionDraft.Variable("x1")))
          .Compile();
    }

    private static void ChildAtRoot(SymbolicExpression expression, int index)
    {
        expression.Root.Child(index);
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
