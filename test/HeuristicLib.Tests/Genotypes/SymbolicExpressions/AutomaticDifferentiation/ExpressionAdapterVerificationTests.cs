using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;
using HEAL.HeuristicLib.Numerics;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions.AutomaticDifferentiation;

public sealed class ExpressionAdapterVerificationTests
{
    [Theory]
    [InlineData(Operation.Add)]
    [InlineData(Operation.Subtract)]
    [InlineData(Operation.Multiply)]
    [InlineData(Operation.Divide)]
    [InlineData(Operation.Negate)]
    [InlineData(Operation.Exp)]
    [InlineData(Operation.Log)]
    [InlineData(Operation.Sqrt)]
    [InlineData(Operation.Abs)]
    [InlineData(Operation.Square)]
    [InlineData(Operation.Cube)]
    [InlineData(Operation.CubeRoot)]
    [InlineData(Operation.Power)]
    [InlineData(Operation.Root)]
    [InlineData(Operation.AnalyticQuotient)]
    [InlineData(Operation.Sin)]
    [InlineData(Operation.Cos)]
    [InlineData(Operation.Tan)]
    [InlineData(Operation.Tanh)]
    public void SupportedOperationMatchesExpressionEvaluationAndFiniteDifferences(Operation operation)
    {
        VerifyEvaluationAndJacobian(CreateSupportedExpression(operation));
    }

    // Every operation an expression can contain has a differentiation rule, so the compiler's unsupported-operation
    // failure is currently unreachable through built-in symbols and only a custom symbol emitting an opcode without a
    // rule can produce it. This fails as soon as an opcode is added without a matching rule, which is the point.
    [Fact]
    public void EveryBuiltInOperationIsDifferentiable()
    {
        // Terminals are excluded by their arity rather than by name, so adding one does not silently enrol it here.
        var operations = Enum.GetValues<Operation>()
            .Where(operation => operation is not Operation.Invalid && !OperationCatalog.GetInfo(operation).IsTerminal)
            .ToArray();

        operations.ShouldNotBeEmpty();
        foreach (var operation in operations)
        {
            DifferentiableExpressionCompiler
                .TryCompile(CreateSupportedExpression(operation), out _, out var failure)
                .ShouldBeTrue($"{operation} has no differentiation rule.");
            failure.ShouldBeNull();
        }
    }

