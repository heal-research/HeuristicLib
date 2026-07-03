using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

public sealed record SymbolicExpressionSearchSpace : SearchSpace<SymbolicExpression>
{
    private readonly Dictionary<int, SymbolicExpressionOpCode[]> operationsByArity;
    private readonly HashSet<SymbolicExpressionOpCode> symbolSet;
    private readonly HashSet<string> variableSet;

    public SymbolicExpressionSearchSpace(int maximumLength, int maximumDepth, IEnumerable<SymbolicExpressionOpCode> allowedOperations, IEnumerable<string> allowedVariables, bool allowNumericLiterals = true)
    {
        if (maximumLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumLength));

        if (maximumDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumDepth));

        MaximumLength = maximumLength;
        MaximumDepth = maximumDepth;
        AllowedVariables = allowedVariables.Select(ValidateVariableName).Distinct(StringComparer.Ordinal).ToArray();
        AllowedSymbols = BuildAllowedSymbols(allowedOperations, AllowedVariables, allowNumericLiterals);
        AllowedTerminalSymbols = AllowedSymbols.Where(symbol => SymbolicExpressionOpCodes.GetMetadata(symbol).IsTerminal).ToArray();

        symbolSet = AllowedSymbols.ToHashSet();
        variableSet = AllowedVariables.ToHashSet(StringComparer.Ordinal);
        AllowsVariables = symbolSet.Contains(SymbolicExpressionOpCode.Variable);
        AllowsNumericLiterals = symbolSet.Contains(SymbolicExpressionOpCode.NumericLiteral);

        ValidateTerminalConfiguration(AllowedTerminalSymbols);

        operationsByArity = AllowedSymbols
            .Select(symbol => new AllowedSymbol(symbol, SymbolicExpressionOpCodes.GetMetadata(symbol)))
            .Where(symbol => !symbol.Metadata.IsTerminal)
            .GroupBy(symbol => symbol.Metadata.Arity, symbol => symbol.OpCode)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    public int MaximumLength { get; }
    public int MaximumDepth { get; }
    public IReadOnlyList<SymbolicExpressionOpCode> AllowedSymbols { get; }
    public IReadOnlyList<SymbolicExpressionOpCode> AllowedTerminalSymbols { get; }
    public IReadOnlyList<string> AllowedVariables { get; }
    public bool AllowsVariables { get; }
    public bool AllowsNumericLiterals { get; }

    public IReadOnlyList<SymbolicExpressionOpCode> GetOperations(int arity)
    {
        return operationsByArity.TryGetValue(arity, out var operations)
            ? operations
            : [];
    }

    public bool ContainsOperation(SymbolicExpressionOpCode opCode, int arity)
    {
        return SymbolicExpressionOpCodes.TryGetMetadata(opCode, out var metadata)
               && metadata.Arity == arity
               && !metadata.IsTerminal
               && symbolSet.Contains(opCode);
    }

    public bool ContainsVariable(string variableName)
    {
        return variableSet.Contains(variableName);
    }

    public override bool Contains(SymbolicExpression genotype)
    {
        if (genotype.Length > MaximumLength || genotype.Depth > MaximumDepth)
            return false;

        for (var i = 0; i < genotype.InstructionCount; i++)
        {
            var instruction = genotype.GetInstruction(i);
            if (!ContainsInstruction(genotype, instruction))
                return false;
        }

        return true;
    }

    private bool ContainsInstruction(SymbolicExpression genotype, ExpressionInstruction instruction)
    {
        if (!SymbolicExpressionOpCodes.TryGetMetadata(instruction.OpCode, out var metadata) || metadata.Arity != instruction.Arity || !symbolSet.Contains(instruction.OpCode))
            return false;

        return metadata.PayloadKind switch
        {
            SymbolicExpressionPayloadKind.VariableReference => ContainsVariable(genotype.GetVariableReference(instruction.PayloadIndex).Name),
            SymbolicExpressionPayloadKind.NumericLiteral => true,
            _ => true
        };
    }

    private static void ValidateTerminalConfiguration(IReadOnlyList<SymbolicExpressionOpCode> allowedTerminalSymbols)
    {
        if (allowedTerminalSymbols.Count == 0)
            throw new ArgumentException("At least one terminal symbol must be allowed.", nameof(allowedTerminalSymbols));
    }

    private static string ValidateVariableName(string variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
            throw new ArgumentException("Variable names must not be empty.", nameof(variableName));

        return variableName;
    }

    private static SymbolicExpressionOpCode ValidateSymbol(SymbolicExpressionOpCode symbol)
    {
        return SymbolicExpressionOpCodes.IsSupported(symbol)
            ? symbol
            : throw new ArgumentException($"Unsupported symbolic expression symbol {symbol}.");
    }

    private static SymbolicExpressionOpCode ValidateOperation(SymbolicExpressionOpCode symbol)
    {
        symbol = ValidateSymbol(symbol);
        if (SymbolicExpressionOpCodes.IsTerminal(symbol))
            throw new ArgumentException($"Terminal symbol {symbol} must not be supplied as an allowed operation.");

        return symbol;
    }

    private static SymbolicExpressionOpCode[] BuildAllowedSymbols(IEnumerable<SymbolicExpressionOpCode> allowedOperations, IReadOnlyList<string> allowedVariables, bool allowNumericLiterals)
    {
        var symbols = new List<SymbolicExpressionOpCode>();
        if (allowedVariables.Count > 0)
            symbols.Add(SymbolicExpressionOpCode.Variable);

        if (allowNumericLiterals)
            symbols.Add(SymbolicExpressionOpCode.NumericLiteral);

        symbols.AddRange(allowedOperations.Select(ValidateOperation));
        return symbols.Distinct().ToArray();
    }

    private readonly record struct AllowedSymbol(SymbolicExpressionOpCode OpCode, SymbolicExpressionOpCodeMetadata Metadata);
}
