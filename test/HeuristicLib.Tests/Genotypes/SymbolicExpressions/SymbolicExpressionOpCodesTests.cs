using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionOpCodesTests
{
    [Fact]
    public void IsSupported_RecognizesBuiltInOpcodes()
    {
        OpCodes.IsSupported(OpCode.Log).ShouldBeTrue();
        OpCodes.IsSupported(OpCode.Invalid).ShouldBeFalse();
    }

    [Fact]
    public void MetadataAccessors_ReturnCanonicalArityAndPayloadKind()
    {
        OpCodes.GetArity(OpCode.Log).ShouldBe(1);
        OpCodes.GetArity(OpCode.Add).ShouldBe(2);
        OpCodes.GetPayloadKind(OpCode.Variable).ShouldBe(PayloadKind.VariableReference);
        OpCodes.GetPayloadKind(OpCode.Constant).ShouldBe(PayloadKind.Constant);
        OpCodes.GetPayloadKind(OpCode.Multiply).ShouldBe(PayloadKind.None);
    }

    [Fact]
    public void MetadataAccessors_DescribeExtendedNumericOperations()
    {
        var unary = new[]
        {
            OpCode.Sin, OpCode.Cos, OpCode.Tan, OpCode.Tanh, OpCode.Abs,
            OpCode.Square, OpCode.Cube, OpCode.CubeRoot
        };
        var binary = new[] { OpCode.Power, OpCode.Root, OpCode.AnalyticQuotient };

        foreach (var opCode in unary)
        {
            OpCodes.IsSupported(opCode).ShouldBeTrue();
            OpCodes.GetArity(opCode).ShouldBe(1);
            OpCodes.GetPayloadKind(opCode).ShouldBe(PayloadKind.None);
        }

        foreach (var opCode in binary)
        {
            OpCodes.IsSupported(opCode).ShouldBeTrue();
            OpCodes.GetArity(opCode).ShouldBe(2);
            OpCodes.GetPayloadKind(opCode).ShouldBe(PayloadKind.None);
        }
    }

    [Fact]
    public void IsTerminal_RecognizesPayloadTerminals()
    {
        OpCodes.IsTerminal(OpCode.Variable).ShouldBeTrue();
        OpCodes.IsTerminal(OpCode.Constant).ShouldBeTrue();
        OpCodes.IsTerminal(OpCode.Add).ShouldBeFalse();
    }

    [Fact]
    public void OperationPresets_HaveDocumentedContents()
    {
        Symbols.MinimalOperations.ShouldBe([
            Symbols.Addition,
            Symbols.Subtraction,
            Symbols.Multiplication,
            Symbols.Division
        ]);
        Symbols.DefaultOperations.ShouldBe([
            Symbols.Addition,
            Symbols.Subtraction,
            Symbols.Multiplication,
            Symbols.Division,
            Symbols.Exponential,
            Symbols.Logarithm,
            Symbols.SquareRoot,
            Symbols.Square
        ]);
        Symbols.AllOperations.ShouldBe([
            Symbols.Addition,
            Symbols.Subtraction,
            Symbols.Multiplication,
            Symbols.Division,
            Symbols.Negation,
            Symbols.Exponential,
            Symbols.Logarithm,
            Symbols.SquareRoot,
            Symbols.Sine,
            Symbols.Cosine,
            Symbols.Tangent,
            Symbols.HyperbolicTangent,
            Symbols.Absolute,
            Symbols.Square,
            Symbols.Cube,
            Symbols.CubeRoot,
            Symbols.Power,
            Symbols.Root,
            Symbols.AnalyticQuotient,
            Symbols.Sigmoid
        ]);
    }
}
