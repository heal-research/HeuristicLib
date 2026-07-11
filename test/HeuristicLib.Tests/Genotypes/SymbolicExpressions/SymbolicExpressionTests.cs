using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random.Distributions;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionTests
{
    [Fact]
    public void Create_RejectsMalformedPostOrderTokens()
    {
        Should.Throw<ArgumentException>(() => ExpressionTree.Create([new ExpressionNode(new AdditionSymbol())]));
    }

    [Fact]
    public void Create_RejectsEmptyNodeSequence()
    {
        Should.Throw<ArgumentException>(() => ExpressionTree.Create([]));
    }

    [Fact]
    public void Create_RejectsMultipleRootExpressions()
    {
        var x0 = Variable("x0").Build().Root.Node;
        var x1 = Variable("x1").Build().Root.Node;

        Should.Throw<ArgumentException>(() => ExpressionTree.Create([x0, x1]));
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
    public void Navigation_UsesTokenArityAndSubtreeMetadata()
    {
        var expression = (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();

        expression.Length.ShouldBe(5);
        expression.Depth.ShouldBe(3);
        expression.Root.Symbol.ShouldBe(new AdditionSymbol());
        expression.Root.Child(1).Symbol.ShouldBe(new MultiplicationSymbol());
        expression.TraversePreOrder().Select(node => node.Symbol).ShouldBe([
            new AdditionSymbol(), new VariableSymbol(["x0"]), new MultiplicationSymbol(), new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])
        ]);
    }

    [Fact]
    public void RootAndChildren_ExposeTheLogicalTree()
    {
        var root = CreateLinearExpression().Root;

        root.Length.ShouldBe(5);
        root.Arity.ShouldBe(2);
        root.Child(0).TryGetVariableReference(out var left).ShouldBeTrue();
        root.Child(1).Symbol.ShouldBe(Symbols.Multiplication);
        root.Child(1).Child(0).TryGetConstantValue(out var literal).ShouldBeTrue();
        root.Child(1).Child(1).TryGetVariableReference(out var right).ShouldBeTrue();
        left.Name.ShouldBe("x0");
        literal.ShouldBe(2.0);
        right.Name.ShouldBe("x1");
    }

    [Fact]
    public void Root_ReturnsTheWholeExpressionAsASubtree()
    {
        var root = CreateLinearExpression().Root;

        root.Length.ShouldBe(5);
        root.Symbol.ShouldBe(Symbols.Addition);
        root.Arity.ShouldBe(2);
    }

    [Fact]
    public void Child_NavigatesUnarySubtrees()
    {
        var root = Sqrt(Variable("x0")).Build().Root;

        root.Symbol.ShouldBe(Symbols.SquareRoot);
        root.Arity.ShouldBe(1);
        root.Child(0).TryGetVariableReference(out var variable).ShouldBeTrue();
        variable.Name.ShouldBe("x0");
    }

    [Fact]
    public void Child_PreservesLeftToRightOrderAndRejectsInvalidIndexes()
    {
        var root = (Variable("left") - Variable("right")).Build().Root;

        root.Child(0).TryGetVariableReference(out var left).ShouldBeTrue();
        root.Child(1).TryGetVariableReference(out var right).ShouldBeTrue();
        left.Name.ShouldBe("left");
        right.Name.ShouldBe("right");
        Should.Throw<ArgumentOutOfRangeException>(() => root.Child(2));
        Should.Throw<ArgumentOutOfRangeException>(() => Variable("x0").Build().Root.Child(0));
    }

    [Fact]
    public void Traversals_UseTheExpectedOrdersAndSubtreeLengths()
    {
        var expression = CreateLinearExpression();

        expression.TraversePostOrder().Select(node => node.Symbol).ShouldBe([
            new VariableSymbol(["x0"]), new FixedConstantSymbol(2.0), new VariableSymbol(["x1"]), Symbols.Multiplication, Symbols.Addition
        ]);
        expression.TraversePostOrder().Select(node => node.SubtreeLength).ShouldBe([1, 1, 1, 3, 5]);
        expression.TraversePreOrder().Select(node => node.Symbol).ShouldBe([
            Symbols.Addition, new VariableSymbol(["x0"]), Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])
        ]);
        expression.TraverseBreadthFirst().Select(node => node.Symbol).ShouldBe([
            Symbols.Addition, new VariableSymbol(["x0"]), Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])
        ]);
        expression.Root.Child(1).TraversePreOrder().Select(node => node.Symbol).ShouldBe([
            Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])
        ]);
    }

    [Fact]
    public void TraversePostOrder_ProvidesLocationsForImmutableEdits()
    {
        var expression = CreateLinearExpression();
        var location = expression.TraversePostOrder().Last().Location;

        expression.WithNode(location, new ExpressionNode(Symbols.Subtraction)).ToInfixString().ShouldBe("(x0 - (2 * x1))");
    }

    [Fact]
    public void TraversePreOrder_VisitsTheRootBeforeItsChildren()
    {
        CreateLinearExpression().TraversePreOrder().Select(node => node.Symbol).ShouldBe([
            Symbols.Addition, new VariableSymbol(["x0"]), Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])
        ]);
    }

    [Fact]
    public void TraverseBreadthFirst_VisitsTheTreeLevelByLevel()
    {
        CreateLinearExpression().TraverseBreadthFirst().Select(node => node.Symbol).ShouldBe([
            Symbols.Addition, new VariableSymbol(["x0"]), Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])
        ]);
    }

    [Fact]
    public void SubtreeTraversal_StaysWithinTheSelectedBranch()
    {
        var branch = CreateLinearExpression().Root.Child(1);

        branch.TraversePostOrder().Select(node => node.Symbol).ShouldBe([new FixedConstantSymbol(2.0), new VariableSymbol(["x1"]), Symbols.Multiplication]);
        branch.TraversePreOrder().Select(node => node.Symbol).ShouldBe([Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])]);
    }

    [Fact]
    public void TryGetHelpers_ReturnFalseForTheWrongTerminalKind()
    {
        var root = CreateLinearExpression().Root;

        root.TryGetConstantValue(out _).ShouldBeFalse();
        root.Child(1).Child(0).TryGetVariableReference(out _).ShouldBeFalse();
    }

    [Fact]
    public void Equality_IncludesSymbolConfiguration()
    {
        var narrow = new EvolvableConstantSymbol(new UniformDoubleDistribution(-1, 1), new ResampleInitialNumericPerturbation());
        var broad = new EvolvableConstantSymbol(new UniformDoubleDistribution(-10, 10), new ResampleInitialNumericPerturbation());

        Constant(0.5, narrow).Build().ShouldNotBe(Constant(0.5, broad).Build());
        Constant(0.5, narrow).Build().ShouldBe(Constant(0.5, new EvolvableConstantSymbol(new UniformDoubleDistribution(-1, 1), new ResampleInitialNumericPerturbation())).Build());
        FixedConstant(1.0).Build().ShouldNotBe(Constant(1.0).Build());
    }

    [Fact]
    public void WithNode_RequiresMatchingArity()
    {
        var expression = (Variable("x0") + Variable("x1")).Build();

        Should.Throw<ArgumentException>(() => expression.WithNode(expression.RootLocation, new ExpressionNode(new SquareRootSymbol())));
    }

    [Fact]
    public void WithNode_ReplacesSameAritySymbolWithoutMutatingTheOriginal()
    {
        var expression = (Variable("x0") + Variable("x1")).Build();

        var edited = expression.WithNode(expression.RootLocation, new ExpressionNode(Symbols.Subtraction));

        expression.ToInfixString().ShouldBe("(x0 + x1)");
        edited.ToInfixString().ShouldBe("(x0 - x1)");
        edited.EvaluateSingleRow(("x0", 5.0), ("x1", 2.0)).ShouldBe(3.0);
    }

    [Fact]
    public void WithVariableAndWithConstant_ReplaceOnlyTheSelectedNode()
    {
        var variables = new VariableSymbol(["x0", "x1"]);
        var expression = (Variable("x0", variables) + Variable("x0", variables)).Build();
        var secondVariable = expression.TraversePostOrder().Last().Child(1).Location;
        var editedVariable = expression.WithVariable(secondVariable, variables, "x1");
        var literalExpression = (FixedConstant(1.0) + FixedConstant(1.0)).Build();
        var secondLiteral = literalExpression.Root.Child(1).Location;
        var editedLiteral = literalExpression.WithConstant(secondLiteral, new FixedConstantSymbol(4.0), 4.0);

        expression.ToInfixString().ShouldBe("(x0 + x0)");
        editedVariable.ToInfixString().ShouldBe("(x0 + x1)");
        literalExpression.ToInfixString().ShouldBe("(1 + 1)");
        editedLiteral.ToInfixString().ShouldBe("(1 + 4)");
    }

    [Fact]
    public void ReplaceSubtree_ReplacesOnlyTheSelectedBranch()
    {
        var expression = CreateLinearExpression();
        var edited = expression.ReplaceSubtree(expression.Root.Child(1).Location, Variable("x2").Build());

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        edited.ToInfixString().ShouldBe("(x0 + x2)");
        edited.EvaluateSingleRow(("x0", 3.0), ("x2", 4.0)).ShouldBe(7.0);
    }

    [Fact]
    public void Evaluate_UsesVariableNamesAndAppliesOperations()
    {
        CreateLinearExpression().EvaluateSingleRow(("x1", 5.0), ("x0", 3.0)).ShouldBe(13.0);
        Sqrt(Log(Variable("x0") / FixedConstant(Math.E))).Build().EvaluateSingleRow(("x0", Math.E * Math.E)).ShouldBe(1.0, tolerance: 1e-12);
    }

    [Fact]
    public void Evaluate_RejectsMissingAndDuplicateVariableBindings()
    {
        var expression = Variable("x0").Build();

        Should.Throw<ArgumentException>(() => expression.Evaluate(DataFrame.FromOwnedColumns([])));
        Should.Throw<ArgumentException>(() => expression.Evaluate(DataFrame.FromOwnedColumns([KeyValuePair.Create("x1", new[] { 1.0 })])));
        Should.Throw<ArgumentException>(() => expression.EvaluateSingleRow(("x1", 1.0)));
        Should.Throw<ArgumentException>(() => expression.EvaluateSingleRow(("x0", 1.0), ("x0", 2.0)));
    }

    [Fact]
    public void Evaluate_UsesColumnOrderAndCallerProvidedBuffers()
    {
        var data = DataFrame.FromMatrix(["x0", "x1"], new double[,] { { 1.0, 3.0 }, { 2.0, 4.0 }, { 3.0, 5.0 } });
        var destination = new[] { double.NaN, double.NaN, double.NaN, 42.0 };
        var compiled = CreateLinearExpression().Compile();
        var workspace = new double[ExpressionInterpreter.GetWorkspaceLength(compiled, data)];

        CreateLinearExpression().Evaluate(data, destination, workspace);

        destination.ShouldBe([7.0, 10.0, 13.0, 42.0]);
    }

    [Fact]
    public void Evaluate_UsesTheDataFrameColumnOrder()
    {
        var data = DataFrame.FromMatrix(["x0", "x1"], new double[,] { { 1.0, 3.0 }, { 2.0, 4.0 }, { 3.0, 5.0 } });

        CreateLinearExpression().Evaluate(data).ShouldBe([7.0, 10.0, 13.0]);
    }

    [Fact]
    public void Evaluate_WritesOnlyTheExpressionRowsIntoTheDestination()
    {
        var data = DataFrame.FromOwnedColumns([KeyValuePair.Create("x0", new[] { 1.0, 2.0 })]);
        var destination = new[] { double.NaN, double.NaN, 42.0 };

        Variable("x0").Build().Evaluate(data, destination);

        destination.ShouldBe([1.0, 2.0, 42.0]);
    }

    [Fact]
    public void Evaluate_UsesCallerProvidedWorkspace()
    {
        var expression = (Variable("x0") + FixedConstant(2.0)).Build();
        var data = DataFrame.FromOwnedColumns([KeyValuePair.Create("x0", new[] { 1.0, 2.0 })]);
        var destination = new[] { double.NaN, double.NaN };
        var workspace = new double[ExpressionInterpreter.GetWorkspaceLength(expression.Compile(), data)];

        expression.Evaluate(data, destination, workspace);

        destination.ShouldBe([3.0, 4.0]);
    }

    [Fact]
    public void Evaluate_TerminalsRequireNoWorkspaceAndRejectInvalidBuffers()
    {
        var data = DataFrame.FromOwnedColumns([KeyValuePair.Create("x0", new[] { 1.0, 2.0 })]);
        var terminal = Variable("x0").Build();
        var expression = Variable("x0") + FixedConstant(2.0);
        var compiled = expression.Build().Compile();

        ExpressionInterpreter.GetWorkspaceLength(terminal.Compile(), data).ShouldBe(0);
        terminal.Evaluate(data, new double[2], []);
        Should.Throw<ArgumentException>(() => terminal.Evaluate(data, new double[1]));
        Should.Throw<ArgumentException>(() => expression.Build().Evaluate(data, new double[2], new double[ExpressionInterpreter.GetWorkspaceLength(compiled, data) - 1]));
    }

    [Fact]
    public void Evaluate_TerminalExpressionsDoNotRequireWorkspace()
    {
        var data = DataFrame.FromOwnedColumns([KeyValuePair.Create("x0", new[] { 1.0, 2.0 })]);
        var destination = new double[2];

        Variable("x0").Build().Evaluate(data, destination, []);

        destination.ShouldBe([1.0, 2.0]);
    }

    [Fact]
    public void Evaluate_RejectsDestinationAndWorkspaceBuffersThatAreTooSmall()
    {
        var data = DataFrame.FromOwnedColumns([KeyValuePair.Create("x0", Enumerable.Range(0, 4097).Select(value => (double)value).ToArray())]);
        var expression = (Variable("x0") + FixedConstant(2.0)).Build();
        var workspaceLength = ExpressionInterpreter.GetWorkspaceLength(expression.Compile(), data);

        Should.Throw<ArgumentException>(() => expression.Evaluate(data, new double[4096]));
        Should.Throw<ArgumentException>(() => expression.Evaluate(data, new double[4097], new double[workspaceLength - 1]));
    }

    [Fact]
    public void Evaluate_AppliesScalarVectorOperationsInBothOperandOrders()
    {
        var data = DataFrame.FromOwnedColumns([KeyValuePair.Create("x0", new[] { 1.0, 2.0, 3.0 })]);

        (FixedConstant(10.0) - Variable("x0")).Build().Evaluate(data).ShouldBe([9.0, 8.0, 7.0]);
        (FixedConstant(12.0) / Variable("x0")).Build().Evaluate(data).ShouldBe([12.0, 6.0, 4.0]);
        (Variable("x0") - FixedConstant(1.0)).Build().Evaluate(data).ShouldBe([0.0, 1.0, 2.0]);
    }

    [Fact]
    public void Evaluate_ProcessesBatchesBeyondTheDefaultBatchSize()
    {
        CreateLinearExpression().Evaluate(CreateLinearData(8193)).ShouldBe(Enumerable.Range(0, 8193).Select(row => 2.0 * row).ToArray());
    }

    [Fact]
    public void Evaluate_ProcessesTheFinalPartialBatchWithoutOverwritingTheBufferTail()
    {
        var data = CreateLinearData(4097);
        var destination = Enumerable.Repeat(-1.0, 4098).ToArray();
        var compiled = CreateLinearExpression().Compile();
        var workspace = new double[ExpressionInterpreter.GetWorkspaceLength(compiled, data)];

        CreateLinearExpression().Evaluate(data, destination, workspace);

        destination[0].ShouldBe(0.0);
        destination[4095].ShouldBe(8190.0);
        destination[4096].ShouldBe(8192.0);
        destination[4097].ShouldBe(-1.0);
    }

    private static ExpressionTree CreateLinearExpression() =>
        (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();

    private static DataFrame CreateLinearData(int rowCount) => DataFrame.FromOwnedColumns([
        KeyValuePair.Create("x0", Enumerable.Range(0, rowCount).Select(row => (double)row).ToArray()),
        KeyValuePair.Create("x1", Enumerable.Range(0, rowCount).Select(row => row * 0.5).ToArray())
    ]);
}
