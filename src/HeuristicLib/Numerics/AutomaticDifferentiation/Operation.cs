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
    Sqrt,
    Abs,
    Square,
    Cube,
    CubeRoot,
    Power,
    Root,
    AnalyticQuotient,
    Sin,
    Cos,
    Tan,
    Tanh,
}
