using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

// total size of this class should be small to improve cache access while executing the code
public class Instruction(SymbolicExpressionTreeNode dynamicNode, ushort nArguments, byte opCode, object data) {
  // the tree node can hold additional data that is necessary for the execution of this instruction
  public SymbolicExpressionTreeNode DynamicNode { get; set; } = dynamicNode;
  // op code of the function that determines what operation should be executed
  public byte OpCode { get; set; } = opCode;
  // number of arguments of the current instruction
  public ushort NArguments { get; set; } = nArguments;
  // an optional object value (addresses for calls, argument index for arguments)
  public object Data { get; set; } = data;
}
