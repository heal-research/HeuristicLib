using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionTests
{
    [Fact]
    public void Node_RejectsMissingOperationChildren()
    {
        Should.Throw<ArgumentException>(() => new ExpressionNode(new AdditionSymbol()));
    }

    [Fact]
    public void Node_RejectsAnIncorrectNumberOfChildren()
    {
        var child = Variable("x0").Build().Root;

        Should.Throw<ArgumentException>(() => new ExpressionNode(new AdditionSymbol(), child));
    }

    [Fact]
    public void Node_RequiresPayloadsForPayloadBearingSymbols()
    {
        Should.Throw<ArgumentException>(() => new ExpressionNode(new VariableSymbol(["x0"])));
        Should.Throw<ArgumentException>(() => new ExpressionNode(new FixedConstantSymbol(1.0)));
    }

    [Fact]
    public void SymbolCreateNode_CopiesTheSuppliedChildren()
    {
        var left = Variable("x0").Build().Root;
        var right = Variable("x1").Build().Root;
        var children = new[] { left, right };

        var node = Symbols.Addition.CreateNode(new SequenceRandomNumberGenerator(), children);

        node.Children.ShouldNotBeSameAs(children);
        children[0] = Variable("changed").Build().Root;
        node.Child(0).ShouldBeSameAs(left);
    }

    [Fact]
    public void FromOwnedChildren_StoresTheSuppliedArrayWithoutCopying()
    {
        var children = new[] { Variable("x0").Build().Root, Variable("x1").Build().Root };

        var node = ExpressionNode.FromOwnedChildren(Symbols.Addition, children);

        node.Children.ShouldBeSameAs(children);
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
    public void Navigation_UsesNodeArityAndSubtreeMetadata()
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
    public void PointIndexes_FollowNaturalRootFirstTreeOrder()
    {
        var expression = (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();

        Enumerable.Range(0, expression.Length)
            .Select(index => expression.GetPoint(index).Node.Symbol)
            .ShouldBe([
                Symbols.Addition,
                new VariableSymbol(["x0"]),
                Symbols.Multiplication,
                new FixedConstantSymbol(2.0),
                new VariableSymbol(["x1"])
            ]);
    }

    [Fact]
    public void RootAndChildren_ExposeTheLogicalTree()
    {
        var root = CreateLinearExpression().Root;

        root.Length.ShouldBe(5);
        root.Arity.ShouldBe(2);
        root.Child(0).TryGetVariableName(out var left).ShouldBeTrue();
        root.Child(1).Symbol.ShouldBe(Symbols.Multiplication);
        root.Child(1).Child(0).TryGetConstantValue(out var literal).ShouldBeTrue();
        root.Child(1).Child(1).TryGetVariableName(out var right).ShouldBeTrue();
        left.ShouldBe("x0");
        literal.ShouldBe(2.0);
        right.ShouldBe("x1");
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
    public void ExpressionPoint_IdentifiesAnOccurrenceByItsTreeAndParentPath()
    {
        var shared = Variable("x0").Build().Root;
        var expression = new ExpressionTree(new ExpressionNode(Symbols.Addition, shared, shared));
        var left = expression.RootPoint.Child(0);
        var right = expression.RootPoint.Child(1);

        ReferenceEquals(left.Node, right.Node).ShouldBeTrue();
        left.Tree.ShouldBeSameAs(expression);
        left.Parent.ShouldNotBeNull();
        left.Parent.IsRoot.ShouldBeTrue();
        left.Parent.Tree.ShouldBeSameAs(expression);
        left.Parent!.Node.ShouldBeSameAs(expression.Root);
        left.ChildIndex.ShouldBe(0);
        right.ChildIndex.ShouldBe(1);
        left.Depth.ShouldBe(1);
    }

    [Fact]
    public void ExpressionPoint_RebindsItsPathAndRejectsUseWithAnotherTree()
    {
        var expression = CreateLinearExpression();
        var point = expression.RootPoint.Child(1).Child(1);
        var edited = expression.WithConstant(expression.RootPoint.Child(1).Child(0), new FixedConstantSymbol(3.0), 3.0);
        var rebound = point.Rebind(edited);

        rebound.Tree.ShouldBeSameAs(edited);
        rebound.Node.TryGetVariableName(out var variableName).ShouldBeTrue();
        variableName.ShouldBe("x1");
        Should.Throw<ArgumentException>(() => edited.Replace(point, point.Node));
    }

    [Fact]
    public void Child_NavigatesUnarySubtrees()
    {
        var root = Sqrt(Variable("x0")).Build().Root;

        root.Symbol.ShouldBe(Symbols.SquareRoot);
        root.Arity.ShouldBe(1);
        root.Child(0).TryGetVariableName(out var variable).ShouldBeTrue();
        variable.ShouldBe("x0");
    }

    [Fact]
    public void Child_PreservesLeftToRightOrderAndRejectsInvalidIndexes()
    {
        var root = (Variable("left") - Variable("right")).Build().Root;

        root.Child(0).TryGetVariableName(out var left).ShouldBeTrue();
        root.Child(1).TryGetVariableName(out var right).ShouldBeTrue();
        left.ShouldBe("left");
        right.ShouldBe("right");
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
    public void PointTraversal_ProvidesPointsForImmutableEdits()
    {
        var expression = CreateLinearExpression();
        var point = expression.RootPoint.TraversePostOrder().Last();

        point.ReplaceWith(point.Node.WithSymbol(Symbols.Subtraction)).ToInfixString().ShouldBe("(x0 - (2 * x1))");
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
        root.Child(1).Child(0).TryGetVariableName(out _).ShouldBeFalse();
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
    public void Equality_IsStructuralAndProducesConsistentHashCodes()
    {
        var first = CreateLinearExpression();
        var second = CreateLinearExpression();
        var reordered = (Variable("x0") + Variable("x1") * FixedConstant(2.0)).Build();

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
        first.ShouldNotBe(reordered);
    }

    [Fact]
    public void WithSymbol_RequiresMatchingArity()
    {
        var expression = (Variable("x0") + Variable("x1")).Build();

        Should.Throw<ArgumentException>(() => expression.Root.WithSymbol(new SquareRootSymbol()));
    }

    [Fact]
    public void PointReplacement_ReplacesSameAritySymbolWithoutMutatingTheOriginal()
    {
        var expression = (Variable("x0") + Variable("x1")).Build();

        var edited = expression.RootPoint.ReplaceWith(expression.Root.WithSymbol(Symbols.Subtraction));

        expression.ToInfixString().ShouldBe("(x0 + x1)");
        edited.ToInfixString().ShouldBe("(x0 - x1)");
        edited.EvaluateSingleRow(("x0", 5.0), ("x1", 2.0)).ShouldBe(3.0);
    }

    [Fact]
    public void PointReplacement_CopiesOnlyTheEditedAncestorPath()
    {
        var expression = CreateLinearExpression();
        var point = expression.RootPoint.Child(1).Child(1);

        var edited = expression.WithVariable(point, new VariableSymbol(["x2"]), "x2");

        ReferenceEquals(expression.Root, edited.Root).ShouldBeFalse();
        ReferenceEquals(expression.Root.Child(0), edited.Root.Child(0)).ShouldBeTrue();
        ReferenceEquals(expression.Root.Child(1), edited.Root.Child(1)).ShouldBeFalse();
        ReferenceEquals(expression.Root.Child(1).Child(0), edited.Root.Child(1).Child(0)).ShouldBeTrue();
        ReferenceEquals(expression.Root.Child(1).Child(1), edited.Root.Child(1).Child(1)).ShouldBeFalse();
    }

    [Fact]
    public void PointReplacement_ReturnsTheOriginalTreeForAnEqualNode()
    {
        var expression = CreateLinearExpression();

        var edited = expression.RootPoint.ReplaceWith(expression.Root);

        ReferenceEquals(expression, edited).ShouldBeTrue();
    }

    [Fact]
    public void ReplaceMany_ReplacesSeveralOccurrencesAndSharesUnaffectedBranches()
    {
        var expression = (Variable("x0") + FixedConstant(2.0) * Variable("x1")).Build();
        var constantPoint = expression.RootPoint.Child(1).Child(0);
        var variablePoint = expression.RootPoint.Child(1).Child(1);

        var edited = expression.ReplaceMany([
            (constantPoint, FixedConstant(3.0).Build().Root),
            (variablePoint, Variable("x2").Build().Root)
        ]);

        edited.ToInfixString().ShouldBe("(x0 + (3 * x2))");
        edited.Root.Child(0).ShouldBeSameAs(expression.Root.Child(0));
        edited.Root.Child(1).ShouldNotBeSameAs(expression.Root.Child(1));
        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
    }

    [Fact]
    public void ReplaceMany_ReturnsTheOriginalTreeWhenEveryReplacementIsEqual()
    {
        var expression = CreateLinearExpression();

        var edited = expression.ReplaceMany([
            (expression.RootPoint.Child(0), expression.Root.Child(0)),
            (expression.RootPoint.Child(1), expression.Root.Child(1))
        ]);

        edited.ShouldBeSameAs(expression);
    }

    [Fact]
    public void ReplaceMany_RejectsForeignDuplicateAndOverlappingPoints()
    {
        var expression = CreateLinearExpression();
        var other = CreateLinearExpression();
        var point = expression.RootPoint.Child(1);
        var replacement = (FixedConstant(3.0) * Variable("x2")).Build().Root;

        Should.Throw<ArgumentException>(() => expression.ReplaceMany([(other.RootPoint, replacement)]));
        Should.Throw<ArgumentException>(() => expression.ReplaceMany([(point, replacement), (point, replacement)]));
        Should.Throw<ArgumentException>(() => expression.ReplaceMany([
            (point, replacement),
            (point.Child(0), FixedConstant(3.0).Build().Root)
        ]));
        Should.Throw<ArgumentException>(() => expression.ReplaceMany([
            (point.Child(0), FixedConstant(3.0).Build().Root),
            (point, replacement)
        ]));
    }

    [Fact]
    public void WithVariableAndWithConstant_ReplaceOnlyTheSelectedNode()
    {
        var variables = new VariableSymbol(["x0", "x1"]);
        var expression = (Variable("x0", variables) + Variable("x0", variables)).Build();
        var secondVariable = expression.RootPoint.Child(1);
        var editedVariable = expression.WithVariable(secondVariable, variables, "x1");
        var literalExpression = (FixedConstant(1.0) + FixedConstant(1.0)).Build();
        var secondLiteral = literalExpression.RootPoint.Child(1);
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
        var edited = expression.ReplaceSubtree(expression.RootPoint.Child(1), Variable("x2").Build());

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        edited.ToInfixString().ShouldBe("(x0 + x2)");
        edited.EvaluateSingleRow(("x0", 3.0), ("x2", 4.0)).ShouldBe(7.0);
    }

    [Fact]
    public void ReplaceSubtree_SharesTheUnaffectedBranchAndTheReplacementTree()
    {
        var expression = CreateLinearExpression();
        var replacement = (Variable("x2") - FixedConstant(1.0)).Build();

        var edited = expression.ReplaceSubtree(expression.RootPoint.Child(1), replacement);

        ReferenceEquals(expression.Root.Child(0), edited.Root.Child(0)).ShouldBeTrue();
        ReferenceEquals(replacement.Root, edited.Root.Child(1)).ShouldBeTrue();
    }

    [Fact]
    public void Create_SupportsSymbolsWithMoreThanTwoChildren()
    {
        var expression = ExpressionDraft.Apply(
            new SumThreeSymbol(),
            Variable("x0"),
            Variable("x1"),
            Variable("x2")).Build();

        expression.Length.ShouldBe(4);
        expression.Depth.ShouldBe(2);
        expression.Root.TraverseChildren().Select(child => child.VariableName).ShouldBe(["x0", "x1", "x2"]);
        expression.EvaluateSingleRow(("x0", 1.0), ("x1", 2.0), ("x2", 3.0)).ShouldBe(6.0);
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

    private sealed record SumThreeSymbol() : OperationSymbol("sum3", 3)
    {
        protected override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            emitter.EmitChild(0);
            emitter.EmitChild(1);
            emitter.EmitOperator(OpCode.Add);
            emitter.EmitChild(2);
            emitter.EmitOperator(OpCode.Add);
        }
    }
}
