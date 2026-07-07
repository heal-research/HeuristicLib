namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public ref struct SymbolCompilationContext
{
    private readonly SymbolicExpression expression;
    private readonly List<ExpressionInstruction> instructions;
    private readonly List<NumericLiteral> numericLiterals;
    private readonly List<VariableReference> variableReferences;
    private readonly Dictionary<string, int> variableIndexByName;
    private readonly bool optimize;
    private CompileNode[] compileStack;
    private int stackCount;
    private int rootSymbolIndex;

    internal SymbolCompilationContext(
        SymbolicExpression expression,
        List<ExpressionInstruction> instructions,
        List<NumericLiteral> numericLiterals,
        List<VariableReference> variableReferences,
        Dictionary<string, int> variableIndexByName,
        CompileNode[] compileStack,
        bool optimize)
    {
        this.expression = expression;
        this.instructions = instructions;
        this.numericLiterals = numericLiterals;
        this.variableReferences = variableReferences;
        this.variableIndexByName = variableIndexByName;
        this.optimize = optimize;
        this.compileStack = compileStack;
        stackCount = 0;
        rootSymbolIndex = -1;
    }

    public void EmitChild(int childIndex)
    {
        if (rootSymbolIndex < 0)
            throw new InvalidOperationException("Child emission is only available while compiling a symbol.");

        var childRootIndex = expression.GetChildRootIndex(rootSymbolIndex, childIndex);
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

        instructions.Add(ExpressionInstruction.Variable(variableIndex));
        Push(new CompileNode(CompileNodeKind.Variable, instructions.Count - 1, Length: 1, VariableName: name));
    }

    public void EmitNumericLiteral(NumericLiteral literal)
    {
        var literalIndex = numericLiterals.Count;
        numericLiterals.Add(literal);
        instructions.Add(ExpressionInstruction.NumericLiteral(literalIndex));
        Push(new CompileNode(CompileNodeKind.Constant, instructions.Count - 1, Length: 1, ConstantValue: literal.Value, IsFixedConstant: literal.Kind == NumericLiteralKind.Fixed));
    }

    public void EmitOperator(SymbolicExpressionOpCode opCode)
    {
        var arity = SymbolicExpressionOpCodes.GetArity(opCode);
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
            instructions.Add(ExpressionInstruction.Unary(opCode, child.Length));
            Push(new CompileNode(CompileNodeKind.Complex, instructions.Count - 1, child.Length + 1));
            return;
        }

        if (arity == 2)
        {
            var right = Pop();
            var left = Pop();
            instructions.Add(ExpressionInstruction.Binary(opCode, left.Length, right.Length));
            Push(new CompileNode(CompileNodeKind.Complex, instructions.Count - 1, left.Length + right.Length + 1));
            return;
        }

        throw new InvalidOperationException($"Opcode {opCode} with arity {arity} is not supported by the symbolic expression compiler.");
    }

    internal void CompileSymbol(int symbolIndex)
    {
        var previousRootIndex = rootSymbolIndex;
        rootSymbolIndex = symbolIndex;
        expression.GetSymbol(symbolIndex).Emit(ref this);
        rootSymbolIndex = previousRootIndex;
    }

    private bool TryOptimizeConstantFold(SymbolicExpressionOpCode opCode, int arity)
    {
        if (arity == 1)
        {
            var child = Peek();
            if (child.Kind != CompileNodeKind.Constant || !child.IsFixedConstant)
                return false;

            var foldedUnary = opCode switch
            {
                SymbolicExpressionOpCode.Negate => -child.ConstantValue,
                SymbolicExpressionOpCode.Exp => Math.Exp(child.ConstantValue),
                SymbolicExpressionOpCode.Log => Math.Log(child.ConstantValue),
                SymbolicExpressionOpCode.Sqrt => Math.Sqrt(child.ConstantValue),
                _ => double.NaN
            };

            if (double.IsNaN(foldedUnary) && opCode is not (SymbolicExpressionOpCode.Negate or SymbolicExpressionOpCode.Exp or SymbolicExpressionOpCode.Log or SymbolicExpressionOpCode.Sqrt))
                return false;

            RollBackTo(child.StartIndex);
            Pop();
            EmitNumericLiteral(new NumericLiteral(foldedUnary, NumericLiteralKind.Fixed));
            return true;
        }

        if (arity != 2)
            return false;

        var right = Peek();
        var left = compileStack[stackCount - 2];
        if (left.Kind != CompileNodeKind.Constant || right.Kind != CompileNodeKind.Constant || !left.IsFixedConstant || !right.IsFixedConstant)
            return false;

        var foldedBinary = opCode switch
        {
            SymbolicExpressionOpCode.Add => left.ConstantValue + right.ConstantValue,
            SymbolicExpressionOpCode.Subtract => left.ConstantValue - right.ConstantValue,
            SymbolicExpressionOpCode.Multiply => left.ConstantValue * right.ConstantValue,
            SymbolicExpressionOpCode.Divide => left.ConstantValue / right.ConstantValue,
            _ => double.NaN
        };

        if (double.IsNaN(foldedBinary) && opCode is not (SymbolicExpressionOpCode.Add or SymbolicExpressionOpCode.Subtract or SymbolicExpressionOpCode.Multiply or SymbolicExpressionOpCode.Divide))
            return false;

        RollBackTo(left.StartIndex);
        Pop();
        Pop();
        EmitNumericLiteral(new NumericLiteral(foldedBinary, NumericLiteralKind.Fixed));
        return true;
    }

    private bool TryOptimizeIdentityElimination(SymbolicExpressionOpCode opCode, int arity)
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

    private static bool IsRightIdentity(SymbolicExpressionOpCode opCode, CompileNode node) =>
        node.Kind == CompileNodeKind.Constant
        && node.IsFixedConstant
        && ((opCode is SymbolicExpressionOpCode.Add or SymbolicExpressionOpCode.Subtract && node.ConstantValue == 0.0)
            || (opCode is SymbolicExpressionOpCode.Multiply or SymbolicExpressionOpCode.Divide && node.ConstantValue == 1.0));

    private static bool IsLeftIdentity(SymbolicExpressionOpCode opCode, CompileNode node) =>
        node.Kind == CompileNodeKind.Constant
        && node.IsFixedConstant
        && ((opCode == SymbolicExpressionOpCode.Add && node.ConstantValue == 0.0)
            || (opCode == SymbolicExpressionOpCode.Multiply && node.ConstantValue == 1.0));

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

    internal readonly record struct CompileNode(CompileNodeKind Kind, int StartIndex, int Length, double ConstantValue = 0.0, bool IsFixedConstant = false, string? VariableName = null);

    internal enum CompileNodeKind
    {
        Constant,
        Variable,
        Complex
    }
}
