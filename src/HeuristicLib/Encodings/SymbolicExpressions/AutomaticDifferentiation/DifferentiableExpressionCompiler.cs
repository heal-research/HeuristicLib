using System.Diagnostics.CodeAnalysis;
using HEAL.HeuristicLib.Numerics;
using AD = HEAL.HeuristicLib.Numerics.AutomaticDifferentiation;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

internal static class DifferentiableExpressionCompiler
{
    internal static bool TryCompile(ExpressionTree expression, [NotNullWhen(true)] out DifferentiableExpression? differentiableExpression) =>
        TryCompile(expression, out differentiableExpression, out _);

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

        public void EmitOperation(Operation operation)
        {
            var frame = GetCurrentFrame();
            if (operation is Operation.Variable or Operation.Constant)
                throw new InvalidOperationException($"Operation {operation} must be emitted through its terminal method.");

            ref readonly var definition = ref OperationCatalog.GetInfo(operation);
            var arity = definition.Arity;
            var availableValues = valueStack.Count - frame.StackStart;
            if (availableValues < arity)
                throw new InvalidOperationException($"Symbol '{frame.Point.Node.Symbol.Name}' emitted {operation}, which requires {arity} operands, but only {availableValues} are available.");
            if (!definition.IsDifferentiable)
                failure ??= new ExpressionCompilationFailure(frame.Point, operation);
            if (failure is not null)
            {
                valueStack.RemoveRange(valueStack.Count - arity, arity);
                valueStack.Add(failurePlaceholder ??= builder.Constant(double.NaN));
                return;
            }

            switch (arity)
            {
                case 1:
                    valueStack.Add(builder.Unary(operation, Pop()));
                    break;
                case 2:
                    // The operands come off the stack in reverse.
                    var right = Pop();
                    var left = Pop();
                    valueStack.Add(builder.Binary(operation, left, right));
                    break;
                default:
                    throw new NotSupportedException($"Operation {operation} has unsupported arity {arity}.");
            }
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

        private sealed class EmissionFrame(ExpressionPoint point, int stackStart)
        {
            internal ExpressionPoint Point { get; } = point;
            internal int StackStart { get; } = stackStart;
            internal AD.Value?[] ChildValues { get; } = new AD.Value?[point.Node.Arity];
        }
    }
}
