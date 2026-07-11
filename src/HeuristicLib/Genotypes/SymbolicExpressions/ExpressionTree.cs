namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public sealed class ExpressionTree : IEquatable<ExpressionTree>
{
    private readonly ExpressionNode[] nodes;
    private readonly int[] subtreeLengths;
    private readonly int hashCode;

    private ExpressionTree(ExpressionNode[] nodes, int[] subtreeLengths, bool takeOwnership)
    {
        this.nodes = takeOwnership ? nodes : nodes.ToArray();
        this.subtreeLengths = takeOwnership ? subtreeLengths : subtreeLengths.ToArray();

        Depth = ValidateAndCalculateDepth(this.nodes, this.subtreeLengths);
        hashCode = CalculateHashCode();
    }

    internal int NodeCount => nodes.Length;
    public int Length => nodes.Length;
    public int Complexity => Length;
    public int Depth { get; }
    public ExpressionSubtree Root => CreateSubtree(nodes.Length - 1);
    public ExpressionLocation RootLocation => new(nodes.Length - 1);
    public IEnumerable<ExpressionSubtree> TraversePreOrder() => Root.TraversePreOrder();
    public IEnumerable<ExpressionSubtree> TraversePostOrder() => Root.TraversePostOrder();
    public IEnumerable<ExpressionSubtree> TraverseBreadthFirst() => Root.TraverseBreadthFirst();

    public static ExpressionTree Create(IEnumerable<ExpressionNode> nodes)
    {
        var tokenArray = nodes.ToArray();
        var subtreeLengths = CalculateSubtreeLengths(tokenArray);
        return new ExpressionTree(tokenArray, subtreeLengths, takeOwnership: true);
    }

    internal static ExpressionTree FromOwnedArrays(ExpressionNode[] nodes, int[] subtreeLengths)
    {
        return new ExpressionTree(nodes, subtreeLengths, takeOwnership: true);
    }

    public CompiledExpressionTree Compile(bool optimize = true)
    {
        return ExpressionCompiler.Compile(this, optimize);
    }

    public ExpressionSubtree GetSubtree(ExpressionLocation location)
    {
        return CreateSubtree(location.InstructionIndex);
    }

    public ExpressionTree WithNode(ExpressionLocation location, ExpressionNode node)
    {
        return WithNode(location.InstructionIndex, node);
    }

    internal ExpressionTree WithNode(int index, ExpressionNode node)
    {
        ValidateIndex(index);
        if (node.Arity != nodes[index].Arity)
            throw new ArgumentException($"Replacement node arity {node.Arity} must match selected node arity {nodes[index].Arity}.", nameof(node));

        var newTokens = nodes.ToArray();
        newTokens[index] = node;
        return FromOwnedArrays(newTokens, subtreeLengths);
    }

    public ExpressionTree WithVariable(ExpressionLocation location, VariableSymbol symbol, string variableName)
    {
        return WithNode(location, new ExpressionNode(symbol, variableName));
    }

    public ExpressionTree WithConstant(ExpressionLocation location, ConstantSymbol symbol, double value)
    {
        return WithNode(location, new ExpressionNode(symbol, value));
    }

    public ExpressionTree ReplaceSubtree(ExpressionLocation location, ExpressionTree replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        return ReplaceSubtree(location.InstructionIndex, replacement);
    }

    internal ExpressionTree ReplaceSubtree(int rootIndex, ExpressionTree replacement)
    {
        ValidateIndex(rootIndex);

        var replacedLength = subtreeLengths[rootIndex];
        var replacedStart = rootIndex - replacedLength + 1;
        var newLength = nodes.Length - replacedLength + replacement.nodes.Length;
        var splicedTokens = new ExpressionNode[newLength];

        nodes.AsSpan(0, replacedStart).CopyTo(splicedTokens);
        replacement.nodes.AsSpan().CopyTo(splicedTokens.AsSpan(replacedStart));
        nodes.AsSpan(rootIndex + 1).CopyTo(splicedTokens.AsSpan(replacedStart + replacement.nodes.Length));

        return Create(splicedTokens);
    }

    internal ExpressionSubtree CreateSubtree(int rootIndex)
    {
        ValidateIndex(rootIndex);

        var length = subtreeLengths[rootIndex];
        var start = rootIndex - length + 1;
        return new ExpressionSubtree(this, start, length, rootIndex);
    }

    internal ExpressionNode GetNode(int index) => nodes[index];

    internal int GetSubtreeLength(int index) => subtreeLengths[index];

    internal int GetChildRootIndex(int rootIndex, int childIndex)
    {
        var arity = nodes[rootIndex].Arity;
        if ((uint)childIndex >= (uint)arity)
            throw new ArgumentOutOfRangeException(nameof(childIndex));

        var childRootIndex = rootIndex - 1;
        for (var i = arity - 1; i > childIndex; i--)
        {
            childRootIndex -= subtreeLengths[childRootIndex];
        }

        return childRootIndex;
    }

    public string ToInfixString()
    {
        var stack = new Stack<string>();
        foreach (var node in nodes)
        {
            switch (node.Symbol)
            {
                case VariableSymbol:
                    stack.Push(node.VariableName!);
                    break;
                case ConstantSymbol:
                    stack.Push(node.NumericValue.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case BuiltInOperationSymbol operation when operation.Arity == 1:
                    PushUnary(stack, operation.Name);
                    break;
                case BuiltInOperationSymbol operation when operation.Arity == 2:
                    PushBinary(stack, operation.Name);
                    break;
                default:
                    PushFunctionCall(stack, node);
                    break;
            }
        }

        return stack.Single();
    }

    public override string ToString() => ToInfixString();

    public bool Equals(ExpressionTree? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return hashCode == other.hashCode
               && nodes.SequenceEqual(other.nodes)
               && subtreeLengths.SequenceEqual(other.subtreeLengths);
    }

    public override bool Equals(object? obj) => obj is ExpressionTree other && Equals(other);

    public override int GetHashCode() => hashCode;

    public static bool operator ==(ExpressionTree? left, ExpressionTree? right) => Equals(left, right);

    public static bool operator !=(ExpressionTree? left, ExpressionTree? right) => !Equals(left, right);

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

    private static void PushFunctionCall(Stack<string> stack, ExpressionNode node)
    {
        var arguments = new string[node.Arity];
        for (var i = node.Arity - 1; i >= 0; i--)
        {
            arguments[i] = stack.Pop();
        }

        stack.Push($"{node.Name}({string.Join(", ", arguments)})");
    }

    private static int[] CalculateSubtreeLengths(ExpressionNode[] nodes)
    {
        if (nodes.Length == 0)
            throw new ArgumentException("Expression must contain at least one node.", nameof(nodes));

        var lengths = new int[nodes.Length];
        var stack = new Stack<int>();
        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (stack.Count < node.Arity)
                throw new ArgumentException($"Node {i} requires {node.Arity} operands but only {stack.Count} are available.", nameof(nodes));

            var length = 1;
            for (var j = 0; j < node.Arity; j++)
            {
                length += stack.Pop();
            }

            lengths[i] = length;
            stack.Push(length);
        }

        if (stack.Count != 1)
            throw new ArgumentException("Expression nodes must contain exactly one root expression.", nameof(nodes));

        return lengths;
    }

    private static int ValidateAndCalculateDepth(ExpressionNode[] nodes, int[] subtreeLengths)
    {
        if (nodes.Length == 0)
            throw new ArgumentException("Expression must contain at least one node.", nameof(nodes));

        if (nodes.Length != subtreeLengths.Length)
            throw new ArgumentException("Subtree length table must have the same length as the node table.", nameof(subtreeLengths));

        var stack = new Stack<SubtreeState>();
        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (stack.Count < node.Arity)
                throw new ArgumentException($"Node {i} requires {node.Arity} operands but only {stack.Count} are available.", nameof(nodes));

            var subtreeLength = 1;
            var depth = 1;
            for (var j = 0; j < node.Arity; j++)
            {
                var child = stack.Pop();
                subtreeLength += child.Length;
                depth = Math.Max(depth, child.Depth + 1);
            }

            if (subtreeLengths[i] != subtreeLength)
            {
                throw new ArgumentException(
                  $"Node {i} declares subtree length {subtreeLengths[i]} but calculated length is {subtreeLength}.",
                  nameof(subtreeLengths));
            }

            stack.Push(new SubtreeState(subtreeLength, depth));
        }

        if (stack.Count != 1)
            throw new ArgumentException("Expression nodes must contain exactly one root expression.", nameof(nodes));

        return stack.Pop().Depth;
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)nodes.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
    }

    private int CalculateHashCode()
    {
        var hash = new HashCode();
        foreach (var node in nodes)
        {
            hash.Add(node);
        }

        foreach (var subtreeLength in subtreeLengths)
        {
            hash.Add(subtreeLength);
        }

        return hash.ToHashCode();
    }

    private readonly record struct SubtreeState(int Length, int Depth);
}
