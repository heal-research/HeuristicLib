namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

internal sealed class ExpressionCompilation : IExpressionEmitter
{
    private readonly ExpressionTree expression;
    private readonly List<Instruction> instructions;
    private readonly List<double> constants;
    private readonly List<VariableReference> variableReferences;
    private readonly Dictionary<string, int> variableIndexByName;
    private readonly bool optimize;
    private CompileNode[] compileStack;
    private int stackCount;
    private int rootIndex;

    internal ExpressionCompilation(ExpressionTree expression, bool optimize)
    {
        this.expression = expression;
        instructions = new List<Instruction>(expression.Length);
        constants = new List<double>(expression.Length);
        variableReferences = new List<VariableReference>(expression.Length);
        variableIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        this.optimize = optimize;
        compileStack = new CompileNode[expression.Length];
        stackCount = 0;
        rootIndex = -1;
    }

    internal CompiledExpressionTree Compile()
    {
        CompileSymbol(expression.RootLocation.InstructionIndex);
        return CreateWithCompactedPayloadTables();
    }

    public void EmitChild(int childIndex)
    {
        if (rootIndex < 0)
            throw new InvalidOperationException("Child emission is only available while compiling a symbol.");

        var childRootIndex = expression.GetChildRootIndex(rootIndex, childIndex);
        CompileSymbol(childRootIndex);
    }

    public void EmitVariable(string name)
    {
        if (!variableIndexByName.TryGetValue(name, out var variableIndex))
        {
            variableIndex = variableReferences.Count;
            variableIndexByName.Add(name, variableIndex);
            variableReferences.Add(new VariableReference(name, variableIndex));
        }

        instructions.Add(Instruction.Variable(variableIndex));
        Push(new CompileNode(CompileNodeKind.Variable, instructions.Count - 1, Length: 1, VariableName: name));
    }

    public void EmitConstant(double value)
    {
        var constantIndex = constants.Count;
        constants.Add(value);
        instructions.Add(Instruction.Constant(constantIndex));
        Push(new CompileNode(CompileNodeKind.Constant, instructions.Count - 1, Length: 1, ConstantValue: value));
    }

    public void EmitOperator(OpCode opCode)
    {
        var arity = OpCodes.GetArity(opCode);
        if (stackCount < arity)
            throw new InvalidOperationException($"Opcode {opCode} requires {arity} operands but only {stackCount} are available.");

        if (optimize)
        {
            if (TryOptimizeConstantFold(opCode, arity))
                return;

            if (TryOptimizeIdentityElimination(opCode, arity))
                return;

            // TODO: Consider x * 0 -> 0, 0 / x -> 0, and x / x -> 1 only after deciding the intended NaN, infinity, and divide-by-zero semantics.
            // TODO: Consider reassociation and associative constant combining, such as (x + 2) + 3 -> x + 5, only if the floating-point rounding change is acceptable.
            // TODO: Consider broader algebraic simplifications, such as inverse-function rewrites, only after domain and semantic rules are explicit.
        }

        if (arity == 1)
        {
            var child = Pop();
            instructions.Add(Instruction.Unary(opCode, child.Length));
            Push(new CompileNode(CompileNodeKind.Complex, instructions.Count - 1, child.Length + 1));
            return;
        }

        if (arity == 2)
        {
            var right = Pop();
            var left = Pop();
            instructions.Add(Instruction.Binary(opCode, left.Length, right.Length));
            Push(new CompileNode(CompileNodeKind.Complex, instructions.Count - 1, left.Length + right.Length + 1));
            return;
        }

        throw new InvalidOperationException($"Opcode {opCode} with arity {arity} is not supported by the symbolic expression compiler.");
    }

    private void CompileSymbol(int index)
    {
        var previousRootIndex = rootIndex;
        rootIndex = index;
        var node = expression.GetNode(index);
        node.Symbol.EmitNode(node, this);
        rootIndex = previousRootIndex;
    }

    private bool TryOptimizeConstantFold(OpCode opCode, int arity)
    {
        if (arity == 1)
        {
            var child = Peek();
            if (child.Kind != CompileNodeKind.Constant)
                return false;

            var foldedUnary = opCode switch
            {
                OpCode.Negate => -child.ConstantValue,
                OpCode.Exp => Math.Exp(child.ConstantValue),
                OpCode.Log => Math.Log(child.ConstantValue),
                OpCode.Sqrt => Math.Sqrt(child.ConstantValue),
                _ => double.NaN
            };

            if (double.IsNaN(foldedUnary) && opCode is not (OpCode.Negate or OpCode.Exp or OpCode.Log or OpCode.Sqrt))
                return false;

            RollBackTo(child.StartIndex);
            Pop();
            EmitConstant(foldedUnary);
            return true;
        }

        if (arity != 2)
            return false;

        var right = Peek();
        var left = compileStack[stackCount - 2];
        if (left.Kind != CompileNodeKind.Constant || right.Kind != CompileNodeKind.Constant)
            return false;

        var foldedBinary = opCode switch
        {
            OpCode.Add => left.ConstantValue + right.ConstantValue,
            OpCode.Subtract => left.ConstantValue - right.ConstantValue,
            OpCode.Multiply => left.ConstantValue * right.ConstantValue,
            OpCode.Divide => left.ConstantValue / right.ConstantValue,
            _ => double.NaN
        };

        if (double.IsNaN(foldedBinary) && opCode is not (OpCode.Add or OpCode.Subtract or OpCode.Multiply or OpCode.Divide))
            return false;

        RollBackTo(left.StartIndex);
        Pop();
        Pop();
        EmitConstant(foldedBinary);
        return true;
    }

