using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpressionOpCodesTests
{
    [Fact]
    public void IsSupported_RecognizesBuiltInOpcodes()
    {
        SymbolicExpressionOpCodes.IsSupported(SymbolicExpressionOpCode.Log).ShouldBeTrue();
        SymbolicExpressionOpCodes.IsSupported(SymbolicExpressionOpCode.Invalid).ShouldBeFalse();
    }

    [Fact]
    public void MetadataAccessors_ReturnCanonicalArityAndPayloadKind()
    {
        SymbolicExpressionOpCodes.GetArity(SymbolicExpressionOpCode.Log).ShouldBe(1);
        SymbolicExpressionOpCodes.GetArity(SymbolicExpressionOpCode.Add).ShouldBe(2);
        SymbolicExpressionOpCodes.GetPayloadKind(SymbolicExpressionOpCode.Variable).ShouldBe(SymbolicExpressionPayloadKind.VariableReference);
        SymbolicExpressionOpCodes.GetPayloadKind(SymbolicExpressionOpCode.NumericLiteral).ShouldBe(SymbolicExpressionPayloadKind.NumericLiteral);
        SymbolicExpressionOpCodes.GetPayloadKind(SymbolicExpressionOpCode.Multiply).ShouldBe(SymbolicExpressionPayloadKind.None);
    }

    [Fact]
    public void IsTerminal_RecognizesPayloadTerminals()
    {
        SymbolicExpressionOpCodes.IsTerminal(SymbolicExpressionOpCode.Variable).ShouldBeTrue();
        SymbolicExpressionOpCodes.IsTerminal(SymbolicExpressionOpCode.NumericLiteral).ShouldBeTrue();
        SymbolicExpressionOpCodes.IsTerminal(SymbolicExpressionOpCode.Add).ShouldBeFalse();
    }
}
