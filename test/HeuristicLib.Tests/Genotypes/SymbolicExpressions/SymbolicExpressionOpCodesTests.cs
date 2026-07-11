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
    public void IsTerminal_RecognizesPayloadTerminals()
    {
        OpCodes.IsTerminal(OpCode.Variable).ShouldBeTrue();
        OpCodes.IsTerminal(OpCode.Constant).ShouldBeTrue();
        OpCodes.IsTerminal(OpCode.Add).ShouldBeFalse();
    }
}
