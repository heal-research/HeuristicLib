using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

/// <remarks>
/// <see cref="MaximumLength"/>, <see cref="MaximumDepth"/> and <see cref="SelectionWeights"/> are the only members a
/// <c>with</c> expression may set. Setting the weights rebuilds every derived sampler for the unchanged symbols;
/// searching a different set of symbols means constructing a new search space.
/// </remarks>
public sealed record ExpressionTreeSearchSpace : SearchSpace<ExpressionTree>
{
    private const int MaximumPrecomputedArity = 2;

    private readonly Dictionary<int, Symbol[]> symbolsByArity;
    private readonly Dictionary<int, WeightedItemSampler<Symbol>> samplerByArity;
    private readonly Dictionary<(int MinimumArity, int MaximumArity), WeightedItemSampler<Symbol>> samplerByArityRange;
    private readonly WeightedItemSampler<Symbol> sampler;
    private readonly int maximumSymbolArity;
    private readonly HashSet<string> allowedVariableNames;
    private readonly bool allowsVariables;
    private readonly bool allowsEvolvableConstants;

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IEnumerable<OperationSymbol> operations, IEnumerable<string> variables)
        : this(maximumLength, maximumDepth, ComposeCommonSymbols(operations, variables, [new EvolvableConstantSymbol()]))
    {
    }

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IEnumerable<OperationSymbol> operations, IEnumerable<string> variables, IEnumerable<ConstantSymbol> constants)
        : this(maximumLength, maximumDepth, ComposeCommonSymbols(operations, variables, constants))
    {
    }

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IReadOnlyList<Symbol> symbols)
        : this(maximumLength, maximumDepth, symbols, selectionWeights: null)
    {
    }

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IReadOnlyList<(Symbol Symbol, double Weight)> symbols)
        : this(maximumLength, maximumDepth, CreateWeightedSymbolSet(symbols))
    {
    }

    private ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, WeightedSymbolSet symbols)
        : this(maximumLength, maximumDepth, symbols.Symbols, symbols.Weights)
    {
    }

    public ExpressionTreeSearchSpace(int maximumLength, int maximumDepth, IReadOnlyList<Symbol> symbols, IReadOnlyList<double>? selectionWeights)
    {
        if (maximumLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumLength));
        if (maximumDepth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximumDepth));

        MaximumLength = maximumLength;
        MaximumDepth = maximumDepth;
        var items = symbols.ToValueArray();
        if (items.IsEmpty)
            throw new ArgumentException("At least one symbol must be supplied.", nameof(symbols));

        sampler = new WeightedItemSampler<Symbol>(items, selectionWeights);

        symbolsByArity = Symbols
            .GroupBy(symbol => symbol.Arity)
            .ToDictionary(group => group.Key, group => group.ToArray());
        if (!symbolsByArity.TryGetValue(0, out var terminalSymbols))
            throw new ArgumentException("At least one terminal symbol must be allowed.", nameof(symbols));
        TerminalSymbols = terminalSymbols.ToImmutableArray();
        maximumSymbolArity = symbolsByArity.Keys.Max();

        // Both sampler dictionaries slice the configured weights, so the SelectionWeights accessor rebuilds them.
        samplerByArity = CreateAritySamplers();
        samplerByArityRange = CreateCommonRangeSamplers();
        allowedVariableNames = Symbols
            .OfType<VariableSymbol>()
            .SelectMany(symbol => symbol.Variables)
            .ToHashSet(StringComparer.Ordinal);
        allowsVariables = allowedVariableNames.Count > 0;
        allowsEvolvableConstants = Symbols.OfType<EvolvableConstantSymbol>().Any();
    }

    public int MaximumLength
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    }

    public int MaximumDepth
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            field = value;
        }
    }

    public ValueArray<Symbol> Symbols => sampler.Items;
    public ImmutableArray<Symbol> TerminalSymbols { get; }

    /// <summary>
    /// Gets the configured selection weights, exactly as supplied, or an empty collection for uniform selection.
    /// Setting them reweights <see cref="Symbols"/> and rebuilds the derived per-arity samplers; an empty collection
    /// restores uniform selection.
    /// </summary>
    public ValueArray<double> SelectionWeights
    {
        get => sampler.Weights;
        init
        {
            sampler = sampler with { Weights = value };
            samplerByArity = CreateAritySamplers();
            samplerByArityRange = CreateCommonRangeSamplers();
        }
    }

    public bool AllowsVariables => allowsVariables;
    public bool AllowsEvolvableConstants => allowsEvolvableConstants;

    /// <remarks>
    /// Equality covers the configured limits, symbols and selection weights only. The remaining members are lookup
    /// structures derived from those values in the constructor, and comparing them would degrade to reference equality.
    /// </remarks>
    public bool Equals(ExpressionTreeSearchSpace? other) =>
        other is not null
        && MaximumLength == other.MaximumLength
        && MaximumDepth == other.MaximumDepth
        && Symbols.Equals(other.Symbols)
        && SelectionWeights.Equals(other.SelectionWeights);

    public override int GetHashCode() =>
        HashCode.Combine(MaximumLength, MaximumDepth, Symbols, SelectionWeights);

    public IReadOnlyList<Symbol> GetSymbols(int arity) =>
        symbolsByArity.TryGetValue(arity, out var symbols) ? symbols : [];

    public Symbol SelectSymbol(int arity, IRandomNumberGenerator random)
    {
        if (!samplerByArity.TryGetValue(arity, out var aritySampler))
            throw new ArgumentException($"No symbols with arity {arity} are allowed.", nameof(arity));

        return aritySampler.Sample(random);
    }

    public Symbol SelectSymbol(int minimumArity, int maximumArity, IRandomNumberGenerator random)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumArity);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumArity, minimumArity);

        if (minimumArity == maximumArity)
            return SelectSymbol(minimumArity, random);

        var effectiveMaximumArity = Math.Min(maximumArity, maximumSymbolArity);
        if (minimumArity == effectiveMaximumArity)
            return SelectSymbol(minimumArity, random);

        if (samplerByArityRange.TryGetValue((minimumArity, effectiveMaximumArity), out var rangeSampler))
            return rangeSampler.Sample(random);

        return CreateRangeSampler(minimumArity, maximumArity).Sample(random);
    }

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

        for (var i = 0; i < node.Arity; i++)
        {
            var child = node.GetChild(i);
            if (!ContainsNodeAndDescendants(child))
                return false;
        }

        return true;
    }

    private bool ContainsNode(ExpressionNode node) => node switch
    {
        VariableExpressionNode variable => variable.Symbol.Variables.All(allowedVariableNames.Contains),
        NumericConstantExpressionNode { Symbol: FixedConstantSymbol fixedConstant } constant => constant.Value.Equals(fixedConstant.Value) && Symbols.Contains(fixedConstant),
        NumericConstantExpressionNode { Symbol: EvolvableConstantSymbol } => AllowsEvolvableConstants,
        TerminalExpressionNode => Symbols.Contains(node.Symbol),
        _ => node.Symbol is OperationSymbol operation && Symbols.Contains(operation),
    };

    private WeightedItemSampler<Symbol> CreateRangeSampler(int minimumArity, int maximumArity)
    {
        var indexes = new List<int>();
        for (var i = 0; i < Symbols.Count; i++)
        {
            if (Symbols[i].Arity >= minimumArity && Symbols[i].Arity <= maximumArity)
                indexes.Add(i);
        }

        if (indexes.Count == 0)
            throw new ArgumentException($"No symbols with arity in [{minimumArity}, {maximumArity}] are allowed.", nameof(maximumArity));

        return CreateSampler(indexes);
    }

    private Dictionary<int, WeightedItemSampler<Symbol>> CreateAritySamplers() =>
        symbolsByArity.Keys.ToDictionary(
            arity => arity,
            arity => CreateSampler(Symbols
                .Select((symbol, index) => (Symbol: symbol, Index: index))
                .Where(entry => entry.Symbol.Arity == arity)
                .Select(entry => entry.Index)
                .ToArray()));

    private Dictionary<(int MinimumArity, int MaximumArity), WeightedItemSampler<Symbol>> CreateCommonRangeSamplers()
    {
        var samplers = new Dictionary<(int MinimumArity, int MaximumArity), WeightedItemSampler<Symbol>>();
        var precomputedMaximumArity = Math.Min(MaximumPrecomputedArity, maximumSymbolArity);
        for (var minimumArity = 0; minimumArity < precomputedMaximumArity; minimumArity++)
        {
            for (var maximumArity = minimumArity + 1; maximumArity <= precomputedMaximumArity; maximumArity++)
            {
                var indexes = Symbols
                    .Select((symbol, index) => (Symbol: symbol, Index: index))
                    .Where(entry => entry.Symbol.Arity >= minimumArity && entry.Symbol.Arity <= maximumArity)
                    .Select(entry => entry.Index)
                    .ToArray();
                if (indexes.Length > 0)
                    samplers.Add((minimumArity, maximumArity), CreateSampler(indexes));
            }
        }

        return samplers;
    }

    private WeightedItemSampler<Symbol> CreateSampler(IReadOnlyList<int> indexes) =>
        new(
            [.. indexes.Select(index => Symbols[index])],
            sampler.Weights.IsEmpty ? null : [.. indexes.Select(index => sampler.Weights[index])]);

    private static IReadOnlyList<Symbol> ComposeCommonSymbols(IEnumerable<OperationSymbol> operations, IEnumerable<string> variables, IEnumerable<ConstantSymbol> constants)
    {
        var symbols = new List<Symbol>();
        symbols.AddRange(operations);
        var variableArray = variables.ToArray();
        if (variableArray.Length > 0)
            symbols.Add(new VariableSymbol(variableArray));
        symbols.AddRange(constants);
        return symbols;
    }

    private static WeightedSymbolSet CreateWeightedSymbolSet(IReadOnlyList<(Symbol Symbol, double Weight)> symbols)
    {
        var entries = symbols.ToArray();
        return new WeightedSymbolSet(entries.Select(entry => entry.Symbol).ToArray(), entries.Select(entry => entry.Weight).ToArray());
    }

    private readonly record struct WeightedSymbolSet(Symbol[] Symbols, double[] Weights);
}
