namespace HEAL.HeuristicLib.Numerics;

/// <summary>
/// The one place an operation is declared. Every consumer reads this instead of switching over <see cref="Operation"/>.
/// </summary>
/// <remarks>
/// <para>
/// Adding an operation means adding a type in <c>OperationDefinitions.cs</c> and one line to the declaration list. The
/// compiler then requires that type to supply every facet, including its adjoint rule.
/// </para>
/// <para>
/// Kernels are looked up separately from information, and separately per arity, so that nothing a caller receives is
/// optional. Asking for the kernels of the wrong arity is a mistake and throws rather than returning nothing.
/// </para>
/// </remarks>
internal static class OperationCatalog
{
    private static readonly Declaration[] Declarations =
    [
        Terminal<VariableDefinition>(),
        Terminal<ConstantDefinition>(),
        Terminal<ParameterDefinition>(),

        Binary<AddDefinition>(),
        Binary<SubtractDefinition>(),
        Binary<MultiplyDefinition>(),
        Binary<DivideDefinition>(),
        Binary<PowerDefinition>(),
        Binary<RootDefinition>(),
        Binary<AnalyticQuotientDefinition>(),

        Unary<NegateDefinition>(),
        Unary<ExpDefinition>(),
        Unary<LogDefinition>(),
        Unary<SqrtDefinition>(),
        Unary<AbsDefinition>(),
        Unary<SquareDefinition>(),
        Unary<CubeDefinition>(),
        Unary<CubeRootDefinition>(),
        Unary<SinDefinition>(),
        Unary<CosDefinition>(),
        Unary<TanDefinition>(),
        Unary<TanhDefinition>()
    ];

    private static readonly OperationInfo[] InfoByOperation = BuildInfo();
    private static readonly UnaryOperationKernels[] UnaryByOperation = Build(declaration => declaration.Unary);
    private static readonly BinaryOperationKernels[] BinaryByOperation = Build(declaration => declaration.Binary);
    private static readonly OperationInfo[] AllInfo = [.. Declarations.Select(declaration => declaration.Info)];

    public static ReadOnlySpan<OperationInfo> All => AllInfo;

    /// <remarks>
    /// <see cref="Operation.Invalid"/> is rejected first. It is zero, and so is an unfilled slot, so comparing the
    /// stored operation alone would report the gap at index zero as a declaration of it.
    /// </remarks>
    public static bool IsDeclared(Operation operation)
    {
        if (operation == Operation.Invalid)
            return false;

        var index = (int)operation;
        return index >= 0 && index < InfoByOperation.Length && InfoByOperation[index].Operation == operation;
    }

    /// <exception cref="ArgumentException"><paramref name="operation"/> is not declared.</exception>
    public static ref readonly OperationInfo GetInfo(Operation operation)
    {
        if (!IsDeclared(operation))
            throw new ArgumentException($"Operation {operation} is not a declared operation.", nameof(operation));

        return ref InfoByOperation[(int)operation];
    }

    public static bool TryGetInfo(Operation operation, out OperationInfo info)
    {
        if (!IsDeclared(operation))
        {
            info = default;
            return false;
        }

        info = InfoByOperation[(int)operation];
        return true;
    }

    /// <exception cref="ArgumentException"><paramref name="operation"/> is not a unary operation.</exception>
    public static ref readonly UnaryOperationKernels GetUnary(Operation operation)
    {
        if (GetInfo(operation).Arity != 1)
            throw new ArgumentException($"Operation {operation} is not a unary operation.", nameof(operation));

        return ref UnaryByOperation[(int)operation];
    }

    /// <exception cref="ArgumentException"><paramref name="operation"/> is not a binary operation.</exception>
    public static ref readonly BinaryOperationKernels GetBinary(Operation operation)
    {
        if (GetInfo(operation).Arity != 2)
            throw new ArgumentException($"Operation {operation} is not a binary operation.", nameof(operation));

        return ref BinaryByOperation[(int)operation];
    }

    /// <summary>
    /// Applies a binary operation to operands of which at least one is a span, choosing the matching shape.
    /// </summary>
    /// <remarks>
    /// Three shapes rather than four: two scalar operands produce a scalar rather than filling a span, and that case
    /// belongs to the caller, which has to treat a scalar result differently in any event.
    /// </remarks>
    public static void ApplyToSpan(in BinaryOperationKernels kernels, in Operand left, in Operand right, Span<double> result, ScratchSpans scratch)
    {
        if (left.IsScalar && right.IsScalar)
            throw new ArgumentException("Two scalar operands produce a scalar, which the caller applies through the scalar kernel.", nameof(left));

        if (left.IsScalar)
            kernels.ScalarSpan(left.Scalar, right.Span, result, scratch);
        else if (right.IsScalar)
            kernels.SpanScalar(left.Span, right.Scalar, result, scratch);
        else
            kernels.Span(left.Span, right.Span, result, scratch);
    }

    // Every field is read from the operation type. Arity is supplied here rather than declared, because the
    // constraint on each overload already fixes it and a declared arity could contradict the interface.
    private static Declaration Terminal<TOperation>()
        where TOperation : ITerminalOperationDefinition =>
        new(new OperationInfo(TOperation.Operation, TOperation.Name, Arity: 0, TOperation.PayloadKind,
            TOperation.IsDifferentiable, ScratchSpanCount: 0, AdjointScratchSpanCount: 0, OperationNotation.Function), Unary: null, Binary: null);

    private static Declaration Unary<TOperation>()
        where TOperation : IUnaryOperationDefinition =>
        new(new OperationInfo(TOperation.Operation, TOperation.Name, Arity: 1, PayloadKind.None,
                TOperation.IsDifferentiable, TOperation.ScratchSpanCount, TOperation.AdjointScratchSpanCount, TOperation.Notation),
            new UnaryOperationKernels(TOperation.Apply, TOperation.Apply, TOperation.Adjoint), Binary: null);

    private static Declaration Binary<TOperation>()
        where TOperation : IBinaryOperationDefinition =>
        new(new OperationInfo(TOperation.Operation, TOperation.Name, Arity: 2, PayloadKind.None,
                TOperation.IsDifferentiable, TOperation.ScratchSpanCount, TOperation.AdjointScratchSpanCount, TOperation.Notation), Unary: null,
            new BinaryOperationKernels(TOperation.Apply, TOperation.Apply, TOperation.Apply, TOperation.Apply, TOperation.Adjoint));

    private static OperationInfo[] BuildInfo()
    {
        var lookup = new OperationInfo[HighestOperationValue() + 1];
        foreach (var declaration in Declarations)
        {
            if (lookup[(int)declaration.Info.Operation].Operation != Operation.Invalid)
                throw new InvalidOperationException($"Operation {declaration.Info.Operation} is declared more than once.");

            lookup[(int)declaration.Info.Operation] = declaration.Info;
        }

        return lookup;
    }

    private static TKernels[] Build<TKernels>(Func<Declaration, TKernels?> select)
        where TKernels : struct
    {
        var lookup = new TKernels[HighestOperationValue() + 1];
        foreach (var declaration in Declarations)
        {
            if (select(declaration) is { } kernels)
                lookup[(int)declaration.Info.Operation] = kernels;
        }

        return lookup;
    }

    private static int HighestOperationValue()
    {
        var highest = 0;
        foreach (var declaration in Declarations)
            highest = Math.Max(highest, (int)declaration.Info.Operation);

        return highest;
    }

    // Only the declaration list mixes arities, and it is private. What a caller receives never does.
    private readonly record struct Declaration(
        OperationInfo Info,
        UnaryOperationKernels? Unary,
        BinaryOperationKernels? Binary);
}
