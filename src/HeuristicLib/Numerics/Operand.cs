namespace HEAL.HeuristicLib.Numerics;

/// <summary>
/// One operand of an operation, which is either a single value or a span of values.
/// </summary>
/// <remarks>
/// A single value arises when a constant subexpression folded, so an expression evaluated over many rows still
/// carries operands that are the same in every row. Keeping the two apart lets an operation use the broadcast form
/// that avoids expanding one value into a whole span.
/// </remarks>
public readonly ref struct Operand
{
    private readonly ReadOnlySpan<double> vector;
    private readonly double scalar;

    private Operand(double scalar)
    {
        this.scalar = scalar;
        vector = default;
        IsScalar = true;
    }

    private Operand(ReadOnlySpan<double> vector)
    {
        this.vector = vector;
        scalar = default;
        IsScalar = false;
    }

    public bool IsScalar { get; }

    public double Scalar => scalar;

    public ReadOnlySpan<double> Span => vector;

    public static Operand FromScalar(double value) => new(value);

    public static Operand FromSpan(ReadOnlySpan<double> values) => new(values);
}
