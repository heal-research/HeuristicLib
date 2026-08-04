using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using AD = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;

internal static class DifferentiableExpressionCompiler
{
    internal static bool TryCompile(ExpressionTree expression, [NotNullWhen(true)] out DifferentiableExpression? differentiableExpression, [NotNullWhen(false)] out ExpressionCompilationFailure? failure)
    {
        return new Compilation(expression).TryRun(out differentiableExpression, out failure);
    }

    private sealed class Compilation : IExpressionEmitter
    {
        private readonly ExpressionTree expression;
        private readonly AD.Builder builder = new();
        private readonly List<AD.Value> valueStack = [];
        private readonly Dictionary<string, AD.Value> inputsByName = new(StringComparer.Ordinal);
        private readonly List<string> variableNames = [];
        private readonly List<ParameterBinding> parameterBindings = [];
        private EmissionFrame? currentFrame;
        private ExpressionCompilationFailure? failure;
        private AD.Value? failurePlaceholder;

        internal Compilation(ExpressionTree expression)
        {
            this.expression = expression;
        }

        internal bool TryRun([NotNullWhen(true)] out DifferentiableExpression? differentiableExpression, [NotNullWhen(false)] out ExpressionCompilationFailure? compilationFailure)
        {
            var root = Lower(expression.RootPoint);
            if (valueStack.Count != 1)
                throw new InvalidOperationException($"Expression lowering must produce exactly one value but produced {valueStack.Count}.");
            if (failure is not null)
            {
                differentiableExpression = null;
                compilationFailure = failure;
                return false;
            }

            differentiableExpression = new DifferentiableExpression(expression, builder.Build(root), [.. variableNames], [.. parameterBindings]);
            compilationFailure = null;
            return true;
        }

        public void EmitChild(int childIndex)
        {
            var frame = GetCurrentFrame();
            if (childIndex < 0 || childIndex >= frame.Point.Node.Arity)
                throw new ArgumentOutOfRangeException(nameof(childIndex));

            if (frame.ChildValues[childIndex] is { } childValue)
            {
                valueStack.Add(childValue);
                return;
            }

            childValue = Lower(frame.Point.Child(childIndex));
            frame.ChildValues[childIndex] = childValue;
        }

        public void EmitVariable(string name)
        {
            GetCurrentFrame();
            if (!inputsByName.TryGetValue(name, out var input))
            {
                input = builder.Input();
                inputsByName.Add(name, input);
                variableNames.Add(name);
            }

            valueStack.Add(input);
        }

        public void EmitConstant(double value)
        {
            var frame = GetCurrentFrame();
            if (frame.Point.Node is NumericConstantExpressionNode { Symbol: EvolvableConstantSymbol })
            {
                valueStack.Add(builder.Parameter());
                parameterBindings.Add(new ParameterBinding(frame.Point));
                return;
            }

            valueStack.Add(builder.Constant(value));
        }

        public void EmitOperator(OpCode opCode)
        {
            var frame = GetCurrentFrame();
            if (opCode is OpCode.Variable or OpCode.Constant)
                throw new InvalidOperationException($"Opcode {opCode} must be emitted through its terminal method.");

            var arity = OpCodes.GetArity(opCode);
            var availableValues = valueStack.Count - frame.StackStart;
            if (availableValues < arity)
                throw new InvalidOperationException($"Symbol '{frame.Point.Node.Symbol.Name}' emitted {opCode}, which requires {arity} operands, but only {availableValues} are available.");
            if (!IsSupported(opCode))
                failure ??= new ExpressionCompilationFailure(frame.Point, opCode);
            if (failure is not null)
            {
                valueStack.RemoveRange(valueStack.Count - arity, arity);
                valueStack.Add(failurePlaceholder ??= builder.Constant(double.NaN));
                return;
            }

            AD.Value result;
            if (arity == 1)
            {
                var operand = Pop();
                result = opCode switch
                {
                    OpCode.Negate => builder.Negate(operand),
                    OpCode.Exp => builder.Exp(operand),
                    OpCode.Log => builder.Log(operand),
                    OpCode.Sin => builder.Sin(operand),
                    OpCode.Cos => builder.Cos(operand),
                    OpCode.Tan => builder.Tan(operand),
                    OpCode.Tanh => builder.Tanh(operand),
                    _ => throw new InvalidOperationException($"Opcode {opCode} is not a supported unary operation.")
                };
            }
            else
            {
                var right = Pop();
                var left = Pop();
                result = opCode switch
                {
                    OpCode.Add => builder.Add(left, right),
                    OpCode.Subtract => builder.Subtract(left, right),
                    OpCode.Multiply => builder.Multiply(left, right),
                    OpCode.Divide => builder.Divide(left, right),
                    _ => throw new InvalidOperationException($"Opcode {opCode} is not a supported binary operation.")
                };
            }

            valueStack.Add(result);
        }

        private AD.Value Lower(ExpressionPoint point)
        {
            var previousFrame = currentFrame;
            var frame = new EmissionFrame(point, valueStack.Count);
            currentFrame = frame;
            try
            {
                point.Node.Symbol.Emit(point.Node, this);
            }
            finally
            {
                currentFrame = previousFrame;
            }

            var emittedValueCount = valueStack.Count - frame.StackStart;
            if (emittedValueCount != 1)
                throw new InvalidOperationException($"Symbol '{point.Node.Symbol.Name}' must emit exactly one value but emitted {emittedValueCount}.");

            return valueStack[^1];
        }

        private EmissionFrame GetCurrentFrame()
        {
            if (currentFrame is null)
                throw new InvalidOperationException("Expression emission is only available while lowering a symbol.");

            return currentFrame;
        }

        private AD.Value Pop()
        {
            var index = valueStack.Count - 1;
            var value = valueStack[index];
            valueStack.RemoveAt(index);
            return value;
        }

        private static bool IsSupported(OpCode opCode) => opCode is
            OpCode.Add or OpCode.Subtract or OpCode.Multiply or OpCode.Divide or
            OpCode.Negate or OpCode.Exp or OpCode.Log or OpCode.Sin or OpCode.Cos or OpCode.Tan or OpCode.Tanh;

        private sealed class EmissionFrame(ExpressionPoint point, int stackStart)
        {
            internal ExpressionPoint Point { get; } = point;
            internal int StackStart { get; } = stackStart;
            internal AD.Value?[] ChildValues { get; } = new AD.Value?[point.Node.Arity];
        }
    }
}
