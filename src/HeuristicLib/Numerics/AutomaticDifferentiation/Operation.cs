namespace HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

internal enum Operation : byte
{
    Input,
    Parameter,
    Constant,
    Add,
    Subtract,
    Multiply,
    Divide,
    Negate,
    Exp,
    Log,
    Sin,
    Cos,
    Tan,
    Tanh,
}
