using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.SymbolicExpressions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;

public record NodeReplacementMutator
    : SingleSolutionMutator<SymbolicExpression, SymbolicExpressionSearchSpace>
{
    public NodeReplacementMutator()
        : this(SymbolicExpressionSamplingProfile.Default)
    {
    }

    public NodeReplacementMutator(SymbolicExpressionSamplingProfile samplingProfile)
    {
        SamplingProfile = samplingProfile;
    }

    public SymbolicExpressionSamplingProfile SamplingProfile { get; }

    public override SymbolicExpression Mutate(SymbolicExpression parent, IRandomNumberGenerator random, SymbolicExpressionSearchSpace searchSpace)
    {
        return NodeReplacementMutation.Mutate(parent, random, searchSpace, SamplingProfile);
    }
}

public static class NodeReplacementMutation
{
    public static SymbolicExpression Mutate(SymbolicExpression parent, IRandomNumberGenerator random, SymbolicExpressionSearchSpace searchSpace)
    {
        return Mutate(parent, random, searchSpace, SymbolicExpressionSamplingProfile.Default);
    }

    public static SymbolicExpression Mutate(SymbolicExpression parent, IRandomNumberGenerator random, SymbolicExpressionSearchSpace searchSpace, SymbolicExpressionSamplingProfile samplingProfile)
    {
        var instructionIndex = random.NextInt(parent.InstructionCount);
        var instruction = parent.GetInstruction(instructionIndex);
        var location = new SymbolicExpressionLocation(instructionIndex);
        return instruction.Arity == 0
            ? ReplaceTerminal(parent, location, random, searchSpace, samplingProfile)
            : ReplaceNonTerminal(parent, location, instruction, random, searchSpace);
    }

    private static SymbolicExpression ReplaceTerminal(SymbolicExpression parent, SymbolicExpressionLocation location, IRandomNumberGenerator random, SymbolicExpressionSearchSpace searchSpace, SymbolicExpressionSamplingProfile samplingProfile)
    {
        var symbol = searchSpace.AllowedTerminalSymbols[random.NextInt(searchSpace.AllowedTerminalSymbols.Count)];
        return SymbolicExpressionOpCodes.GetPayloadKind(symbol) switch
        {
            SymbolicExpressionPayloadKind.VariableReference => ReplaceWithVariable(parent, location, random, searchSpace),
            SymbolicExpressionPayloadKind.NumericLiteral => ReplaceWithNumericLiteral(parent, location, random, samplingProfile),
            _ => throw new InvalidOperationException($"Unsupported terminal symbol {symbol}.")
        };
    }

    private static SymbolicExpression ReplaceWithVariable(SymbolicExpression parent, SymbolicExpressionLocation location, IRandomNumberGenerator random, SymbolicExpressionSearchSpace searchSpace)
    {
        var selectedVariable = random.NextInt(searchSpace.AllowedVariables.Count);
        return parent.WithVariable(location, searchSpace.AllowedVariables[selectedVariable]);
    }

    private static SymbolicExpression ReplaceWithNumericLiteral(SymbolicExpression parent, SymbolicExpressionLocation location, IRandomNumberGenerator random, SymbolicExpressionSamplingProfile samplingProfile)
    {
        return parent.WithNumericLiteral(location, new NumericLiteral(samplingProfile.NumericLiteralInitializationDistribution.Sample(random), NumericLiteralKind.Optimizable));
    }

    private static SymbolicExpression ReplaceNonTerminal(SymbolicExpression parent, SymbolicExpressionLocation location, ExpressionInstruction instruction, IRandomNumberGenerator random, SymbolicExpressionSearchSpace searchSpace)
    {
        return parent.WithOpCode(location, SelectOperationReplacement(instruction, random, searchSpace));
    }

    private static SymbolicExpressionOpCode SelectOperationReplacement(ExpressionInstruction instruction, IRandomNumberGenerator random, SymbolicExpressionSearchSpace searchSpace)
    {
        var operations = searchSpace.GetOperations(instruction.Arity);
        return operations[random.NextInt(operations.Count)];
    }

}
