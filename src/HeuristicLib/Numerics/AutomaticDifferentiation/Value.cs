namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal readonly struct Value
{
    internal Value(Builder owner, int instructionIndex)
    {
        Owner = owner;
        InstructionIndex = instructionIndex;
    }

    internal Builder Owner { get; }

    internal int InstructionIndex { get; }
}
