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
    Sin = 16,
    Cos = 17,
    Tan = 18,
    Tanh = 19,
    Log = 20,
    Sqrt = 21,
    Abs = 22,
    Square = 23,
    Cube = 24,
    CubeRoot = 25,
    Power = 30,
    Root = 31,
    AnalyticQuotient = 32
}
