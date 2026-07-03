namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public enum SymbolicExpressionPayloadKind
{
    None,
    VariableReference,
    NumericLiteral
}

internal readonly record struct SymbolicExpressionOpCodeMetadata(int Arity, SymbolicExpressionPayloadKind PayloadKind)
{
    public bool IsTerminal => Arity == 0;
}

public static class SymbolicExpressionOpCodes
{
    public static IReadOnlyList<SymbolicExpressionOpCode> All { get; } =
    [
        SymbolicExpressionOpCode.Variable,
        SymbolicExpressionOpCode.NumericLiteral,
        SymbolicExpressionOpCode.Add,
        SymbolicExpressionOpCode.Subtract,
        SymbolicExpressionOpCode.Multiply,
        SymbolicExpressionOpCode.Divide,
        SymbolicExpressionOpCode.Log,
        SymbolicExpressionOpCode.Sqrt
    ];

    public static IReadOnlyList<SymbolicExpressionOpCode> Terminals { get; } =
    [
        SymbolicExpressionOpCode.Variable,
        SymbolicExpressionOpCode.NumericLiteral
    ];

    public static IReadOnlyList<SymbolicExpressionOpCode> BasicArithmetic { get; } =
    [
        SymbolicExpressionOpCode.Add,
        SymbolicExpressionOpCode.Subtract,
        SymbolicExpressionOpCode.Multiply,
        SymbolicExpressionOpCode.Divide,
        SymbolicExpressionOpCode.Log,
        SymbolicExpressionOpCode.Sqrt
    ];

    public static bool IsSupported(SymbolicExpressionOpCode opCode)
    {
        return TryGetMetadata(opCode, out _);
    }

    public static bool IsTerminal(SymbolicExpressionOpCode opCode)
    {
        return GetMetadata(opCode).IsTerminal;
    }

    public static int GetArity(SymbolicExpressionOpCode opCode)
    {
        return TryGetMetadata(opCode, out var metadata)
            ? metadata.Arity
            : throw new ArgumentException($"Unsupported symbol opcode {opCode}.", nameof(opCode));
    }

    public static bool TryGetArity(SymbolicExpressionOpCode opCode, out int arity)
    {
        if (TryGetMetadata(opCode, out var metadata))
        {
            arity = metadata.Arity;
            return true;
        }

        arity = -1;
        return false;
    }

    public static SymbolicExpressionPayloadKind GetPayloadKind(SymbolicExpressionOpCode opCode)
    {
        return TryGetMetadata(opCode, out var metadata)
            ? metadata.PayloadKind
            : throw new ArgumentException($"Unsupported symbol opcode {opCode}.", nameof(opCode));
    }

    public static bool TryGetPayloadKind(SymbolicExpressionOpCode opCode, out SymbolicExpressionPayloadKind payloadKind)
    {
        if (TryGetMetadata(opCode, out var metadata))
        {
            payloadKind = metadata.PayloadKind;
            return true;
        }

        payloadKind = default;
        return false;
    }

    internal static SymbolicExpressionOpCodeMetadata GetMetadata(SymbolicExpressionOpCode opCode)
    {
        return TryGetMetadata(opCode, out var metadata)
            ? metadata
            : throw new ArgumentException($"Unsupported symbol opcode {opCode}.", nameof(opCode));
    }

    internal static bool TryGetMetadata(SymbolicExpressionOpCode opCode, out SymbolicExpressionOpCodeMetadata metadata)
    {
        metadata = opCode switch
        {
            SymbolicExpressionOpCode.Variable => new SymbolicExpressionOpCodeMetadata(0, SymbolicExpressionPayloadKind.VariableReference),
            SymbolicExpressionOpCode.NumericLiteral => new SymbolicExpressionOpCodeMetadata(0, SymbolicExpressionPayloadKind.NumericLiteral),
            SymbolicExpressionOpCode.Add => new SymbolicExpressionOpCodeMetadata(2, SymbolicExpressionPayloadKind.None),
            SymbolicExpressionOpCode.Subtract => new SymbolicExpressionOpCodeMetadata(2, SymbolicExpressionPayloadKind.None),
            SymbolicExpressionOpCode.Multiply => new SymbolicExpressionOpCodeMetadata(2, SymbolicExpressionPayloadKind.None),
            SymbolicExpressionOpCode.Divide => new SymbolicExpressionOpCodeMetadata(2, SymbolicExpressionPayloadKind.None),
            SymbolicExpressionOpCode.Log => new SymbolicExpressionOpCodeMetadata(1, SymbolicExpressionPayloadKind.None),
            SymbolicExpressionOpCode.Sqrt => new SymbolicExpressionOpCodeMetadata(1, SymbolicExpressionPayloadKind.None),
            _ => default
        };

        return opCode is SymbolicExpressionOpCode.Variable
                         or SymbolicExpressionOpCode.NumericLiteral
                         or SymbolicExpressionOpCode.Add
                         or SymbolicExpressionOpCode.Subtract
                         or SymbolicExpressionOpCode.Multiply
                         or SymbolicExpressionOpCode.Divide
                         or SymbolicExpressionOpCode.Log
                         or SymbolicExpressionOpCode.Sqrt;
    }

    internal static bool MatchesArity(SymbolicExpressionOpCode opCode, int arity)
    {
        return TryGetMetadata(opCode, out var metadata) && metadata.Arity == arity;
    }
}
