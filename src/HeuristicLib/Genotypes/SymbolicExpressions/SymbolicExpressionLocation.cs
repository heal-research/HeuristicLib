namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public readonly record struct SymbolicExpressionLocation
{
    internal SymbolicExpressionLocation(int instructionIndex)
    {
        InstructionIndex = instructionIndex;
    }

    internal int InstructionIndex { get; }
}
