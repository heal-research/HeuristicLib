namespace HEAL.HeuristicLib.Tests.Numerics;

public sealed class OperationCatalogTests
{
    // The compiler makes an operation type supply every facet, but it cannot check that every opcode has a type.
    // This does.
    [Fact]
    public void EveryOpCodeIsDeclared()
    {
        var missing = Enum.GetValues<Operation>()
            .Where(operation => operation is not Operation.Invalid)
            .Where(operation => !OperationCatalog.IsDeclared(operation))
            .ToArray();

        missing.ShouldBeEmpty();
    }

    [Fact]
    public void InvalidIsNotDeclared()
    {
        OperationCatalog.IsDeclared(Operation.Invalid).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => OperationCatalog.GetInfo(Operation.Invalid));
    }

    // Kernels are reached through the arity that owns them, so asking for the wrong shape is a mistake rather than
    // something a caller has to check for.
    [Fact]
    public void KernelsAreOnlyReachableThroughTheirOwnArity()
    {
        Should.Throw<ArgumentException>(() => OperationCatalog.GetUnary(Operation.Add));
        Should.Throw<ArgumentException>(() => OperationCatalog.GetBinary(Operation.Exp));
        Should.Throw<ArgumentException>(() => OperationCatalog.GetUnary(Operation.Variable));
        Should.Throw<ArgumentException>(() => OperationCatalog.GetBinary(Operation.Variable));
    }

    [Fact]
    public void EveryOperationExposesTheKernelsItsArityRequires()
    {
        foreach (var info in OperationCatalog.All.ToArray())
        {
            switch (info.Arity)
            {
                case 0:
                    info.IsTerminal.ShouldBeTrue();
                    break;
                case 1:
                    OperationCatalog.GetUnary(info.Operation).Scalar.ShouldNotBeNull();
                    OperationCatalog.GetUnary(info.Operation).Span.ShouldNotBeNull();
                    break;
                case 2:
                    var binary = OperationCatalog.GetBinary(info.Operation);
                    binary.Scalar.ShouldNotBeNull();
                    binary.SpanScalar.ShouldNotBeNull();
                    binary.ScalarSpan.ShouldNotBeNull();
                    binary.Span.ShouldNotBeNull();
                    break;
                default:
                    throw new InvalidOperationException($"{info.Operation} has an unexpected arity {info.Arity}.");
            }
        }
    }

    // Opcode values are never reused, so the lookup arrays span the values rather than their count and gaps are
    // expected. This fails if numbering ever becomes sparse enough for that to stop being cheap, which is the point
    // at which a dense remap would be needed instead.
    [Fact]
    public void TheOpCodeSpaceStaysDenseEnoughToIndexDirectly()
    {
        var declared = OperationCatalog.All.Length;
        var highest = 0;
        foreach (var info in OperationCatalog.All.ToArray())
            highest = Math.Max(highest, (int)info.Operation);

        (highest + 1).ShouldBeLessThanOrEqualTo(declared * 4);
    }

    [Fact]
    public void GapsInTheOpCodeSpaceAreNotDeclared()
    {
        var declaredValues = OperationCatalog.All.ToArray().Select(info => (int)info.Operation).ToHashSet();
        var gaps = Enumerable.Range(1, declaredValues.Max()).Where(value => !declaredValues.Contains(value)).ToArray();

        gaps.ShouldNotBeEmpty("the opcode space is expected to have gaps, so this test should be exercising them");
        foreach (var gap in gaps)
        {
            OperationCatalog.IsDeclared((Operation)gap).ShouldBeFalse($"opcode {gap} is a gap.");
            Should.Throw<ArgumentException>(() => OperationCatalog.GetInfo((Operation)gap));
        }
    }

    [Fact]
    public void EveryOperationHasAName()
    {
        foreach (var info in OperationCatalog.All.ToArray())
            info.Name.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(Operation.Log, 1)]
    [InlineData(Operation.Negate, 1)]
    [InlineData(Operation.Add, 2)]
    [InlineData(Operation.AnalyticQuotient, 2)]
    [InlineData(Operation.Variable, 0)]
    public void ArityMatchesTheOperation(Operation operation, int expected) =>
        OperationCatalog.GetInfo(operation).Arity.ShouldBe(expected);

    // A fact rather than a theory because the payload kind is internal, and a public theory cannot take it as a
    // parameter. The cases are the same.
    [Fact]
    public void PayloadKindMatchesTheOperation()
    {
        AssertPayloadKind(Operation.Variable, PayloadKind.VariableReference);
        AssertPayloadKind(Operation.Constant, PayloadKind.Constant);
        AssertPayloadKind(Operation.Parameter, PayloadKind.Parameter);
        AssertPayloadKind(Operation.Multiply, PayloadKind.None);

        static void AssertPayloadKind(Operation operation, PayloadKind expected) =>
            OperationCatalog.GetInfo(operation).PayloadKind.ShouldBe(expected);
    }

    [Theory]
    [InlineData(Operation.Variable, true)]
    [InlineData(Operation.Constant, true)]
    [InlineData(Operation.Add, false)]
    public void TerminalsAreRecognized(Operation operation, bool expected) =>
        OperationCatalog.GetInfo(operation).IsTerminal.ShouldBe(expected);

    // Only Root and AnalyticQuotient build an intermediate span, and the interpreter sizes its workspace from
    // this, so a wrong value here would corrupt evaluation rather than merely waste memory.
    [Theory]
    [InlineData(Operation.Root, 1)]
    [InlineData(Operation.AnalyticQuotient, 1)]
    [InlineData(Operation.Add, 0)]
    [InlineData(Operation.Power, 0)]
    public void ScratchSpanCountMatchesTheOperation(Operation operation, int expected) =>
        OperationCatalog.GetInfo(operation).ScratchSpanCount.ShouldBe(expected);

    [Theory]
    [InlineData(Operation.Add, 3.0, 4.0, 7.0)]
    [InlineData(Operation.Subtract, 3.0, 4.0, -1.0)]
    [InlineData(Operation.Multiply, 3.0, 4.0, 12.0)]
    [InlineData(Operation.Divide, 3.0, 4.0, 0.75)]
    public void BinaryKernelsComputeTheirOperation(Operation operation, double left, double right, double expected) =>
        OperationCatalog.GetBinary(operation).Scalar(left, right).ShouldBe(expected);

    // Rendering an expression reads the name and the notation from here, so a wrong notation would print a binary
    // function as though it were an operator.
    [Fact]
    public void NameAndNotationMatchTheOperation()
    {
        AssertNameAndNotation(Operation.Add, "+", OperationNotation.Infix);
        AssertNameAndNotation(Operation.Divide, "/", OperationNotation.Infix);
        AssertNameAndNotation(Operation.Power, "pow", OperationNotation.Function);
        AssertNameAndNotation(Operation.AnalyticQuotient, "aq", OperationNotation.Function);
        AssertNameAndNotation(Operation.Exp, "exp", OperationNotation.Function);
        AssertNameAndNotation(Operation.CubeRoot, "cbrt", OperationNotation.Function);

        static void AssertNameAndNotation(Operation operation, string name, OperationNotation notation)
        {
            ref readonly var info = ref OperationCatalog.GetInfo(operation);
            info.Name.ShouldBe(name);
            info.Notation.ShouldBe(notation);
        }
    }

    [Fact]
    public void OnlyBinaryOperationsUseInfixNotation()
    {
        foreach (var info in OperationCatalog.All.ToArray())
        {
            if (info.Notation == OperationNotation.Infix)
                info.Arity.ShouldBe(2, $"{info.Operation} is written between its operands.");
        }
    }

    [Theory]
    [InlineData(Operation.Negate, 3.0, -3.0)]
    [InlineData(Operation.Square, 3.0, 9.0)]
    [InlineData(Operation.Cube, 3.0, 27.0)]
    [InlineData(Operation.Abs, -3.0, 3.0)]
    public void UnaryKernelsComputeTheirOperation(Operation operation, double value, double expected) =>
        OperationCatalog.GetUnary(operation).Scalar(value).ShouldBe(expected);
}
