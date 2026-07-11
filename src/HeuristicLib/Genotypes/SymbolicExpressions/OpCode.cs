namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public enum OpCode : ushort
{
    Invalid = 0,
    Variable = 1,
    Constant = 2,
    Add = 10,
    Subtract = 11,
    Multiply = 12,
    Divide = 13,
    Negate = 14,
    Exp = 15,
    Log = 20,
    Sqrt = 21
}
