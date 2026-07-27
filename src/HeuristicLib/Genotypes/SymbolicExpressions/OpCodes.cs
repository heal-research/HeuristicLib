namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class OpCodes
{
    public static bool IsSupported(OpCode opCode)
    {
        return TryGetMetadata(opCode, out _);
    }

    public static bool IsTerminal(OpCode opCode)
    {
        return GetMetadata(opCode).IsTerminal;
    }

    public static int GetArity(OpCode opCode)
    {
        return TryGetMetadata(opCode, out var metadata)
            ? metadata.Arity
            : throw new ArgumentException($"Unsupported symbol opcode {opCode}.", nameof(opCode));
    }

    public static bool TryGetArity(OpCode opCode, out int arity)
    {
        if (TryGetMetadata(opCode, out var metadata))
        {
            arity = metadata.Arity;
            return true;
        }

        arity = -1;
        return false;
    }

    public static PayloadKind GetPayloadKind(OpCode opCode)
    {
        return TryGetMetadata(opCode, out var metadata)
            ? metadata.PayloadKind
            : throw new ArgumentException($"Unsupported symbol opcode {opCode}.", nameof(opCode));
    }

    public static bool TryGetPayloadKind(OpCode opCode, out PayloadKind payloadKind)
    {
        if (TryGetMetadata(opCode, out var metadata))
        {
            payloadKind = metadata.PayloadKind;
            return true;
        }

        payloadKind = default;
        return false;
    }

    internal static OpCodeMetadata GetMetadata(OpCode opCode)
    {
        return TryGetMetadata(opCode, out var metadata)
            ? metadata
            : throw new ArgumentException($"Unsupported symbol opcode {opCode}.", nameof(opCode));
    }

    internal static bool TryGetMetadata(OpCode opCode, out OpCodeMetadata metadata)
    {
        metadata = opCode switch
        {
            OpCode.Variable => new OpCodeMetadata(0, PayloadKind.VariableReference),
            OpCode.Constant => new OpCodeMetadata(0, PayloadKind.Constant),
            OpCode.Add => new OpCodeMetadata(2, PayloadKind.None),
            OpCode.Subtract => new OpCodeMetadata(2, PayloadKind.None),
            OpCode.Multiply => new OpCodeMetadata(2, PayloadKind.None),
            OpCode.Divide => new OpCodeMetadata(2, PayloadKind.None),
            OpCode.Negate => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Exp => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Sin => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Cos => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Tan => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Tanh => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Log => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Sqrt => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Abs => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Square => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Cube => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.CubeRoot => new OpCodeMetadata(1, PayloadKind.None),
            OpCode.Power => new OpCodeMetadata(2, PayloadKind.None),
            OpCode.Root => new OpCodeMetadata(2, PayloadKind.None),
            OpCode.AnalyticQuotient => new OpCodeMetadata(2, PayloadKind.None),
            _ => default
        };

        return metadata.Arity != 0 || opCode is OpCode.Variable or OpCode.Constant;
    }

    internal static bool MatchesArity(OpCode opCode, int arity)
    {
        return TryGetMetadata(opCode, out var metadata) && metadata.Arity == arity;
    }
}

public enum PayloadKind
{
    None,
    VariableReference,
    Constant
}

internal readonly record struct OpCodeMetadata(int Arity, PayloadKind PayloadKind)
{
    public bool IsTerminal => Arity == 0;
}
