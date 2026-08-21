namespace HEAL.HeuristicLib.Numerics;

/// <summary>
/// Identifies one numeric operation. This is the value an instruction stores, and the only thing a backend needs in
/// order to know what to compute.
/// </summary>
/// <remarks>
/// <para>
/// One identity is shared by every consumer — expression compilation and interpretation, automatic differentiation,
/// and any later backend — so that an operation is named once rather than once per engine. Behavior is not stored
/// here: the instruction stream is data, and what each operation computes belongs to its operation type.
/// </para>
/// <para>
/// Values are a compatibility contract. Genotypes are persistable and store symbols, and a separately compiled
/// device kernel would switch on these numbers, so values are appended and never reordered or reused. A retired
/// operation leaves its value permanently unused rather than freeing it.
/// </para>
/// <para>
/// The width is <see langword="ushort"/> rather than <see langword="byte"/> because values are never reused, so the
/// space only ever grows, and because it costs nothing: an instruction pads to the same size either way.
/// </para>
/// </remarks>
public enum Operation : ushort
{
    Invalid = 0,

    /// <summary>Reads one row of an input column.</summary>
    Variable = 1,

    /// <summary>Reads a value fixed for the lifetime of the program.</summary>
    Constant = 2,

    /// <summary>
    /// Reads a value the caller optimizes. Only differentiation emits this; expression programs carry an evolvable
    /// constant as a <see cref="Constant"/>, because whether a constant is optimizable is a property of its symbol.
    /// </summary>
    Parameter = 3,

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