    private bool TryOptimizeIdentityElimination(OpCode opCode, int arity)
    {
        if (arity != 2 || stackCount < 2)
            return false;

        var right = Peek();
        var left = compileStack[stackCount - 2];

        if (IsRightIdentity(opCode, right))
        {
            RollBackTo(right.StartIndex);
            Pop();
            return true;
        }

        if (!IsLeftIdentity(opCode, left))
            return false;

        var sourceStart = right.StartIndex;
        var sourceLength = right.Length;
        var targetStart = left.StartIndex;
        MoveInstructionSpan(sourceStart, sourceLength, targetStart);
        Pop();
        Pop();
        Push(right with { StartIndex = targetStart });
        return true;
    }

    private static bool IsRightIdentity(OpCode opCode, CompileNode node) =>
        node.Kind == CompileNodeKind.Constant
        && ((opCode is OpCode.Add or OpCode.Subtract && node.ConstantValue == 0.0)
            || (opCode is OpCode.Multiply or OpCode.Divide && node.ConstantValue == 1.0));

    private static bool IsLeftIdentity(OpCode opCode, CompileNode node) =>
        node.Kind == CompileNodeKind.Constant
        && ((opCode == OpCode.Add && node.ConstantValue == 0.0)
            || (opCode == OpCode.Multiply && node.ConstantValue == 1.0));

    private void MoveInstructionSpan(int sourceStart, int length, int targetStart)
    {
        for (var i = 0; i < length; i++)
        {
            instructions[targetStart + i] = instructions[sourceStart + i];
        }

        RollBackTo(targetStart + length);
    }

    private void RollBackTo(int instructionCount)
    {
        instructions.RemoveRange(instructionCount, instructions.Count - instructionCount);
    }

    private void Push(CompileNode node)
    {
        if (stackCount == compileStack.Length)
            Array.Resize(ref compileStack, compileStack.Length * 2);

        compileStack[stackCount++] = node;
    }

    private CompileNode Pop()
    {
        return compileStack[--stackCount];
    }

    private CompileNode Peek()
    {
        return compileStack[stackCount - 1];
    }

    private CompiledExpressionTree CreateWithCompactedPayloadTables()
    {
        var compactedConstants = new List<double>();
        var compactedVariableReferences = new List<VariableReference>();
        var compactedVariableIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
        var compactedInstructions = new Instruction[instructions.Count];

        for (var i = 0; i < instructions.Count; i++)
        {
            var instruction = instructions[i];
            switch (OpCodes.GetPayloadKind(instruction.OpCode))
            {
                case PayloadKind.Constant:
                    var constant = constants[instruction.PayloadIndex];
                    var constantIndex = compactedConstants.IndexOf(constant);
                    if (constantIndex < 0)
                    {
                        constantIndex = compactedConstants.Count;
                        compactedConstants.Add(constant);
                    }

                    compactedInstructions[i] = instruction with { PayloadIndex = constantIndex };
                    break;
                case PayloadKind.VariableReference:
                    var variableName = variableReferences[instruction.PayloadIndex].Name;
                    if (!compactedVariableIndexByName.TryGetValue(variableName, out var variableIndex))
                    {
                        variableIndex = compactedVariableReferences.Count;
                        compactedVariableIndexByName.Add(variableName, variableIndex);
                        compactedVariableReferences.Add(new VariableReference(variableName, variableIndex));
                    }

                    compactedInstructions[i] = instruction with { PayloadIndex = variableIndex };
                    break;
                default:
                    compactedInstructions[i] = instruction;
                    break;
            }
        }

        return CompiledExpressionTree.FromOwnedArrays(
            compactedInstructions,
            compactedConstants.ToArray(),
            compactedVariableReferences.ToArray());
    }

    internal readonly record struct CompileNode(CompileNodeKind Kind, int StartIndex, int Length, double ConstantValue = 0.0, string? VariableName = null);

    internal enum CompileNodeKind
    {
        Constant,
        Variable,
        Complex
    }
}
