using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions.AutomaticDifferentiation;

public sealed class ExpressionAdapterVerificationTests
{
    [Theory]
    [InlineData(OpCode.Add)]
    [InlineData(OpCode.Subtract)]
    [InlineData(OpCode.Multiply)]
    [InlineData(OpCode.Divide)]
    [InlineData(OpCode.Negate)]
    [InlineData(OpCode.Exp)]
    [InlineData(OpCode.Log)]
    [InlineData(OpCode.Sin)]
    [InlineData(OpCode.Cos)]
    [InlineData(OpCode.Tan)]
    [InlineData(OpCode.Tanh)]
    public void SupportedOperationMatchesExpressionEvaluationAndFiniteDifferences(OpCode operation)
    {
        VerifyEvaluationAndJacobian(CreateSupportedExpression(operation));
    }

    [Fact]
    public void MacroMatchesExpressionEvaluationAndFiniteDifferences()
    {
        var expression = Sigmoid(Add(Multiply(Constant(0.2), Variable("x")), FixedConstant(0.8))).Build();

        VerifyEvaluationAndJacobian(expression);
    }

    [Theory]
    [InlineData(OpCode.Sqrt)]
    [InlineData(OpCode.Abs)]
    [InlineData(OpCode.Square)]
    [InlineData(OpCode.Cube)]
    [InlineData(OpCode.CubeRoot)]
    [InlineData(OpCode.Power)]
    [InlineData(OpCode.Root)]
    [InlineData(OpCode.AnalyticQuotient)]
    public void UnsupportedBuiltInOperationProducesACompilationFailure(OpCode operation)
    {
        var expression = CreateUnsupportedExpression(operation);

        var success = DifferentiableExpressionCompiler.TryCompile(expression, out var differentiableExpression, out var failure);

        success.ShouldBeFalse();
        differentiableExpression.ShouldBeNull();
        failure.ShouldNotBeNull();
        failure.Point.Node.ShouldBeSameAs(expression.Root);
        failure.Symbol.ShouldBeSameAs(expression.Root.Symbol);
        failure.UnsupportedOperation.ShouldBe(operation);
    }

    [Theory]
    [InlineData(OpCode.Divide)]
    [InlineData(OpCode.Log)]
    public void NonFiniteResultsMatchExpressionEvaluation(OpCode operation)
    {
        double[] x = [0.0, -1.0, 1.0];
        var dataFrame = new DataFrame([Series<double>.FromOwnedArray("x", x)]);
        var expression = operation switch
        {
            OpCode.Divide => Divide(FixedConstant(1.0), Variable("x")).Build(),
            OpCode.Log => Log(Variable("x")).Build(),
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

    private static ExpressionTree CreateSupportedExpression(OpCode operation)
    {
        var x = Variable("x");
        var left = Add(Multiply(Constant(0.2), x), FixedConstant(0.8));
        var right = Add(Multiply(Constant(-0.15), x), FixedConstant(1.5));
        return operation switch
        {
            OpCode.Add => Add(left, right).Build(),
            OpCode.Subtract => Subtract(left, right).Build(),
            OpCode.Multiply => Multiply(left, right).Build(),
            OpCode.Divide => Divide(left, right).Build(),
            OpCode.Negate => Negate(left).Build(),
            OpCode.Exp => Exp(left).Build(),
            OpCode.Log => Log(left).Build(),
            OpCode.Sin => Sin(left).Build(),
            OpCode.Cos => Cos(left).Build(),
            OpCode.Tan => Tan(left).Build(),
            OpCode.Tanh => Tanh(left).Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
    }

    private static ExpressionTree CreateUnsupportedExpression(OpCode operation)
    {
        var x = Variable("x");
        return operation switch
        {
            OpCode.Sqrt => Sqrt(x).Build(),
            OpCode.Abs => Abs(x).Build(),
            OpCode.Square => Square(x).Build(),
            OpCode.Cube => Cube(x).Build(),
            OpCode.CubeRoot => CubeRoot(x).Build(),
            OpCode.Power => Power(x, FixedConstant(2.0)).Build(),
            OpCode.Root => Root(x, FixedConstant(2.0)).Build(),
            OpCode.AnalyticQuotient => AnalyticQuotient(x, FixedConstant(2.0)).Build(),
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
