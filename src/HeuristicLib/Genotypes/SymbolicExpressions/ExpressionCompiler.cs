namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public static class ExpressionCompiler
{
    public static CompiledExpression Compile(ExpressionTree expression, bool optimize = true)
    {
        return new Compilation(expression, optimize).Run();
    }

    private sealed class Compilation : IExpressionEmitter
    {
        private readonly ExpressionTree expression;
        private readonly List<Instruction> instructions;
        private readonly List<double> constants;
        private readonly List<VariableReference> variableReferences;
        private readonly Dictionary<string, int> variableIndexByName;
        private readonly bool optimize;
        private CompileNode[] compileStack;
        private ExpressionNode? current;
        private int stackCount;

        internal Compilation(ExpressionTree expression, bool optimize)
        {
            this.expression = expression;
            this.optimize = optimize;
            instructions = new List<Instruction>(expression.Length);
            constants = new List<double>(expression.Length);
            variableReferences = new List<VariableReference>(expression.Length);
            variableIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);
            compileStack = new CompileNode[Math.Max(1, expression.Length)];
        }

        internal CompiledExpression Run()
        {
            CompileExpressionNode(expression.Root);
            return BuildProgram();
        }

        public void EmitChild(int childIndex)
        {
            if (current is null)
                throw new InvalidOperationException("Child emission is only available while compiling a symbol.");

            var child = current.GetChild(childIndex);
            CompileExpressionNode(child);
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
            Push(new CompileNode(CompileNodeKind.Variable, instructions.Count - 1, 1));
        }

        public void EmitConstant(double value)
        {
            var constantIndex = constants.Count;
            constants.Add(value);
            instructions.Add(Instruction.Constant(constantIndex));
            Push(new CompileNode(CompileNodeKind.Constant, instructions.Count - 1, 1, value));
        }

        public void EmitOperator(OpCode opCode)
        {
            var arity = OpCodes.GetArity(opCode);
            if (stackCount < arity)
                throw new InvalidOperationException($"Opcode {opCode} requires {arity} operands but only {stackCount} are available.");

            if (optimize && (TryOptimizeConstantFold(opCode, arity) || TryOptimizeIdentityElimination(opCode, arity)))
                return;

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

        private void CompileExpressionNode(ExpressionNode node)
        {
            var previous = current;
            current = node;
            try
            {
                node.Symbol.Emit(node, this);
            }
            finally
            {
                current = previous;
            }
        }

        private CompiledExpression BuildProgram()
        {
            if (stackCount != 1)
                throw new InvalidOperationException($"Compilation must produce exactly one expression but produced {stackCount}.");

            var compactedConstants = new List<double>();
            var compactedVariables = new List<VariableReference>();
            var compactedVariableIndices = new Dictionary<string, int>(StringComparer.Ordinal);
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
                        if (!compactedVariableIndices.TryGetValue(variableName, out var variableIndex))
                        {
                            variableIndex = compactedVariables.Count;
                            compactedVariableIndices.Add(variableName, variableIndex);
                            compactedVariables.Add(new VariableReference(variableName, variableIndex));
                        }

                        compactedInstructions[i] = instruction with { PayloadIndex = variableIndex };
                        break;
                    default:
                        compactedInstructions[i] = instruction;
                        break;
                }
            }

            return CompiledExpression.FromOwnedArrays(compactedInstructions, compactedConstants.ToArray(), compactedVariables.ToArray());
        }

        private bool TryOptimizeConstantFold(OpCode opCode, int arity)
        {
            if (arity == 1)
            {
                var child = Peek();
                if (child.Kind != CompileNodeKind.Constant)
                    return false;

                var supported = true;
                var value = opCode switch
                {
                    OpCode.Negate => -child.ConstantValue,
                    OpCode.Exp => Math.Exp(child.ConstantValue),
                    OpCode.Log => Math.Log(child.ConstantValue),
                    OpCode.Sqrt => Math.Sqrt(child.ConstantValue),
                    _ => Unsupported()
                };
                if (!supported)
                    return false;

                RollBackTo(child.RootIndex);
                Pop();
                EmitConstant(value);
                return true;

                double Unsupported()
                {
                    supported = false;
                    return 0.0;
                }
            }

            if (arity != 2)
                return false;

            var right = Peek();
            var left = compileStack[stackCount - 2];
            if (left.Kind != CompileNodeKind.Constant || right.Kind != CompileNodeKind.Constant)
                return false;

            var supportedBinary = true;
            var folded = opCode switch
            {
                OpCode.Add => left.ConstantValue + right.ConstantValue,
                OpCode.Subtract => left.ConstantValue - right.ConstantValue,
                OpCode.Multiply => left.ConstantValue * right.ConstantValue,
                OpCode.Divide => left.ConstantValue / right.ConstantValue,
                _ => UnsupportedBinary()
            };
            if (!supportedBinary)
                return false;

            RollBackTo(left.RootIndex);
            Pop();
            Pop();
            EmitConstant(folded);
            return true;

            double UnsupportedBinary()
            {
                supportedBinary = false;
                return 0.0;
            }
        }

        private bool TryOptimizeIdentityElimination(OpCode opCode, int arity)
        {
            if (arity != 2 || stackCount < 2)
                return false;

            var right = Peek();
            var left = compileStack[stackCount - 2];
            if (IsRightIdentity(opCode, right))
            {
                RollBackTo(right.RootIndex);
                Pop();
                return true;
            }

            if (!IsLeftIdentity(opCode, left))
                return false;

            var rightStartIndex = right.RootIndex - right.Length + 1;
            MoveInstructionSpan(rightStartIndex, right.Length, left.RootIndex);
            Pop();
            Pop();
            Push(right with { RootIndex = left.RootIndex + right.Length - 1 });
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
            if (sourceStart < 0 || length < 0 || sourceStart > instructions.Count - length)
                throw new InvalidOperationException($"Cannot move instruction span [{sourceStart}, {sourceStart + length}) from a program containing {instructions.Count} instructions.");
            if (targetStart < 0 || targetStart > sourceStart)
                throw new InvalidOperationException($"Cannot move an instruction span from {sourceStart} to {targetStart}.");

            for (var i = 0; i < length; i++)
                instructions[targetStart + i] = instructions[sourceStart + i];

            RollBackTo(targetStart + length);
        }

        private void RollBackTo(int count) => instructions.RemoveRange(count, instructions.Count - count);

        private void Push(CompileNode node)
        {
            if (stackCount == compileStack.Length)
                Array.Resize(ref compileStack, compileStack.Length * 2);

            compileStack[stackCount++] = node;
        }

        private CompileNode Pop() => compileStack[--stackCount];
        private CompileNode Peek() => compileStack[stackCount - 1];

        private readonly record struct CompileNode(CompileNodeKind Kind, int RootIndex, int Length, double ConstantValue = 0.0);

        private enum CompileNodeKind
        {
            Constant,
            Variable,
            Complex
        }
    }
}
