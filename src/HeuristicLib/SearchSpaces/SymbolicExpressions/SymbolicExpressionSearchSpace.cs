using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

namespace HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

public sealed record SymbolicExpressionSearchSpace : SearchSpace<SymbolicExpression>
{
    private readonly Dictionary<int, Symbol[]> operationsByArity;
    private readonly HashSet<Symbol> operationSet;
    private readonly HashSet<string> variableSet;

    public SymbolicExpressionSearchSpace(int maximumLength, int maximumDepth, IEnumerable<Symbol> allowedSymbols, IEnumerable<string> allowedVariables, bool allowNumericLiterals = true)
    {
        if (maximumLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumLength));

        if (maximumDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumDepth));

        MaximumLength = maximumLength;
        MaximumDepth = maximumDepth;
        AllowedVariables = allowedVariables.Select(ValidateVariableName).Distinct(StringComparer.Ordinal).ToArray();
        AllowedSymbols = BuildAllowedSymbols(allowedSymbols, AllowedVariables, allowNumericLiterals);
        AllowedTerminalSymbols = AllowedSymbols.Where(symbol => symbol.Arity == 0).ToArray();

        operationSet = AllowedSymbols.Where(symbol => symbol.Arity > 0).ToHashSet();
        variableSet = AllowedVariables.ToHashSet(StringComparer.Ordinal);
        AllowsVariables = AllowedTerminalSymbols.Any(symbol => symbol is VariableSymbol);
        AllowsNumericLiterals = AllowedTerminalSymbols.Any(symbol => symbol is NumericLiteralSymbol);

        ValidateTerminalConfiguration(AllowedTerminalSymbols);

        operationsByArity = AllowedSymbols
            .Where(symbol => symbol.Arity > 0)
            .GroupBy(symbol => symbol.Arity)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    public int MaximumLength { get; }
    public int MaximumDepth { get; }
    public IReadOnlyList<Symbol> AllowedSymbols { get; }
    public IReadOnlyList<Symbol> AllowedTerminalSymbols { get; }
    public IReadOnlyList<string> AllowedVariables { get; }
    public bool AllowsVariables { get; }
    public bool AllowsNumericLiterals { get; }

    public IReadOnlyList<Symbol> GetOperations(int arity)
    {
        return operationsByArity.TryGetValue(arity, out var operations)
            ? operations
            : [];
    }

    public bool ContainsOperation(Symbol symbol)
    {
        return symbol.Arity > 0 && operationSet.Contains(symbol);
    }

    public bool ContainsVariable(string variableName)
    {
        return variableSet.Contains(variableName);
    }

    public override bool Contains(SymbolicExpression genotype)
    {
        if (genotype.Length > MaximumLength || genotype.Depth > MaximumDepth)
            return false;

        for (var i = 0; i < genotype.SymbolCount; i++)
        {
            if (!ContainsSymbol(genotype.GetSymbol(i)))
                return false;
        }

        return true;
    }

    private bool ContainsSymbol(Symbol symbol)
    {
        return symbol switch
        {
            VariableSymbol variable => AllowsVariables && ContainsVariable(variable.VariableName),
            NumericLiteralSymbol => AllowsNumericLiterals,
            _ => operationSet.Contains(symbol)
        };
    }

    private static void ValidateTerminalConfiguration(IReadOnlyList<Symbol> allowedTerminalSymbols)
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

    private static Symbol[] BuildAllowedSymbols(IEnumerable<Symbol> allowedSymbols, IReadOnlyList<string> allowedVariables, bool allowNumericLiterals)
    {
        var symbols = new List<Symbol>();
        if (allowedVariables.Count > 0)
            symbols.Add(new VariableSymbol(allowedVariables[0]));

        if (allowNumericLiterals)
            symbols.Add(new NumericLiteralSymbol(new NumericLiteral(0.0, NumericLiteralKind.Optimizable)));

        symbols.AddRange(allowedSymbols.Select(ValidateOperation));
        return symbols.Distinct().ToArray();
    }

    private static Symbol ValidateOperation(Symbol symbol)
    {
        if (symbol.Arity == 0)
            throw new ArgumentException($"Terminal symbol {symbol.Name} must not be supplied as an allowed operation.", nameof(symbol));

        return symbol;
    }
}
