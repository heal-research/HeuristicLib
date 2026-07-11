namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly record struct ExpressionLocation
{
    internal ExpressionLocation(int instructionIndex)
    {
        InstructionIndex = instructionIndex;
    }

    internal int InstructionIndex { get; }
}
