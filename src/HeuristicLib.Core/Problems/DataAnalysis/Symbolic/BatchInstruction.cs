namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

public readonly struct BatchInstruction(byte opcode, ushort numberOfArguments, int childIndex, double value, double weight, double[] buf, double[] data)
{
  public readonly byte Opcode = opcode;
  public readonly ushort NumberOfArguments = numberOfArguments;
  public readonly int ChildIndex = childIndex;

  public readonly double Value = value; // for numbers and constants
  public readonly double Weight = weight; // for variables
  public readonly double[] Buf = buf;
  public readonly double[] Data = data; // to hold dataset data
}
