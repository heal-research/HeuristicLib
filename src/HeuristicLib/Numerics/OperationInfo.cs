namespace HEAL.HeuristicLib.Numerics;

/// <summary>
/// What an operation is, independently of what it computes.
/// </summary>
/// <remarks>
/// Every field applies to every operation, so none of them is optional. Kernels are not here because they differ by
/// arity, and mixing arities into one type is what would make them optional.
/// </remarks>
public readonly record struct OperationInfo(
    Operation Operation,
    string Name,
    int Arity,
    PayloadKind PayloadKind,
    bool IsDifferentiable,
    int ScratchSpanCount,
    int AdjointScratchSpanCount,
    OperationNotation Notation)
{
    public bool IsTerminal => Arity == 0;
}

/// <summary>How an instruction's payload index is read.</summary>
public enum PayloadKind
{
    None,
    VariableReference,
    Constant,

    /// <summary>Indexes the caller-optimized parameter vector. Only differentiation programs carry this.</summary>
    Parameter
}

/// <summary>How an operation is written when an expression is rendered.</summary>
public enum OperationNotation
{
    /// <summary>Written as a call, such as <c>exp(x)</c> or <c>pow(x, y)</c>.</summary>
    Function,

    /// <summary>Written between its operands, such as <c>(x + y)</c>. Only binary operations use this.</summary>
    Infix
}