    /// <remarks>
    /// A parameter-only operand carries no input, so it reaches an adjoint rule as a single value rather than a span.
    /// Both sides of a binary operation can take either shape independently, and the rules that build intermediates
    /// have to broadcast correctly in all four combinations rather than only the one a fixed test expression happens
    /// to produce.
    /// </remarks>
    [Theory]
    [InlineData(Operation.Power, false, false)]
    [InlineData(Operation.Power, false, true)]
    [InlineData(Operation.Power, true, false)]
    [InlineData(Operation.Power, true, true)]
    [InlineData(Operation.Root, false, false)]
    [InlineData(Operation.Root, false, true)]
    [InlineData(Operation.Root, true, false)]
    [InlineData(Operation.Root, true, true)]
    [InlineData(Operation.AnalyticQuotient, false, false)]
    [InlineData(Operation.AnalyticQuotient, false, true)]
    [InlineData(Operation.AnalyticQuotient, true, false)]
    [InlineData(Operation.AnalyticQuotient, true, true)]
    public void BinaryOperationMatchesFiniteDifferencesForEveryOperandShape(Operation operation, bool leftIsParameterOnly, bool rightIsParameterOnly)
    {
        var x = Variable("x");
        var left = leftIsParameterOnly ? Constant(1.5) : Constant(0.2) * x + FixedConstant(0.8);
        var right = rightIsParameterOnly ? Constant(1.25) : Constant(-0.15) * x + FixedConstant(1.5);
        var expression = operation switch
        {
            Operation.Power => Power(left, right).Build(),
            Operation.Root => Root(left, right).Build(),
            Operation.AnalyticQuotient => AnalyticQuotient(left, right).Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        VerifyEvaluationAndJacobian(expression);
    }

    [Fact]
    public void MacroMatchesExpressionEvaluationAndFiniteDifferences()
    {
        var expression = Sigmoid(Constant(0.2) * Variable("x") + FixedConstant(0.8)).Build();

        VerifyEvaluationAndJacobian(expression);
    }

    [Theory]
    [InlineData(Operation.Divide)]
    [InlineData(Operation.Log)]
    public void NonFiniteResultsMatchExpressionEvaluation(Operation operation)
    {
        double[] x = [0.0, -1.0, 1.0];
        var dataFrame = new DataFrame([Series<double>.FromOwnedArray("x", x)]);
        var expression = operation switch
        {
            Operation.Divide => (FixedConstant(1.0) / Variable("x")).Build(),
            Operation.Log => Log(Variable("x")).Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        var differentiableExpression = CompileSuccessfully(expression);
        differentiableExpression.TryCreateExecution(dataFrame, out var execution, out var bindingFailure).ShouldBeTrue();
        execution.ShouldNotBeNull();
        bindingFailure.ShouldBeNull();
        var actual = new double[dataFrame.RowCount];

        using (execution)
            execution.Evaluate([], actual);

        var expected = ExpressionInterpreter.Interpret(expression.Compile(optimize: false), dataFrame);
        for (var rowIndex = 0; rowIndex < dataFrame.RowCount; rowIndex++)
        {
            if (double.IsNaN(expected[rowIndex]))
                double.IsNaN(actual[rowIndex]).ShouldBeTrue();
            else
                actual[rowIndex].ShouldBe(expected[rowIndex]);
        }
    }

    private static void VerifyEvaluationAndJacobian(ExpressionTree expression)
    {
        double[] x = [-1.0, -0.25, 0.5, 1.0];
        var dataFrame = new DataFrame([Series<double>.FromOwnedArray("x", x)]);
        var differentiableExpression = CompileSuccessfully(expression);
        differentiableExpression.TryCreateExecution(dataFrame, out var execution, out var bindingFailure).ShouldBeTrue();
        execution.ShouldNotBeNull();
        bindingFailure.ShouldBeNull();
        var parameters = differentiableExpression.CreateInitialParameterValues();
        var outputs = new double[dataFrame.RowCount];
        var jacobian = new double[parameters.Length * dataFrame.RowCount];

        using (execution)
            execution.EvaluateWithJacobian(parameters, outputs, jacobian);

        var expectedOutputs = ExpressionInterpreter.Interpret(expression.Compile(optimize: false), dataFrame);
        AssertClose(outputs, expectedOutputs);
        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
        {
            var step = 1e-6 * Math.Max(1.0, Math.Abs(parameters[parameterIndex]));
            var lowerParameters = (double[])parameters.Clone();
            var upperParameters = (double[])parameters.Clone();
            lowerParameters[parameterIndex] -= step;
            upperParameters[parameterIndex] += step;
            var lowerOutputs = ExpressionInterpreter.Interpret(differentiableExpression.WithParameterValues(lowerParameters).Compile(optimize: false), dataFrame);
            var upperOutputs = ExpressionInterpreter.Interpret(differentiableExpression.WithParameterValues(upperParameters).Compile(optimize: false), dataFrame);
            for (var rowIndex = 0; rowIndex < dataFrame.RowCount; rowIndex++)
            {
                var expectedDerivative = (upperOutputs[rowIndex] - lowerOutputs[rowIndex]) / (2.0 * step);
                AssertClose(jacobian[parameterIndex * dataFrame.RowCount + rowIndex], expectedDerivative);
            }
        }

        differentiableExpression.CreateInitialParameterValues().ShouldBe(parameters);
    }

    private static ExpressionTree CreateSupportedExpression(Operation operation)
    {
        var x = Variable("x");
        var left = Constant(0.2) * x + FixedConstant(0.8);
        var right = Constant(-0.15) * x + FixedConstant(1.5);
        return operation switch
        {
            Operation.Add => (left + right).Build(),
            Operation.Subtract => (left - right).Build(),
            Operation.Multiply => (left * right).Build(),
            Operation.Divide => (left / right).Build(),
            Operation.Negate => Negate(left).Build(),
            Operation.Exp => Exp(left).Build(),
            Operation.Log => Log(left).Build(),
            Operation.Sqrt => Sqrt(left).Build(),
            Operation.Abs => Abs(left).Build(),
            Operation.Square => Square(left).Build(),
            Operation.Cube => Cube(left).Build(),
            Operation.CubeRoot => CubeRoot(left).Build(),
            Operation.Power => Power(left, FixedConstant(2.0)).Build(),
            Operation.Root => Root(left, FixedConstant(2.0)).Build(),
            Operation.AnalyticQuotient => AnalyticQuotient(left, right).Build(),
            Operation.Sin => Sin(left).Build(),
            Operation.Cos => Cos(left).Build(),
            Operation.Tan => Tan(left).Build(),
            Operation.Tanh => Tanh(left).Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static DifferentiableExpression CompileSuccessfully(ExpressionTree expression)
    {
        DifferentiableExpressionCompiler.TryCompile(expression, out var differentiableExpression, out var failure).ShouldBeTrue();
        failure.ShouldBeNull();
        return differentiableExpression;
    }

    private static void AssertClose(ReadOnlySpan<double> actual, ReadOnlySpan<double> expected)
    {
        actual.Length.ShouldBe(expected.Length);
        for (var index = 0; index < actual.Length; index++)
            AssertClose(actual[index], expected[index]);
    }

    private static void AssertClose(double actual, double expected)
    {
        var tolerance = Math.Max(1e-8, Math.Abs(expected) * 1e-6);
        actual.ShouldBe(expected, tolerance);
    }
}
