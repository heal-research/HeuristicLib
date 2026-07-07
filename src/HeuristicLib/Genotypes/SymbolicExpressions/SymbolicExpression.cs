namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class SymbolicExpression : IEquatable<SymbolicExpression>
{
    private readonly Symbol[] symbols;
    private readonly int[] subtreeLengths;
    private readonly int hashCode;

    private SymbolicExpression(Symbol[] symbols, int[] subtreeLengths, bool takeOwnership)
    {
        this.symbols = takeOwnership ? symbols : symbols.ToArray();
        this.subtreeLengths = takeOwnership ? subtreeLengths : subtreeLengths.ToArray();

        Depth = ValidateAndCalculateDepth(this.symbols, this.subtreeLengths);
        hashCode = CalculateHashCode();
    }

    internal int SymbolCount => symbols.Length;
    public int Length => symbols.Length;
    public int Complexity => Length;
    public int Depth { get; }
    public SymbolicSubExpression Root => CreateSubExpression(symbols.Length - 1);
    public SymbolicExpressionLocation RootLocation => new(symbols.Length - 1);
    public IEnumerable<SymbolicSubExpression> TraversePreOrder() => Root.TraversePreOrder();
    public IEnumerable<SymbolicSubExpression> TraversePostOrder() => Root.TraversePostOrder();
    public IEnumerable<SymbolicSubExpression> TraverseBreadthFirst() => Root.TraverseBreadthFirst();

    public static SymbolicExpression Create(IEnumerable<Symbol> symbols)
    {
        var symbolArray = symbols.ToArray();
        var subtreeLengths = CalculateSubtreeLengths(symbolArray);
        return new SymbolicExpression(symbolArray, subtreeLengths, takeOwnership: true);
    }

    internal static SymbolicExpression FromOwnedArrays(Symbol[] symbols, int[] subtreeLengths)
    {
        return new SymbolicExpression(symbols, subtreeLengths, takeOwnership: true);
    }

    public CompiledSymbolicExpression Compile(bool optimize = true)
    {
        return SymbolicExpressionCompiler.Compile(this, optimize);
    }

    public SymbolicSubExpression GetSubExpression(SymbolicExpressionLocation location)
    {
        return CreateSubExpression(location.InstructionIndex);
    }

    public SymbolicExpression WithSymbol(SymbolicExpressionLocation location, Symbol symbol)
    {
        return WithSymbol(location.InstructionIndex, symbol);
    }

    internal SymbolicExpression WithSymbol(int symbolIndex, Symbol symbol)
    {
        ValidateSymbolIndex(symbolIndex);
        if (symbol.Arity != symbols[symbolIndex].Arity)
            throw new ArgumentException($"Replacement symbol arity {symbol.Arity} must match selected symbol arity {symbols[symbolIndex].Arity}.", nameof(symbol));

        var newSymbols = symbols.ToArray();
        newSymbols[symbolIndex] = symbol;
        return FromOwnedArrays(newSymbols, subtreeLengths);
    }

    public SymbolicExpression WithVariable(SymbolicExpressionLocation location, string variableName)
    {
        return WithSymbol(location, new VariableSymbol(variableName));
    }

    public SymbolicExpression WithNumericLiteral(SymbolicExpressionLocation location, NumericLiteral literal)
    {
        return WithSymbol(location, new NumericLiteralSymbol(literal));
    }

    public SymbolicExpression WithNumericLiteral(SymbolicExpressionLocation location, double value)
    {
        return WithNumericLiteral(location, new NumericLiteral(value, NumericLiteralKind.Optimizable));
    }

    public SymbolicExpression ReplaceSubExpression(SymbolicExpressionLocation location, SymbolicExpression replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        return ReplaceSubExpression(location.InstructionIndex, replacement);
    }

    internal SymbolicExpression ReplaceSubExpression(int rootSymbolIndex, SymbolicExpression replacement)
    {
        ValidateSymbolIndex(rootSymbolIndex);

        var replacedLength = subtreeLengths[rootSymbolIndex];
        var replacedStart = rootSymbolIndex - replacedLength + 1;
        var newLength = symbols.Length - replacedLength + replacement.symbols.Length;
        var splicedSymbols = new Symbol[newLength];

        symbols.AsSpan(0, replacedStart).CopyTo(splicedSymbols);
        replacement.symbols.AsSpan().CopyTo(splicedSymbols.AsSpan(replacedStart));
        symbols.AsSpan(rootSymbolIndex + 1).CopyTo(splicedSymbols.AsSpan(replacedStart + replacement.symbols.Length));

        return Create(splicedSymbols);
    }

    internal SymbolicSubExpression CreateSubExpression(int rootSymbolIndex)
    {
        ValidateSymbolIndex(rootSymbolIndex);

        var length = subtreeLengths[rootSymbolIndex];
        var start = rootSymbolIndex - length + 1;
        return new SymbolicSubExpression(this, start, length, rootSymbolIndex);
    }

    internal Symbol GetSymbol(int symbolIndex) => symbols[symbolIndex];

    internal int GetSubtreeLength(int symbolIndex) => subtreeLengths[symbolIndex];

    internal int GetChildRootIndex(int rootSymbolIndex, int childIndex)
    {
        var arity = symbols[rootSymbolIndex].Arity;
        if ((uint)childIndex >= (uint)arity)
            throw new ArgumentOutOfRangeException(nameof(childIndex));

        var childRootIndex = rootSymbolIndex - 1;
        for (var i = arity - 1; i > childIndex; i--)
        {
            childRootIndex -= subtreeLengths[childRootIndex];
        }

        return childRootIndex;
    }

    public string ToInfixString()
    {
        var stack = new Stack<string>();
        foreach (var symbol in symbols)
        {
            switch (symbol)
            {
                case VariableSymbol variable:
                    stack.Push(variable.VariableName);
                    break;
                case NumericLiteralSymbol literal:
                    stack.Push(literal.Literal.Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case PrimitiveSymbol primitive when primitive.Arity == 1:
                    PushUnary(stack, primitive.Name);
                    break;
                case PrimitiveSymbol primitive when primitive.Arity == 2:
                    PushBinary(stack, primitive.Name);
                    break;
                default:
                    PushFunctionCall(stack, symbol);
                    break;
            }
        }

        return stack.Single();
    }

    public override string ToString() => ToInfixString();

    public bool Equals(SymbolicExpression? other)
    {
        return other is not null
               && (ReferenceEquals(this, other)
                   || hashCode == other.hashCode
                   && symbols.SequenceEqual(other.symbols)
                   && subtreeLengths.SequenceEqual(other.subtreeLengths));
    }

    public override bool Equals(object? obj) => obj is SymbolicExpression other && Equals(other);

    public override int GetHashCode() => hashCode;

    public static bool operator ==(SymbolicExpression? left, SymbolicExpression? right) => Equals(left, right);

    public static bool operator !=(SymbolicExpression? left, SymbolicExpression? right) => !Equals(left, right);

    private static void PushBinary(Stack<string> stack, string op)
    {
        var right = stack.Pop();
        var left = stack.Pop();
        stack.Push($"({left} {op} {right})");
    }

    private static void PushUnary(Stack<string> stack, string functionName)
    {
        var child = stack.Pop();
        stack.Push($"{functionName}({child})");
    }

    private static void PushFunctionCall(Stack<string> stack, Symbol symbol)
    {
        var arguments = new string[symbol.Arity];
        for (var i = symbol.Arity - 1; i >= 0; i--)
        {
            arguments[i] = stack.Pop();
        }

        stack.Push($"{symbol.Name}({string.Join(", ", arguments)})");
    }

    private static int[] CalculateSubtreeLengths(Symbol[] symbols)
    {
        if (symbols.Length == 0)
            throw new ArgumentException("Expression must contain at least one symbol.", nameof(symbols));

        var lengths = new int[symbols.Length];
        var stack = new Stack<int>();
        for (var i = 0; i < symbols.Length; i++)
        {
            var symbol = symbols[i];
            if (stack.Count < symbol.Arity)
                throw new ArgumentException($"Symbol {i} requires {symbol.Arity} operands but only {stack.Count} are available.", nameof(symbols));

            var length = 1;
            for (var j = 0; j < symbol.Arity; j++)
            {
                length += stack.Pop();
            }

            lengths[i] = length;
            stack.Push(length);
        }

        if (stack.Count != 1)
            throw new ArgumentException("Expression symbols must contain exactly one root expression.", nameof(symbols));

        return lengths;
    }

    private static int ValidateAndCalculateDepth(Symbol[] symbols, int[] subtreeLengths)
    {
        if (symbols.Length == 0)
            throw new ArgumentException("Expression must contain at least one symbol.", nameof(symbols));

        if (symbols.Length != subtreeLengths.Length)
            throw new ArgumentException("Subtree length table must have the same length as the symbol table.", nameof(subtreeLengths));

        var stack = new Stack<SubtreeState>();
        for (var i = 0; i < symbols.Length; i++)
        {
            var symbol = symbols[i];
            if (stack.Count < symbol.Arity)
                throw new ArgumentException($"Symbol {i} requires {symbol.Arity} operands but only {stack.Count} are available.", nameof(symbols));

            var subtreeLength = 1;
            var depth = 1;
            for (var j = 0; j < symbol.Arity; j++)
            {
                var child = stack.Pop();
                subtreeLength += child.Length;
                depth = Math.Max(depth, child.Depth + 1);
            }

            if (subtreeLengths[i] != subtreeLength)
            {
                throw new ArgumentException(
                  $"Symbol {i} declares subtree length {subtreeLengths[i]} but calculated length is {subtreeLength}.",
                  nameof(subtreeLengths));
            }

            stack.Push(new SubtreeState(subtreeLength, depth));
        }

        if (stack.Count != 1)
            throw new ArgumentException("Expression symbols must contain exactly one root expression.", nameof(symbols));

        return stack.Pop().Depth;
    }

    private void ValidateSymbolIndex(int symbolIndex)
    {
        if ((uint)symbolIndex >= (uint)symbols.Length)
            throw new ArgumentOutOfRangeException(nameof(symbolIndex));
    }

    private int CalculateHashCode()
    {
        var hash = new HashCode();
        foreach (var symbol in symbols)
        {
            hash.Add(symbol);
        }

        foreach (var subtreeLength in subtreeLengths)
        {
            hash.Add(subtreeLength);
        }

        return hash.ToHashCode();
    }

    private readonly record struct SubtreeState(int Length, int Depth);
}
