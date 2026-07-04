using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

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
        var expression = (Variable("left") - Variable("right")).Compile();

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
        var expression = Sqrt(Variable("x0")).Compile();

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
        var expression = Variable("x0").Compile();

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
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Compile();

        expression.Evaluate([3.0, 5.0]).ShouldBe(13.0);
    }

    [Fact]
    public void Evaluate_UsesVariableNames()
    {
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Compile();

        expression.Evaluate(["x1", "x0"], [5.0, 3.0]).ShouldBe(13.0);
    }

    [Fact]
    public void Evaluate_AppliesUnaryAndBinaryOperations()
    {
        var expression = Sqrt(Log(Variable("x0") / Fixed(Math.E))).Compile();

        expression.Evaluate(["x0"], [Math.E * Math.E]).ShouldBe(1.0, tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_RejectsWrongVariableValueCount()
    {
        var expression = Variable("x0").Compile();

        Should.Throw<ArgumentException>(() => expression.Evaluate([]));
    }

    [Fact]
    public void Evaluate_RejectsMissingNamedVariable()
    {
        var expression = Variable("x0").Compile();

        Should.Throw<ArgumentException>(() => expression.Evaluate(["x1"], [1.0]));
    }

    [Fact]
    public void Evaluate_RejectsDuplicateNamedVariable()
    {
        var expression = Variable("x0").Compile();

        Should.Throw<ArgumentException>(() => expression.Evaluate(["x0", "x0"], [1.0, 2.0]));
    }

    [Fact]
    public void Evaluate_UsesVariableTableColumnOrder()
    {
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Compile();
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
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Compile();
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
        var expression = (Variable("x0") + Fixed(2.0) * Variable("x1")).Compile();
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
        var expression = Variable("x0").Compile();
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
        var expression = (Variable("x0") + Fixed(2.0)).Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0 })
        ]);
        var destination = new[] { double.NaN, double.NaN };
        var workspace = new double[SymbolicExpressionInterpreter.GetWorkspaceLength(expression, data)];

        expression.Evaluate(data, destination, workspace);

        destination.ShouldBe([3.0, 4.0]);
    }

    [Fact]
    public void Evaluate_VariableExpressionDoesNotRequireWorkspace()
    {
        var expression = Variable("x0").Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0 })
        ]);
        var destination = new[] { double.NaN, double.NaN };

        SymbolicExpressionInterpreter.GetWorkspaceLength(expression, data).ShouldBe(0);
        expression.Evaluate(data, destination, []);

        destination.ShouldBe([1.0, 2.0]);
    }

    [Fact]
    public void Evaluate_NumericLiteralExpressionDoesNotRequireWorkspace()
    {
        var expression = Fixed(4.0).Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("unused", new[] { 1.0, 2.0, 3.0 })
        ]);
        var destination = new[] { double.NaN, double.NaN, double.NaN };

        SymbolicExpressionInterpreter.GetWorkspaceLength(expression, data).ShouldBe(0);
        expression.Evaluate(data, destination, []);

        destination.ShouldBe([4.0, 4.0, 4.0]);
    }

    [Fact]
    public void Evaluate_AppliesScalarVectorOperationsInBothOperandOrders()
    {
        var scalarMinusVariable = (Fixed(10.0) - Variable("x0")).Compile();
        var scalarDivideVariable = (Fixed(12.0) / Variable("x0")).Compile();
        var variableMinusScalar = (Variable("x0") - Fixed(1.0)).Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0, 3.0 })
        ]);

        scalarMinusVariable.Evaluate(data).ShouldBe([9.0, 8.0, 7.0]);
        scalarDivideVariable.Evaluate(data).ShouldBe([12.0, 6.0, 4.0]);
        variableMinusScalar.Evaluate(data).ShouldBe([0.0, 1.0, 2.0]);
    }

    [Fact]
    public void Evaluate_KeepsScalarOnlySubExpressionOutOfWorkspace()
    {
        var expression = ((Fixed(2.0) + Fixed(3.0)) * Variable("x0")).Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0, 3.0 })
        ]);

        SymbolicExpressionInterpreter.GetWorkspaceLength(expression, data).ShouldBe(3);
        expression.Evaluate(data).ShouldBe([5.0, 10.0, 15.0]);
    }

    [Fact]
    public void Evaluate_UsesBatchesBeyondDefaultBatchSize()
    {
        var expression = CreateLinearExpression();
        var data = CreateLinearData(8193);

        var result = expression.Evaluate(data);

        result.ShouldBe(Enumerable.Range(0, 8193).Select(row => 2.0 * row).ToArray());
    }

    [Fact]
    public void GetWorkspaceLength_UsesLazyScratchSlotsAndCapsAtDefaultBatchSize()
    {
        var expression = CreateLinearExpression();
        var data = CreateLinearData(4097);

        var workspaceLength = SymbolicExpressionInterpreter.GetWorkspaceLength(expression, data);

        workspaceLength.ShouldBe(2 * 4096);
    }

    [Fact]
    public void Evaluate_WritesOnlyActualRowsInFinalPartialBatch()
    {
        var expression = CreateLinearExpression();
        var data = CreateLinearData(4097);
        var destination = Enumerable.Repeat(-1.0, 4098).ToArray();
        var workspace = new double[SymbolicExpressionInterpreter.GetWorkspaceLength(expression, data)];

        expression.Evaluate(data, destination, workspace);

        destination[0].ShouldBe(0.0);
        destination[4095].ShouldBe(8190.0);
        destination[4096].ShouldBe(8192.0);
        destination[4097].ShouldBe(-1.0);
    }

    [Fact]
    public void Evaluate_ReturnsEmptyResultForEmptyDataFrame()
    {
        var expression = (Variable("x0") + Fixed(2.0)).Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", Array.Empty<double>())
        ]);

        expression.Evaluate(data).ShouldBeEmpty();
    }

    [Fact]
    public void Evaluate_RejectsTooSmallWorkspace()
    {
        var expression = (Variable("x0") + Fixed(2.0)).Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", Enumerable.Range(0, 4097).Select(row => (double)row).ToArray())
        ]);
        var workspaceLength = SymbolicExpressionInterpreter.GetWorkspaceLength(expression, data);

        Should.Throw<ArgumentException>(() => expression.Evaluate(data, new double[4097], new double[workspaceLength - 1]));
    }

    [Fact]
    public void Evaluate_RejectsTooSmallDestination()
    {
        var expression = Variable("x0").Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0 })
        ]);

        Should.Throw<ArgumentException>(() => expression.Evaluate(data, new double[1]));
    }

    [Fact]
    public void Evaluate_RejectsMissingNamedColumn()
    {
        var expression = Variable("x0").Compile();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x1", new[] { 1.0 })
        ]);

        Should.Throw<ArgumentException>(() => expression.Evaluate(data));
    }

    [Fact]
    public void WithOpCode_ReplacesSameArityOperationAndKeepsOriginalExpression()
    {
        var expression = (Variable("x0") + Variable("x1")).Compile();

        var location = expression.TraversePostOrder().Single(node => node.OpCode == SymbolicExpressionOpCode.Add).Location;

        var edited = expression.WithOpCode(location, SymbolicExpressionOpCode.Subtract);

        expression.ToInfixString().ShouldBe("(x0 + x1)");
        edited.ToInfixString().ShouldBe("(x0 - x1)");
        edited.Evaluate(["x0", "x1"], [5.0, 2.0]).ShouldBe(3.0);
    }

    [Fact]
    public void WithOpCode_RejectsArityChangingOperation()
    {
        var expression = (Variable("x0") + Variable("x1")).Compile();

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
        var expression = Fixed(1.0).Compile();

        var edited = expression.WithNumericLiteral(expression.RootLocation, 2.0);

        edited.Root.TryGetNumericLiteral(out var literal).ShouldBeTrue();
        literal.ShouldBe(new NumericLiteral(2.0, NumericLiteralKind.Optimizable));
    }

    [Fact]
    public void WithVariable_ReplacesSingleVariableOccurrenceAndCompactsVariableReferences()
    {
        var expression = (Variable("x0") + Variable("x0")).Compile();
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
        var replacement = Variable("x2").Compile();

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
        return (Variable("x0") + Fixed(2.0) * Variable("x1")).Compile();
    }

    private static DataFrame CreateLinearData(int rowCount)
    {
        return DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", Enumerable.Range(0, rowCount).Select(row => (double)row).ToArray()),
          KeyValuePair.Create("x1", Enumerable.Range(0, rowCount).Select(row => row * 0.5).ToArray())
        ]);
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
