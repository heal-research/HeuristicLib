namespace HEAL.HeuristicLib.Numerics;

/// <summary>
/// What every operation declares about itself.
/// </summary>
/// <remarks>
/// Members are static because an operation has no state. Declaring them here makes the compiler require an operation
/// to supply them, which a switch cannot do: a switch that forgets a case still compiles. Arity is deliberately not
/// declared, because it follows from which interface an operation implements and so cannot be stated wrongly.
/// </remarks>
public interface IOperationDefinition
{
    static abstract Operation Operation { get; }

    static abstract string Name { get; }

    /// <summary>Whether an adjoint rule exists. A <see langword="false"/> value makes differentiation reject it.</summary>
    static abstract bool IsDifferentiable { get; }
}

public interface ITerminalOperationDefinition : IOperationDefinition
{
    static abstract PayloadKind PayloadKind { get; }
}

/// <remarks>
/// An operand is either one value, when a constant subexpression folded, or a span of values. Both shapes are
/// declared so a caller can pick without the operation knowing how operands are stored.
/// </remarks>
public interface IUnaryOperationDefinition : IOperationDefinition
{
    /// <summary>How many working spans this operation needs. Each is as long as the result.</summary>
    static abstract int ScratchSpanCount { get; }

    /// <summary>How many working spans this operation's adjoint rule needs. Declared apart from the forward count because a derivative builds different intermediates than the value does.</summary>
    static abstract int AdjointScratchSpanCount { get; }

    /// <summary>How this operation is written when an expression is rendered. Unary operations are always calls.</summary>
    static abstract OperationNotation Notation { get; }

    static abstract double Apply(double value);

    static abstract void Apply(ReadOnlySpan<double> value, Span<double> result, ScratchSpans scratch);

    /// <summary>
    /// Adds this operation's contribution to its operand's adjoints, given the adjoints of its own result.
    /// </summary>
    /// <param name="upstream">The adjoints of this operation's result. Its length is the batch.</param>
    /// <param name="operand">The operand's forward value.</param>
    /// <param name="result">This operation's own forward value, which some derivatives reuse rather than recompute.</param>
    /// <param name="operandAdjoints">Accumulated into, never assigned, because an operand reached by several paths receives a contribution from each.</param>
    /// <param name="scratch">As many working spans as <see cref="AdjointScratchSpanCount"/> declared, each as long as the batch.</param>
    /// <remarks>
    /// There is no activity flag, because with one operand a passive operand means there is nothing to do at all and
    /// the caller skips the rule instead. An operation that declares <see cref="IOperationDefinition.IsDifferentiable"/>
    /// false throws from here; it is still required to supply the member so that the omission cannot be silent.
    /// </remarks>
    static abstract void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch);
}

/// <remarks>
/// Either operand may be one value or a span of values, so there are four combinations. Declaring all four keeps the
/// broadcast forms that avoid expanding a single value into a whole span, and lets the caller route to the right
/// one without every operation repeating that choice.
/// </remarks>
public interface IBinaryOperationDefinition : IOperationDefinition
{
    /// <summary>How many working spans this operation needs. Each is as long as the result.</summary>
    static abstract int ScratchSpanCount { get; }

    /// <summary>How many working spans this operation's adjoint rule needs. Declared apart from the forward count because a derivative builds different intermediates than the value does.</summary>
    static abstract int AdjointScratchSpanCount { get; }

    /// <summary>How this operation is written when an expression is rendered.</summary>
    static abstract OperationNotation Notation { get; }

    static abstract double Apply(double left, double right);

    static abstract void Apply(ReadOnlySpan<double> left, double right, Span<double> result, ScratchSpans scratch);

    static abstract void Apply(double left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch);

    static abstract void Apply(ReadOnlySpan<double> left, ReadOnlySpan<double> right, Span<double> result, ScratchSpans scratch);

    /// <summary>
    /// Adds this operation's contribution to both operands' adjoints, given the adjoints of its own result.
    /// </summary>
    /// <param name="upstream">The adjoints of this operation's result. Its length is the batch.</param>
    /// <param name="left">The left operand's forward value.</param>
    /// <param name="right">The right operand's forward value.</param>
    /// <param name="result">This operation's own forward value, which some derivatives reuse rather than recompute.</param>
    /// <param name="leftAdjoints">Accumulated into when <paramref name="leftIsActive"/>, never assigned.</param>
    /// <param name="leftIsActive">Whether the left operand is active, meaning it depends on a parameter. A rule must not write when this is false.</param>
    /// <param name="rightAdjoints">Accumulated into when <paramref name="rightIsActive"/>, never assigned.</param>
    /// <param name="rightIsActive">Whether the right operand is active, meaning it depends on a parameter. A rule must not write when this is false.</param>
    /// <param name="scratch">As many working spans as <see cref="AdjointScratchSpanCount"/> declared, each as long as the batch.</param>
    /// <remarks>
    /// Active and passive are the activity-analysis terms: an operand is active when it depends on a parameter, and a
    /// passive one has no derivative to receive.
    /// Each side carries its own flag, because either can be passive while the other is active and the caller can only
    /// skip the rule when both are. The flags say what the empty adjoint span would also say, and are stated anyway so
    /// that a rule reads the condition it is meant to test rather than a convention about span lengths. An operation that
    /// declares <see cref="IOperationDefinition.IsDifferentiable"/> false throws from here; it is still required to
    /// supply the member so that the omission cannot be silent.
    /// </remarks>
    static abstract void Adjoint(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch);
}

