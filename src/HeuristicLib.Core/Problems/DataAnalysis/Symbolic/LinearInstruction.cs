using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

// total size of this class should be small to improve cache access while executing the code
public class LinearInstruction(SymbolicExpressionTreeNode dynamicNode, ushort nArguments, byte opCode, object data, double value = 0, int childIndex = 0) : Instruction(dynamicNode, nArguments, opCode, data)
{
  public double Value = value;
  public int ChildIndex = childIndex;
}
