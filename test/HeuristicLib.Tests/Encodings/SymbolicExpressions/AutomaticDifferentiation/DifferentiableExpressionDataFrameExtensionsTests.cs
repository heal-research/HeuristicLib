using static HEAL.HeuristicLib.Encodings.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.Genotypes.SymbolicExpressions.AutomaticDifferentiation;

public sealed class DifferentiableExpressionDataFrameExtensionsTests
{
    [Fact]
    public void TryCreateExecutionBindsDoubleSeriesInAdInputOrder()
    {
        var expression = (Variable("x") - Variable("y")).Build();
        var differentiableExpression = CompileSuccessfully(expression);
        var dataFrame = new DataFrame([
            Series<double>.FromOwnedArray("y", [1.0, 2.0]),
            Series<double>.FromOwnedArray("x", [4.0, 8.0])
        ]);

        var success = differentiableExpression.TryCreateExecution(dataFrame, out var execution, out var failure);

        success.ShouldBeTrue();
        execution.ShouldNotBeNull();
        failure.ShouldBeNull();
        using (execution)
        {
            var outputs = new double[dataFrame.RowCount];
            execution.Evaluate([], outputs);
            outputs.ShouldBe([3.0, 6.0]);
        }
    }

    [Fact]
    public void TryCreateExecutionReportsTheFirstMissingVariable()
    {
        var differentiableExpression = CompileSuccessfully((Variable("x") + Variable("y")).Build());
        var dataFrame = new DataFrame([Series<double>.FromOwnedArray("y", [1.0])]);

        var success = differentiableExpression.TryCreateExecution(dataFrame, out var execution, out var failure);

        success.ShouldBeFalse();
        execution.ShouldBeNull();
        failure.ShouldNotBeNull();
        failure.VariableName.ShouldBe("x");
        failure.Reason.ShouldBe(VariableBindingFailureReason.Missing);
    }

    [Fact]
    public void TryCreateExecutionReportsAnIncompatibleVariableType()
    {
        var differentiableExpression = CompileSuccessfully(Variable("x").Build());
        var dataFrame = new DataFrame([Series<int>.FromOwnedArray("x", [1])]);

        var success = differentiableExpression.TryCreateExecution(dataFrame, out var execution, out var failure);

        success.ShouldBeFalse();
        execution.ShouldBeNull();
        failure.ShouldNotBeNull();
        failure.VariableName.ShouldBe("x");
        failure.Reason.ShouldBe(VariableBindingFailureReason.IncompatibleType);
    }

    [Fact]
    public void TryCreateExecutionRepeatsAnInputFreeExpressionForEveryDataFrameRow()
    {
        var differentiableExpression = CompileSuccessfully(Constant(2.0).Build());
        var dataFrame = new DataFrame([Series<double>.FromOwnedArray("unused", [1.0, 2.0])]);

        differentiableExpression.TryCreateExecution(dataFrame, out var execution).ShouldBeTrue();

        using (execution)
        {
            var outputs = new double[dataFrame.RowCount];
            var jacobian = new double[dataFrame.RowCount];
            execution.EvaluateWithJacobian([3.0], outputs, jacobian);
            outputs.ShouldBe([3.0, 3.0]);
            jacobian.ShouldBe([1.0, 1.0]);
        }
    }

    private static DifferentiableExpression CompileSuccessfully(ExpressionTree expression)
    {
        DifferentiableExpressionCompiler.TryCompile(expression, out var differentiableExpression, out var failure).ShouldBeTrue();
        failure.ShouldBeNull();
        return differentiableExpression;
    }
}
