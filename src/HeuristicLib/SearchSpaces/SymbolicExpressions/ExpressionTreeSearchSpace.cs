using Generator.Equals;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

[Equatable]
public sealed partial record ExpressionTreeSearchSpace : SearchSpace<ExpressionTree>
{
    [IgnoreEquality] private readonly Dictionary<int, Symbol[]> symbolsByArity;
    [IgnoreEquality] private readonly Dictionary<int, ImmutableArray<double>> selectionWeightsByArity;
    [IgnoreEquality] private readonly bool allowsVariables;
    [IgnoreEquality] private readonly bool allowsEvolvableConstants;

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IEnumerable<OperationSymbol> operations, IEnumerable<string> variables)
        : this(maximumLength, maximumDepth, ComposeCommonSymbols(operations, variables, [new EvolvableConstantSymbol()]))
    {
    }

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IEnumerable<OperationSymbol> operations, IEnumerable<string> variables, IEnumerable<ConstantSymbol> constants)
        : this(maximumLength, maximumDepth, ComposeCommonSymbols(operations, variables, constants))
    {
    }

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IEnumerable<Symbol> symbols)
        : this(maximumLength, maximumDepth, symbols, selectionWeights: null)
    {
    }

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IEnumerable<(Symbol Symbol, double Weight)> symbols)
        : this(maximumLength, maximumDepth, CreateWeightedSymbolSet(symbols))
    {
    }

    private ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, WeightedSymbolSet symbols)
        : this(maximumLength, maximumDepth, symbols.Symbols, symbols.Weights)
    {
    }

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IEnumerable<Symbol> symbols, IEnumerable<double>? selectionWeights)
    {
        if (maximumLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumLength));
        if (maximumDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        MaximumLength = maximumLength;
        MaximumDepth = maximumDepth;
        Symbols = symbols.ToImmutableArray();
        if (Symbols.IsDefaultOrEmpty)
            throw new ArgumentException("At least one symbol must be supplied.", nameof(symbols));

        SelectionWeights = WeightSelection.Normalize(selectionWeights?.ToArray(), Symbols.Length);

        symbolsByArity = Symbols
            .GroupBy(symbol => symbol.Arity)
            .ToDictionary(group => group.Key, group => group.ToArray());
        if (!symbolsByArity.TryGetValue(0, out var terminalSymbols))
            throw new ArgumentException("At least one terminal symbol must be allowed.", nameof(symbols));
        TerminalSymbols = terminalSymbols.ToImmutableArray();
        selectionWeightsByArity = symbolsByArity.Keys.ToDictionary(
            arity => arity,
            arity => CreateSelection(Symbols
                .Select((symbol, index) => (Symbol: symbol, Index: index))
                .Where(entry => entry.Symbol.Arity == arity)
                .Select(entry => entry.Index)
                .ToArray()));
        allowsVariables = Symbols.OfType<VariableSymbol>().Any();
        allowsEvolvableConstants = Symbols.OfType<EvolvableConstantSymbol>().Any();
    }

    public int MaximumLength { get; }
    public int MaximumDepth { get; }
    [OrderedEquality] public ImmutableArray<Symbol> Symbols { get; }
    [IgnoreEquality] public ImmutableArray<Symbol> TerminalSymbols { get; }
    [OrderedEquality] public ImmutableArray<double> SelectionWeights { get; }
    [IgnoreEquality] public bool AllowsVariables => allowsVariables;
    [IgnoreEquality] public bool AllowsEvolvableConstants => allowsEvolvableConstants;

    public IReadOnlyList<Symbol> GetSymbols(int arity) =>
        symbolsByArity.TryGetValue(arity, out var symbols) ? symbols : [];

    public Symbol SelectSymbol(int arity, IRandomNumberGenerator random)
    {
        if (!symbolsByArity.TryGetValue(arity, out var symbols))
            throw new ArgumentException($"No symbols with arity {arity} are allowed.", nameof(arity));

        var weights = selectionWeightsByArity[arity];
        var index = WeightSelection.SelectIndex(random, symbols.Length, weights);
        return symbols[index];
    }

    internal ImmutableArray<double> GetSelectionWeights(int arity) =>
        selectionWeightsByArity.TryGetValue(arity, out var weights)
            ? weights
            : throw new ArgumentException($"No symbols with arity {arity} are allowed.", nameof(arity));

    public Symbol? ResolveUniqueSymbol(Func<Symbol, bool> predicate)
    {
        Symbol? match = null;
        foreach (var symbol in Symbols)
        {
            if (!predicate(symbol))
                continue;
            if (match is not null)
                return null;
            match = symbol;
        }

        return match;
    }

    public override bool Contains(ExpressionTree genotype)
    {
        if (genotype.Length > MaximumLength || genotype.Depth > MaximumDepth)
            return false;

        return ContainsNodeAndDescendants(genotype.Root);
    }

    private bool ContainsNodeAndDescendants(ExpressionNode node)
    {
        if (!ContainsNode(node))
            return false;

        foreach (var child in node.TraverseChildren())
        {
            if (!ContainsNodeAndDescendants(child))
                return false;
        }

        return true;
    }

    private bool ContainsNode(ExpressionNode node) => node.Symbol switch
    {
        OperationSymbol operation => Symbols.Contains(operation),
        VariableSymbol => node.VariableName is not null
                              && Symbols.OfType<VariableSymbol>()
                                  .Any(variable => variable.Variables.Contains(node.VariableName, StringComparer.Ordinal)),
        FixedConstantSymbol fixedConstant => node.HasNumericValue
                                                && node.NumericValue == fixedConstant.Value
                                                && Symbols.Contains(fixedConstant),
        EvolvableConstantSymbol => node.HasNumericValue && AllowsEvolvableConstants,
        _ => false
    };

    private ImmutableArray<double> CreateSelection(IReadOnlyList<int> indexes)
    {
        return WeightSelection.Normalize(
            SelectionWeights.IsEmpty ? null : indexes.Select(index => SelectionWeights[index]).ToArray(),
            indexes.Count);
    }

    private static IEnumerable<Symbol> ComposeCommonSymbols(IEnumerable<OperationSymbol> operations, IEnumerable<string> variables, IEnumerable<ConstantSymbol> constants)
    {
        var symbols = new List<Symbol>();
        symbols.AddRange(operations);
        var variableArray = variables.ToArray();
        if (variableArray.Length > 0)
            symbols.Add(new VariableSymbol(variableArray));
        symbols.AddRange(constants);
        return symbols;
    }

    private static WeightedSymbolSet CreateWeightedSymbolSet(IEnumerable<(Symbol Symbol, double Weight)> symbols)
    {
        var entries = symbols.ToArray();
        return new WeightedSymbolSet(entries.Select(entry => entry.Symbol).ToArray(), entries.Select(entry => entry.Weight).ToArray());
    }

    private readonly record struct WeightedSymbolSet(Symbol[] Symbols, double[] Weights);

}
