namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal readonly record struct Instruction(Operation Operation, int LeftOperand, int RightOperand, int PayloadIndex, int VectorPrimalSlot, int AdjointSlot)
{
    internal bool DependsOnInput => Operation == Operation.Variable || VectorPrimalSlot >= 0;

    internal bool DependsOnParameter => AdjointSlot >= 0;
}
