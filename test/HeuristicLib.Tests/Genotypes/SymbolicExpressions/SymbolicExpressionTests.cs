using System.Reflection;
using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Numerics;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.Tests.TestSupport.Random;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionTests
{
    [Fact]
    public void NodeTypes_ExposeOnlyTheirNaturalChildShapes()
    {
        typeof(ExpressionNode).GetMethod(nameof(ExpressionNode.GetChild)).ShouldNotBeNull();
        typeof(ExpressionNode).GetProperty(nameof(ExpressionNode.Symbol), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.PropertyType.ShouldBe(typeof(Symbol));
        typeof(ExpressionNode).GetProperty("Children").ShouldBeNull();
        typeof(TerminalExpressionNode).GetProperty("Children").ShouldBeNull();
        typeof(TerminalExpressionNode).GetProperty(nameof(ExpressionNode.Symbol), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.PropertyType.ShouldBe(typeof(TerminalSymbol));
        typeof(PayloadlessTerminalExpressionNode).GetProperty(nameof(ExpressionNode.Symbol), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.PropertyType.ShouldBe(typeof(PayloadlessTerminalSymbol));
        typeof(OperationExpressionNode).GetProperty("Children").ShouldBeNull();
        typeof(OperationExpressionNode).GetProperty(nameof(ExpressionNode.Symbol), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.PropertyType.ShouldBe(typeof(OperationSymbol));
        typeof(VariableExpressionNode).GetProperty(nameof(ExpressionNode.Symbol), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.PropertyType.ShouldBe(typeof(VariableSymbol));
        typeof(NumericConstantExpressionNode).GetProperty(nameof(ExpressionNode.Symbol), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)!.PropertyType.ShouldBe(typeof(ConstantSymbol));

        typeof(UnaryExpressionNode).GetProperty(nameof(UnaryExpressionNode.Operand)).ShouldNotBeNull();
        typeof(UnaryExpressionNode).GetProperty("Children").ShouldBeNull();
        typeof(BinaryExpressionNode).GetProperty(nameof(BinaryExpressionNode.Left)).ShouldNotBeNull();
        typeof(BinaryExpressionNode).GetProperty(nameof(BinaryExpressionNode.Right)).ShouldNotBeNull();
        typeof(BinaryExpressionNode).GetProperty("Children").ShouldBeNull();
        typeof(NaryExpressionNode).GetProperty(nameof(NaryExpressionNode.Children)).ShouldNotBeNull();
    }

    [Fact]
    public void OperationNodes_RejectSymbolsWithDifferentArities()
    {
        var child = Variable("x0").Build().Root;

        Should.Throw<ArgumentException>(() => new UnaryExpressionNode(new AdditionSymbol(), child));
        Should.Throw<ArgumentException>(() => new BinaryExpressionNode(new SquareRootSymbol(), child, child));
        Should.Throw<ArgumentException>(() => new NaryExpressionNode(new SumThreeSymbol(), child, child));
    }

    [Fact]
    public void Node_RequiresPayloadsForPayloadBearingSymbols()
    {
        Should.Throw<ArgumentException>(() => new VariableExpressionNode(new VariableSymbol(["x0"]), "x1"));
    }

    [Fact]
    public void PayloadlessTerminalNode_SupportsCustomTerminalSymbols()
    {
        var symbol = new OneSymbol();
        var node = symbol.CreateNode(new SequenceRandomNumberGenerator());
        var expression = new ExpressionTree(node);

        node.GetType().ShouldBe(typeof(PayloadlessTerminalExpressionNode));
        node.Symbol.ShouldBe(symbol);
        node.Arity.ShouldBe(0);
        expression.EvaluateSingleRow(new Dictionary<string, double>()).ShouldBe(1.0);
    }

    [Fact]
    public void SymbolCreateNode_AcceptsImmutableChildren()
    {
        var left = Variable("x0").Build().Root;
        var right = Variable("x1").Build().Root;
        var children = ImmutableArray.Create(left, right);

        var node = Symbols.Addition.CreateNode(new SequenceRandomNumberGenerator(), children);

        var binary = node.ShouldBeOfType<BinaryExpressionNode>();
        binary.Left.ShouldBeSameAs(left);
        binary.Right.ShouldBeSameAs(right);
    }

    [Fact]
    public void SymbolCreateNode_SelectsTheSpecializedNodeType()
    {
        var random = new SequenceRandomNumberGenerator();
        var child = Variable("x0").Build().Root;

        Symbols.FixedConstant(1.0).CreateNode(random).ShouldBeOfType<NumericConstantExpressionNode>();
        Symbols.Variable(["x0"]).CreateNode(random).ShouldBeOfType<VariableExpressionNode>();
        Symbols.Negation.CreateNode(random, child).ShouldBeOfType<UnaryExpressionNode>();
        Symbols.Addition.CreateNode(random, child, child).ShouldBeOfType<BinaryExpressionNode>();
        new SumThreeSymbol().CreateNode(random, child, child, child).ShouldBeOfType<NaryExpressionNode>();
    }

    [Fact]
    public void NaryNode_AcceptsImmutableChildrenWithoutChangingTheirOrder()
    {
        var children = ImmutableArray.Create(
            Variable("x0").Build().Root,
            Variable("x1").Build().Root,
            FixedConstant(1.0).Build().Root);

        var node = new NaryExpressionNode(new SumThreeSymbol(), children);

        node.Children.ShouldBe(children);
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
        var root = expression.Root.ShouldBeOfType<BinaryExpressionNode>();
        root.Symbol.ShouldBe(new AdditionSymbol());
        root.Right.Symbol.ShouldBe(new MultiplicationSymbol());
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
        var root = CreateLinearExpression().Root.ShouldBeOfType<BinaryExpressionNode>();
        var multiplication = root.Right.ShouldBeOfType<BinaryExpressionNode>();

        root.Length.ShouldBe(5);
        root.Arity.ShouldBe(2);
        var left = root.Left.ShouldBeOfType<VariableExpressionNode>().VariableName;
        multiplication.Symbol.ShouldBe(Symbols.Multiplication);
        var literal = multiplication.Left.ShouldBeOfType<NumericConstantExpressionNode>().Value;
        var right = multiplication.Right.ShouldBeOfType<VariableExpressionNode>().VariableName;
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
        var expression = new ExpressionTree(new BinaryExpressionNode(Symbols.Addition, shared, shared));
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
        rebound.Node.ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("x1");
        Should.Throw<ArgumentException>(() => edited.Replace(point, point.Node));
    }

    [Fact]
    public void Child_NavigatesUnarySubtrees()
    {
        var root = Sqrt(Variable("x0")).Build().Root;

        root.Symbol.ShouldBe(Symbols.SquareRoot);
        root.Arity.ShouldBe(1);
        root.ShouldBeOfType<UnaryExpressionNode>().Operand.ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("x0");
    }

    [Fact]
    public void BinaryNode_ExposesLeftAndRightWhilePointsValidateChildIndexes()
    {
        var expression = (Variable("left") - Variable("right")).Build();
        var root = expression.Root.ShouldBeOfType<BinaryExpressionNode>();

        root.Left.ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("left");
        root.Right.ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("right");
        root.GetChild(0).ShouldBeSameAs(root.Left);
        root.GetChild(1).ShouldBeSameAs(root.Right);
        Should.Throw<ArgumentOutOfRangeException>(() => root.GetChild(2));
        Should.Throw<ArgumentOutOfRangeException>(() => Variable("x0").Build().Root.GetChild(0));
        Should.Throw<ArgumentOutOfRangeException>(() => expression.RootPoint.Child(2));
        Should.Throw<ArgumentOutOfRangeException>(() => expression.RootPoint.Child(0).Child(0));
    }

    [Fact]
    public void Traversals_UseTheExpectedOrdersAndSubtreeLengths()
    {
        var expression = CreateLinearExpression();

        expression.TraversePostOrder().Select(node => node.Symbol).ShouldBe([
            new VariableSymbol(["x0"]), new FixedConstantSymbol(2.0), new VariableSymbol(["x1"]), Symbols.Multiplication, Symbols.Addition
        ]);
        expression.TraversePostOrder().Select(node => node.Length).ShouldBe([1, 1, 1, 3, 5]);
        expression.TraversePreOrder().Select(node => node.Symbol).ShouldBe([
            Symbols.Addition, new VariableSymbol(["x0"]), Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])
        ]);
        expression.TraverseBreadthFirst().Select(node => node.Symbol).ShouldBe([
            Symbols.Addition, new VariableSymbol(["x0"]), Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])
        ]);
        expression.Root.ShouldBeOfType<BinaryExpressionNode>().Right.TraversePreOrder().Select(node => node.Symbol).ShouldBe([
            Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])
        ]);
    }

    [Fact]
    public void PointTraversal_ProvidesPointsForImmutableEdits()
    {
        var expression = CreateLinearExpression();
        var point = expression.RootPoint.TraversePostOrder().Last();

        var binary = point.Node.ShouldBeOfType<BinaryExpressionNode>();
        point.ReplaceWith(new BinaryExpressionNode(Symbols.Subtraction, binary.Left, binary.Right))
            .ToInfixString().ShouldBe("(x0 - (2 * x1))");
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
        var branch = CreateLinearExpression().Root.ShouldBeOfType<BinaryExpressionNode>().Right;

        branch.TraversePostOrder().Select(node => node.Symbol).ShouldBe([new FixedConstantSymbol(2.0), new VariableSymbol(["x1"]), Symbols.Multiplication]);
        branch.TraversePreOrder().Select(node => node.Symbol).ShouldBe([Symbols.Multiplication, new FixedConstantSymbol(2.0), new VariableSymbol(["x1"])]);
    }

    [Fact]
    public void TerminalSubtypesExposeOnlyTheirValidPayload()
    {
        var root = CreateLinearExpression().Root.ShouldBeOfType<BinaryExpressionNode>();
        var multiplication = root.Right.ShouldBeOfType<BinaryExpressionNode>();

        root.Left.ShouldBeOfType<VariableExpressionNode>();
        multiplication.Left.ShouldBeOfType<NumericConstantExpressionNode>();
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
    public void VariableSymbol_ReweightingKeepsTheVariablesAndResamples()
    {
        var original = new VariableSymbol(["x0", "x1"], [1.0, 0.0]);

        var reweighted = original with { SelectionWeights = [0.0, 1.0] };

        ((VariableExpressionNode)original.CreateNode(new SequenceRandomNumberGenerator())).VariableName.ShouldBe("x0");
        ((VariableExpressionNode)reweighted.CreateNode(new SequenceRandomNumberGenerator())).VariableName.ShouldBe("x1");
        reweighted.Variables.ShouldBe(["x0", "x1"]);
        reweighted.SelectionWeights.ShouldBe([0.0, 1.0]);
        reweighted.ShouldBe(new VariableSymbol(["x0", "x1"], [0.0, 1.0]));

        // Reweighting produces a different symbol by value, so it is a configuration-time facility only.
        reweighted.ShouldNotBe(original);
        reweighted.CanPerturb(original.CreateNode(new SequenceRandomNumberGenerator())).ShouldBeFalse();
    }

    [Fact]
    public void VariableSymbol_ReweightingKeepsTheVariableCountFixed()
    {
        var symbol = new VariableSymbol(["x0", "x1"]);

        Should.Throw<ArgumentException>(() => { _ = symbol with { SelectionWeights = [1.0] }; });
        (symbol with { SelectionWeights = [] }).SelectionWeights.ShouldBeEmpty();
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
    public void OperationNode_EqualityDistinguishesTheOperationSymbol()
    {
        var operand = Variable("x0").Build().Root;

        new UnaryExpressionNode(Symbols.Logarithm, operand)
            .ShouldNotBe(new UnaryExpressionNode(Symbols.SquareRoot, operand));
        new BinaryExpressionNode(Symbols.Addition, operand, operand)
            .ShouldNotBe(new BinaryExpressionNode(Symbols.Subtraction, operand, operand));
    }

    [Fact]
    public void TerminalNode_EqualityUsesItsPayload()
    {
        var variableSymbol = new VariableSymbol(["x0", "x1"]);
        new VariableExpressionNode(variableSymbol, "x0")
            .ShouldNotBe(new VariableExpressionNode(variableSymbol, "x1"));

        var constantSymbol = new EvolvableConstantSymbol();
        new NumericConstantExpressionNode(constantSymbol, 1.0)
            .ShouldNotBe(new NumericConstantExpressionNode(constantSymbol, 2.0));
    }

    [Fact]
    public void Node_EqualityIgnoresCachedSubtreeMetadata()
    {
        var first = (Variable("x0") + Variable("x1")).Build().Root;
        var second = (Variable("x0") + Variable("x1")).Build().Root;

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
        first.Length.ShouldBe(second.Length);
        first.Depth.ShouldBe(second.Depth);
        ReferenceEquals(first, second).ShouldBeFalse();
    }

    [Fact]
    public void NaryNode_EqualityUsesOrderedChildValues()
    {
        var symbol = new SumThreeSymbol();
        var first = new NaryExpressionNode(symbol, Variable("x0").Build().Root, Variable("x1").Build().Root, FixedConstant(1.0).Build().Root);
        var second = new NaryExpressionNode(symbol, Variable("x0").Build().Root, Variable("x1").Build().Root, FixedConstant(1.0).Build().Root);
        var reordered = new NaryExpressionNode(symbol, Variable("x1").Build().Root, Variable("x0").Build().Root, FixedConstant(1.0).Build().Root);

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
        first.ShouldNotBe(reordered);
    }

    [Fact]
    public void FixedConstantNode_RequiresItsSymbolsValue()
    {
        Should.Throw<ArgumentException>(() => new NumericConstantExpressionNode(new FixedConstantSymbol(1.0), 2.0));
    }

    [Fact]
    public void PointReplacement_ReplacesSameAritySymbolWithoutMutatingTheOriginal()
    {
        var expression = (Variable("x0") + Variable("x1")).Build();
        var root = expression.Root.ShouldBeOfType<BinaryExpressionNode>();

        var edited = expression.RootPoint.ReplaceWith(new BinaryExpressionNode(Symbols.Subtraction, root.Left, root.Right));

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
        var originalRoot = expression.Root.ShouldBeOfType<BinaryExpressionNode>();
        var editedRoot = edited.Root.ShouldBeOfType<BinaryExpressionNode>();
        var originalMultiplication = originalRoot.Right.ShouldBeOfType<BinaryExpressionNode>();
        var editedMultiplication = editedRoot.Right.ShouldBeOfType<BinaryExpressionNode>();

        ReferenceEquals(expression.Root, edited.Root).ShouldBeFalse();
        ReferenceEquals(originalRoot.Left, editedRoot.Left).ShouldBeTrue();
        ReferenceEquals(originalMultiplication, editedMultiplication).ShouldBeFalse();
        ReferenceEquals(originalMultiplication.Left, editedMultiplication.Left).ShouldBeTrue();
        ReferenceEquals(originalMultiplication.Right, editedMultiplication.Right).ShouldBeFalse();
    }

    [Fact]
    public void PointReplacement_RebuildsUnaryAndBinaryAncestorsWithoutCopyingUnaffectedNodes()
    {
        var expression = Sqrt(Variable("x0") + Variable("x1")).Build();
        var originalUnary = expression.Root.ShouldBeOfType<UnaryExpressionNode>();
        var originalBinary = originalUnary.Operand.ShouldBeOfType<BinaryExpressionNode>();

        var edited = expression.WithVariable(expression.RootPoint.Child(0).Child(0), new VariableSymbol(["x2"]), "x2");
        var editedUnary = edited.Root.ShouldBeOfType<UnaryExpressionNode>();
        var editedBinary = editedUnary.Operand.ShouldBeOfType<BinaryExpressionNode>();

        editedUnary.ShouldNotBeSameAs(originalUnary);
        editedBinary.ShouldNotBeSameAs(originalBinary);
        editedBinary.Right.ShouldBeSameAs(originalBinary.Right);
        editedBinary.Left.ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("x2");
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
        var originalRoot = expression.Root.ShouldBeOfType<BinaryExpressionNode>();
        var editedRoot = edited.Root.ShouldBeOfType<BinaryExpressionNode>();

        edited.ToInfixString().ShouldBe("(x0 + (3 * x2))");
        editedRoot.Left.ShouldBeSameAs(originalRoot.Left);
        editedRoot.Right.ShouldNotBeSameAs(originalRoot.Right);
        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
    }

    [Fact]
    public void ReplaceMany_RebuildsAnNaryNodeOnceAndSharesUnaffectedChildren()
    {
        var expression = ExpressionDraft.Apply(new SumThreeSymbol(), Variable("x0"), Variable("x1"), Variable("x2")).Build();
        var original = expression.Root.ShouldBeOfType<NaryExpressionNode>();

        var edited = expression.ReplaceMany([
            (expression.RootPoint.Child(0), Variable("x3").Build().Root),
            (expression.RootPoint.Child(2), Variable("x4").Build().Root)
        ]);
        var replacement = edited.Root.ShouldBeOfType<NaryExpressionNode>();

        replacement.ShouldNotBeSameAs(original);
        replacement.Children[1].ShouldBeSameAs(original.Children[1]);
        replacement.Children[0].ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("x3");
        replacement.Children[2].ShouldBeOfType<VariableExpressionNode>().VariableName.ShouldBe("x4");
    }

    [Fact]
    public void ReplaceMany_ReturnsTheOriginalTreeWhenEveryReplacementIsEqual()
    {
        var expression = CreateLinearExpression();
        var root = expression.Root.ShouldBeOfType<BinaryExpressionNode>();

        var edited = expression.ReplaceMany([
            (expression.RootPoint.Child(0), root.Left),
            (expression.RootPoint.Child(1), root.Right)
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
    public void Replace_WithTree_ReplacesOnlyTheSelectedBranch()
    {
        var expression = CreateLinearExpression();
        var edited = expression.Replace(expression.RootPoint.Child(1), Variable("x2").Build());

        expression.ToInfixString().ShouldBe("(x0 + (2 * x1))");
        edited.ToInfixString().ShouldBe("(x0 + x2)");
        edited.EvaluateSingleRow(("x0", 3.0), ("x2", 4.0)).ShouldBe(7.0);
    }

    [Fact]
    public void Replace_WithTree_SharesTheUnaffectedBranchAndTheReplacementTree()
    {
        var expression = CreateLinearExpression();
        var replacement = (Variable("x2") - FixedConstant(1.0)).Build();

        var edited = expression.Replace(expression.RootPoint.Child(1), replacement);
        var originalRoot = expression.Root.ShouldBeOfType<BinaryExpressionNode>();
        var editedRoot = edited.Root.ShouldBeOfType<BinaryExpressionNode>();

        ReferenceEquals(originalRoot.Left, editedRoot.Left).ShouldBeTrue();
        ReferenceEquals(replacement.Root, editedRoot.Right).ShouldBeTrue();
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
        expression.Root.ShouldBeOfType<NaryExpressionNode>().Children
            .Cast<VariableExpressionNode>()
            .Select(child => child.VariableName)
            .ShouldBe(["x0", "x1", "x2"]);
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

        Should.Throw<KeyNotFoundException>(() => expression.Evaluate(new DataFrame([])));
        Should.Throw<KeyNotFoundException>(() =>
            expression.Evaluate(new DataFrame([Series<double>.FromOwnedArray("x1", [1.0])])));
        Should.Throw<KeyNotFoundException>(() => expression.EvaluateSingleRow(("x1", 1.0)));
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
        var data = new DataFrame([Series<double>.FromOwnedArray("x0", [1.0, 2.0])]);
        var destination = new[] { double.NaN, double.NaN, 42.0 };

        Variable("x0").Build().Evaluate(data, destination);

        destination.ShouldBe([1.0, 2.0, 42.0]);
    }

    [Fact]
    public void Evaluate_UsesCallerProvidedWorkspace()
    {
        var expression = (Variable("x0") + FixedConstant(2.0)).Build();
        var data = new DataFrame([Series<double>.FromOwnedArray("x0", [1.0, 2.0])]);
        var destination = new[] { double.NaN, double.NaN };
        var workspace = new double[ExpressionInterpreter.GetWorkspaceLength(expression.Compile(), data)];

        expression.Evaluate(data, destination, workspace);

        destination.ShouldBe([3.0, 4.0]);
    }

    [Fact]
    public void Evaluate_TerminalsRequireNoWorkspaceAndRejectInvalidBuffers()
    {
        var data = new DataFrame([Series<double>.FromOwnedArray("x0", [1.0, 2.0])]);
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
        var data = new DataFrame([Series<double>.FromOwnedArray("x0", [1.0, 2.0])]);
        var destination = new double[2];

        Variable("x0").Build().Evaluate(data, destination, []);

        destination.ShouldBe([1.0, 2.0]);
    }

    [Fact]
    public void Evaluate_RejectsDestinationAndWorkspaceBuffersThatAreTooSmall()
    {
        var data = new DataFrame([
            Series<double>.FromOwnedArray(
                "x0",
                Enumerable.Range(0, 4097).Select(value => (double)value).ToArray())
        ]);
        var expression = (Variable("x0") + FixedConstant(2.0)).Build();
        var workspaceLength = ExpressionInterpreter.GetWorkspaceLength(expression.Compile(), data);

        Should.Throw<ArgumentException>(() => expression.Evaluate(data, new double[4096]));
        Should.Throw<ArgumentException>(() => expression.Evaluate(data, new double[4097], new double[workspaceLength - 1]));
    }

    [Fact]
    public void Evaluate_AppliesScalarVectorOperationsInBothOperandOrders()
    {
        var data = new DataFrame([Series<double>.FromOwnedArray("x0", [1.0, 2.0, 3.0])]);

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

    private static DataFrame CreateLinearData(int rowCount) =>
        new([
            Series<double>.FromOwnedArray(
                "x0",
                Enumerable.Range(0, rowCount).Select(row => (double)row).ToArray()),
            Series<double>.FromOwnedArray(
                "x1",
                Enumerable.Range(0, rowCount).Select(row => row * 0.5).ToArray())
        ]);

    private sealed record SumThreeSymbol() : OperationSymbol("sum3", 3)
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            emitter.EmitChild(0);
            emitter.EmitChild(1);
            emitter.EmitOperation(Operation.Add);
            emitter.EmitChild(2);
            emitter.EmitOperation(Operation.Add);
        }
    }

    private sealed record OneSymbol() : PayloadlessTerminalSymbol("one")
    {
        public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
        {
            emitter.EmitConstant(1.0);
        }
    }
}
