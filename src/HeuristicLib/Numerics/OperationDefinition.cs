namespace HEAL.HeuristicLib.Numerics;

/// <summary>
/// What every operation declares about itself.
/// </summary>
/// <remarks>
/// Arity is not declared: it follows from which interface an operation implements.
/// </remarks>
internal interface IOperationDefinition
{
    static abstract Operation Operation { get; }

    static abstract string Name { get; }

    /// <summary>Whether an adjoint rule exists. A <see langword="false"/> value makes differentiation reject it.</summary>
    static abstract bool IsDifferentiable { get; }
}

internal interface ITerminalOperationDefinition : IOperationDefinition
{
    static abstract PayloadKind PayloadKind { get; }
}

/// <remarks>
/// An operand is either one value, when a constant subexpression folded, or a span of values. Both shapes are declared.
/// </remarks>
internal interface IUnaryOperationDefinition : IOperationDefinition
{
    /// <summary>How many working spans this operation needs. Each is as long as the result.</summary>
    static abstract int ScratchSpanCount { get; }

    /// <summary>How many working spans this operation's adjoint rule needs, which differs from the forward count.</summary>
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
    /// There is no activity flag: with one operand the caller skips the rule instead. An operation that declares
    /// <see cref="IOperationDefinition.IsDifferentiable"/> false throws from here; it is still required to supply the
    /// member so that the omission cannot be silent.
    /// </remarks>
    static abstract void Adjoint(ReadOnlySpan<double> upstream, Operand operand, Operand result, Span<double> operandAdjoints, ScratchSpans scratch);
}

/// <remarks>
/// Either operand may be one value or a span of values, so all four combinations are declared. The broadcast forms
/// avoid expanding a single value into a whole span.
/// </remarks>
internal interface IBinaryOperationDefinition : IOperationDefinition
{
    /// <summary>How many working spans this operation needs. Each is as long as the result.</summary>
    static abstract int ScratchSpanCount { get; }

    /// <summary>How many working spans this operation's adjoint rule needs, which differs from the forward count.</summary>
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
    /// Each side carries its own flag, because either can be passive while the other is active. An operation that
    /// declares <see cref="IOperationDefinition.IsDifferentiable"/> false throws from here; it is still required to
    /// supply the member so that the omission cannot be silent.
    /// </remarks>
    static abstract void Adjoint(ReadOnlySpan<double> upstream, Operand left, Operand right, Operand result, Span<double> leftAdjoints, bool leftIsActive, Span<double> rightAdjoints, bool rightIsActive, ScratchSpans scratch);
}

