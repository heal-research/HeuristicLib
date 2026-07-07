using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionTests
{
    [Fact]
    public void Create_RejectsEmptySymbolProgram()
    {
        Should.Throw<ArgumentException>(() => SymbolicExpression.Create([]));
    }

    [Fact]
    public void Create_RejectsStackUnderflow()
    {
        Should.Throw<ArgumentException>(() => SymbolicExpression.Create([new AddSymbol()]));
    }

    [Fact]
    public void Create_RejectsMultipleRootExpressions()
    {
        Should.Throw<ArgumentException>(() =>
          SymbolicExpression.Create([
            new VariableSymbol("x0"),
            new VariableSymbol("x1")
          ]));
    }

    [Fact]
    public void Create_CalculatesLengthDepthAndComplexity()
    {
        var expression = CreateLinearExpression();

        expression.Length.ShouldBe(5);
        expression.Complexity.ShouldBe(5);
        expression.Depth.ShouldBe(3);
    }

    [Fact]
    public void Root_ReturnsSubExpressionOverWholeExpression()
    {
        var root = CreateLinearExpression().Root;

        root.Length.ShouldBe(5);
        root.Symbol.ShouldBe(new AddSymbol());
        root.Arity.ShouldBe(2);
    }

    [Fact]
    public void Child_NavigatesToSubExpression()
    {
        var root = CreateLinearExpression().Root;

        var left = root.Child(0);
        var right = root.Child(1);

        left.Symbol.ShouldBe(new VariableSymbol("x0"));
        left.TryGetVariableReference(out var leftVariable).ShouldBeTrue();
        leftVariable.Name.ShouldBe("x0");
        right.Symbol.ShouldBe(new MultiplySymbol());
        right.Length.ShouldBe(3);
        right.Child(0).TryGetNumericLiteral(out var literal).ShouldBeTrue();
        right.Child(1).TryGetVariableReference(out var rightVariable).ShouldBeTrue();
        literal.ShouldBe(new NumericLiteral(2.0, NumericLiteralKind.Fixed));
        rightVariable.Name.ShouldBe("x1");
    }

    [Fact]
    public void Child_PreservesLeftToRightOrder()
    {
        var root = (Variable("left") - Variable("right")).Build().Root;

        root.Symbol.ShouldBe(new SubtractSymbol());
        root.Child(0).TryGetVariableReference(out var leftVariable).ShouldBeTrue();
        root.Child(1).TryGetVariableReference(out var rightVariable).ShouldBeTrue();
        leftVariable.Name.ShouldBe("left");
        rightVariable.Name.ShouldBe("right");
    }

    [Fact]
    public void Child_NavigatesUnarySubExpression()
    {
        var root = Sqrt(Variable("x0")).Build().Root;

        root.Symbol.ShouldBe(new SqrtSymbol());
        root.Arity.ShouldBe(1);
        root.Child(0).Symbol.ShouldBe(new VariableSymbol("x0"));
    }

    [Fact]
    public void TraversePostOrder_EnumeratesEditableExpressionLocations()
    {
        var expression = CreateLinearExpression();
        var nodes = expression.TraversePostOrder().ToArray();

        nodes.Select(node => node.Symbol).ShouldBe([
          new VariableSymbol("x0"),
          new NumericLiteralSymbol(new NumericLiteral(2.0, NumericLiteralKind.Fixed)),
          new VariableSymbol("x1"),
          new MultiplySymbol(),
          new AddSymbol()
        ]);
        nodes.Select(node => node.SubtreeLength).ShouldBe([1, 1, 1, 3, 5]);
        expression.WithSymbol(nodes[^1].Location, new SubtractSymbol()).ToInfixString().ShouldBe("(x0 - (2 * x1))");
    }

    [Fact]
    public void TraversePreOrder_EnumeratesRootBeforeChildren()
    {
        CreateLinearExpression().TraversePreOrder().Select(node => node.Symbol).ShouldBe([
          new AddSymbol(),
          new VariableSymbol("x0"),
          new MultiplySymbol(),
          new NumericLiteralSymbol(new NumericLiteral(2.0, NumericLiteralKind.Fixed)),
          new VariableSymbol("x1")
        ]);
    }

    [Fact]
    public void TraverseBreadthFirst_EnumeratesLevelByLevel()
    {
        CreateLinearExpression().TraverseBreadthFirst().Select(node => node.Symbol).ShouldBe([
          new AddSymbol(),
          new VariableSymbol("x0"),
          new MultiplySymbol(),
          new NumericLiteralSymbol(new NumericLiteral(2.0, NumericLiteralKind.Fixed)),
          new VariableSymbol("x1")
        ]);
    }

    [Fact]
    public void SubExpressionTraversal_EnumeratesOnlySelectedSubtree()
    {
        var rightBranch = CreateLinearExpression().Root.Child(1);

        rightBranch.TraversePostOrder().Select(node => node.Symbol).ShouldBe([
          new NumericLiteralSymbol(new NumericLiteral(2.0, NumericLiteralKind.Fixed)),
          new VariableSymbol("x1"),
          new MultiplySymbol()
        ]);
        rightBranch.TraversePreOrder().Select(node => node.Symbol).ShouldBe([
          new MultiplySymbol(),
          new NumericLiteralSymbol(new NumericLiteral(2.0, NumericLiteralKind.Fixed)),
          new VariableSymbol("x1")
        ]);
    }

    [Fact]
    public void Child_RejectsIndexOutsideArity()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CreateLinearExpression().Root.Child(2));
        Should.Throw<ArgumentOutOfRangeException>(() => Variable("x0").Build().Root.Child(0));
    }

    [Fact]
    public void TryGetHelpers_ReturnFalseForWrongTerminalKind()
    {
        var root = CreateLinearExpression().Root;

        root.TryGetNumericLiteral(out _).ShouldBeFalse();
        root.Child(1).Child(0).TryGetVariableReference(out _).ShouldBeFalse();
    }

    [Fact]
    public void Equals_IncludesNumericLiteralKind()
    {
        var fixedExpression = Fixed(1.0).Build();
        var optimizableExpression = Parameter(1.0).Build();

        fixedExpression.ShouldNotBe(optimizableExpression);
    }

    [Fact]
    public void Evaluate_UsesVariableNames()
    {
        CreateLinearExpression().EvaluateSingleRow(("x1", 5.0), ("x0", 3.0)).ShouldBe(13.0);
    }

    [Fact]
    public void Evaluate_AppliesUnaryAndBinaryOperations()
    {
        Sqrt(Log(Variable("x0") / Fixed(Math.E))).Build().EvaluateSingleRow(("x0", Math.E * Math.E)).ShouldBe(1.0, tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_RejectsInvalidVariableBindings()
    {
        var expression = Variable("x0").Build();
        var emptyData = DataFrame.FromOwnedColumns([]);
        var wrongData = DataFrame.FromOwnedColumns([KeyValuePair.Create("x1", new[] { 1.0 })]);

        Should.Throw<ArgumentException>(() => expression.Evaluate(emptyData));
        Should.Throw<ArgumentException>(() => expression.Evaluate(wrongData));
        Should.Throw<ArgumentException>(() => expression.EvaluateSingleRow(("x1", 1.0)));
        Should.Throw<ArgumentException>(() => expression.EvaluateSingleRow(("x0", 1.0), ("x0", 2.0)));
    }

    [Fact]
    public void Evaluate_UsesVariableTableColumnOrder()
    {
        var data = DataFrame.FromMatrix(
          ["x0", "x1"],
          new double[,]
          {
              { 1.0, 3.0 },
              { 2.0, 4.0 },
              { 3.0, 5.0 }
          });

        CreateLinearExpression().Evaluate(data).ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Evaluate_WritesIntoDestination()
    {
        var data = DataFrame.FromMatrix(
          ["x0"],
          new double[,]
          {
              { 1.0 },
              { 2.0 }
          });
        var destination = new[] { double.NaN, double.NaN, 42.0 };

        Variable("x0").Build().Evaluate(data, destination);

        destination.ShouldBe([1.0, 2.0, 42.0]);
    }

    [Fact]
    public void Evaluate_UsesCallerProvidedWorkspace()
    {
        var expression = (Variable("x0") + Fixed(2.0)).Build();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0 })
        ]);
        var destination = new[] { double.NaN, double.NaN };
        var workspace = new double[SymbolicExpressionInterpreter.GetWorkspaceLength(expression.Compile(), data)];

        expression.Evaluate(data, destination, workspace);

        destination.ShouldBe([3.0, 4.0]);
    }

    [Fact]
    public void Evaluate_TerminalExpressionsDoNotRequireWorkspace()
    {
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0 })
        ]);
        var destination = new[] { double.NaN, double.NaN };

        SymbolicExpressionInterpreter.GetWorkspaceLength(Variable("x0").Build().Compile(), data).ShouldBe(0);
        Variable("x0").Build().Evaluate(data, destination, []);
        destination.ShouldBe([1.0, 2.0]);
    }

    [Fact]
    public void Evaluate_AppliesScalarVectorOperationsInBothOperandOrders()
    {
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", new[] { 1.0, 2.0, 3.0 })
        ]);

        (Fixed(10.0) - Variable("x0")).Build().Evaluate(data).ShouldBe([9.0, 8.0, 7.0]);
        (Fixed(12.0) / Variable("x0")).Build().Evaluate(data).ShouldBe([12.0, 6.0, 4.0]);
        (Variable("x0") - Fixed(1.0)).Build().Evaluate(data).ShouldBe([0.0, 1.0, 2.0]);
    }

    [Fact]
    public void Evaluate_UsesBatchesBeyondDefaultBatchSize()
    {
        CreateLinearExpression().Evaluate(CreateLinearData(8193)).ShouldBe(Enumerable.Range(0, 8193).Select(row => 2.0 * row).ToArray());
    }

    [Fact]
    public void Evaluate_WritesOnlyActualRowsInFinalPartialBatch()
    {
        var expression = CreateLinearExpression();
        var data = CreateLinearData(4097);
        var destination = Enumerable.Repeat(-1.0, 4098).ToArray();
        var workspace = new double[SymbolicExpressionInterpreter.GetWorkspaceLength(expression.Compile(), data)];

        expression.Evaluate(data, destination, workspace);

        destination[0].ShouldBe(0.0);
        destination[4095].ShouldBe(8190.0);
        destination[4096].ShouldBe(8192.0);
        destination[4097].ShouldBe(-1.0);
    }

    [Fact]
    public void Evaluate_RejectsInvalidFrameBuffers()
    {
        var expression = (Variable("x0") + Fixed(2.0)).Build();
        var data = DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", Enumerable.Range(0, 4097).Select(row => (double)row).ToArray())
        ]);
        var workspaceLength = SymbolicExpressionInterpreter.GetWorkspaceLength(expression.Compile(), data);

        Should.Throw<ArgumentException>(() => expression.Evaluate(data, new double[4097], new double[workspaceLength - 1]));
        Should.Throw<ArgumentException>(() => Variable("x0").Build().Evaluate(data, new double[1]));
        Should.Throw<ArgumentException>(() => Variable("missing").Build().Evaluate(data));
    }

    [Fact]
    public void WithSymbol_ReplacesSameArityOperationAndKeepsOriginalExpression()
    {
        var expression = (Variable("x0") + Variable("x1")).Build();
        var location = expression.TraversePostOrder().Single(node => node.Symbol is AddSymbol).Location;

        var edited = expression.WithSymbol(location, new SubtractSymbol());

        expression.ToInfixString().ShouldBe("(x0 + x1)");
        edited.ToInfixString().ShouldBe("(x0 - x1)");
        edited.EvaluateSingleRow(("x0", 5.0), ("x1", 2.0)).ShouldBe(3.0);
    }

    [Fact]
    public void WithSymbol_RejectsArityChangingReplacement()
    {
        var expression = (Variable("x0") + Variable("x1")).Build();
        var location = expression.TraversePostOrder().Single(node => node.Symbol is AddSymbol).Location;

        Should.Throw<ArgumentException>(() => expression.WithSymbol(location, new SqrtSymbol()));
    }

    [Fact]
    public void WithNumericLiteral_ReplacesSingleLiteralOccurrence()
    {
        var expression = (Fixed(1.0) + Fixed(1.0)).Build();
        var secondLiteral = expression.TraversePostOrder()
          .Where(node => node.Symbol is NumericLiteralSymbol)
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
    public void WithVariable_ReplacesSingleVariableOccurrence()
    {
        var expression = (Variable("x0") + Variable("x0")).Build();
        var secondVariable = expression.TraversePostOrder()
          .Where(node => node.Symbol is VariableSymbol)
          .Skip(1)
          .Single()
          .Location;

        var edited = expression.WithVariable(secondVariable, "x1");

        expression.ToInfixString().ShouldBe("(x0 + x0)");
        edited.ToInfixString().ShouldBe("(x0 + x1)");
        GetVariableNames(edited).ShouldBe(["x0", "x1"]);
    }

    [Fact]
    public void ReplaceSubExpression_ReplacesSubtree()
    {
        var expression = CreateLinearExpression();
        var replacement = Variable("x2").Build();
        var multiply = expression.TraversePostOrder().Single(node => node.Symbol is MultiplySymbol).Location;

        var edited = expression.ReplaceSubExpression(multiply, replacement);

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        edited.ToInfixString().ShouldBe("(x0 + x2)");
        GetVariableNames(edited).ShouldBe(["x0", "x2"]);
        GetNumericLiterals(edited).ShouldBeEmpty();
        edited.EvaluateSingleRow(("x0", 3.0), ("x2", 4.0)).ShouldBe(7.0);
    }

    private static SymbolicExpression CreateLinearExpression()
    {
        return (Variable("x0") + Fixed(2.0) * Variable("x1")).Build();
    }

    private static DataFrame CreateLinearData(int rowCount)
    {
        return DataFrame.FromOwnedColumns([
          KeyValuePair.Create("x0", Enumerable.Range(0, rowCount).Select(row => (double)row).ToArray()),
          KeyValuePair.Create("x1", Enumerable.Range(0, rowCount).Select(row => row * 0.5).ToArray())
        ]);
    }

    private static string[] GetVariableNames(SymbolicExpression expression)
    {
        return expression.TraversePostOrder()
          .Select(node => node.Symbol)
          .OfType<VariableSymbol>()
          .Select(symbol => symbol.VariableName)
          .ToArray();
    }

    private static NumericLiteral[] GetNumericLiterals(SymbolicExpression expression)
    {
        return expression.TraversePostOrder()
          .Select(node => node.Symbol)
          .OfType<NumericLiteralSymbol>()
          .Select(symbol => symbol.Literal)
          .ToArray();
    }
}
