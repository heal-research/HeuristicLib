using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.DataAnalysis.Regression;

public sealed class ConstantOptimizerTests
{
    [Fact]
    public void TryOptimizeFitsEvolvableConstantsAndRebuildsTheExpression()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        double[] targets = [-1.0, 1.0, 3.0, 5.0, 7.0];
        var data = new RegressionData(new DataFrame([Series<double>.FromOwnedArray("x", x)]), Series<double>.FromOwnedArray("y", targets));
        var expression = (Constant(0.25) * Variable("x") + Constant(-0.5)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        optimizedExpression.ShouldNotBeNull();
        failure.ShouldBeNull();
        optimizedExpression.ShouldNotBeSameAs(expression);
        var predictions = ExpressionInterpreter.Interpret(optimizedExpression.Compile(optimize: false), data.Inputs);
        predictions.ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void TryOptimizeFitsAnInputFreeConstantToEveryTargetRow()
    {
        double[] targets = [1.0, 2.0, 3.0, 4.0, 5.0];
        var data = new RegressionData(new DataFrame([new Series<double>("unused", new double[targets.Length])]), new Series<double>("y", targets));
        var expression = Constant(-1.0).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        optimizedExpression.ShouldNotBeNull();
        failure.ShouldBeNull();
        ExpressionInterpreter.Interpret(optimizedExpression.Compile(optimize: false), data.Inputs).ShouldBe([3.0, 3.0, 3.0, 3.0, 3.0], tolerance: 1e-8);
    }

    [Fact]
    public void TryOptimizeLeavesFixedConstantsUnchanged()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var targets = x.Select(value => 10.0 + (3.0 * value)).ToArray();
        var data = new RegressionData(new DataFrame([new Series<double>("x", x)]), new Series<double>("y", targets));
        var expression = (FixedConstant(10.0) + Constant(0.5) * Variable("x")).Build();
        var fixedConstant = expression.RootPoint.Child(0).Node;

        ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, TestContext.Current.CancellationToken).ShouldBeTrue();

        optimizedExpression.ShouldNotBeNull();
        optimizedExpression.RootPoint.Child(0).Node.ShouldBeSameAs(fixedConstant);
        optimizedExpression.RootPoint.Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(10.0);
        ExpressionInterpreter.Interpret(optimizedExpression.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void RepeatedOptimizationDoesNotModifyTheSourceExpression()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var targets = x.Select(value => 2.0 * value).ToArray();
        var data = new RegressionData(new DataFrame([new Series<double>("x", x)]), new Series<double>("y", targets));
        var expression = (Constant(0.5) * Variable("x")).Build();

        ConstantOptimizer.TryOptimize(expression, data, 100, out var first, TestContext.Current.CancellationToken).ShouldBeTrue();
        ConstantOptimizer.TryOptimize(expression, data, 100, out var second, TestContext.Current.CancellationToken).ShouldBeTrue();

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        expression.RootPoint.Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(0.5);
        ExpressionInterpreter.Interpret(first.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
        ExpressionInterpreter.Interpret(second.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void TryOptimizePreservesASuccessfulNonFiniteResult()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("unused", [1.0, 2.0])]), new Series<double>("y", [0.0, 0.0]));
        var expression = (Constant(0.0) / FixedConstant(0.0)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 10, out var optimizedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        optimizedExpression.ShouldNotBeNull();
        failure.ShouldBeNull();
        ExpressionInterpreter.Interpret(optimizedExpression.Compile(optimize: false), data.Inputs).ShouldAllBe(value => double.IsNaN(value));
    }

    [Fact]
    public void TryOptimizeReturnsTheOriginalExpressionWhenMaximumIterationsIsZero()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = Sqrt(Variable("unbound") + Constant(1.0)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 0, out var optimizedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        optimizedExpression.ShouldBeSameAs(expression);
        failure.ShouldBeNull();
    }

    [Fact]
    public void TryOptimizePreservesAMacroAroundAnOptimizedConstant()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var targets = x.Select(value => 1.0 / (1.0 + Math.Exp(-1.5 * value))).ToArray();
        var data = new RegressionData(new DataFrame([new Series<double>("x", x)]), new Series<double>("y", targets));
        var expression = Sigmoid(Constant(0.2) * Variable("x")).Build();

        ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, TestContext.Current.CancellationToken).ShouldBeTrue();

        optimizedExpression.ShouldNotBeNull();
        optimizedExpression.Root.Symbol.ShouldBeSameAs(Symbols.Sigmoid);
        expression.RootPoint.Child(0).Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(0.2);
        ExpressionInterpreter.Interpret(optimizedExpression.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void TryOptimizeTreatsReferenceSharedConstantsAtDifferentPathsAsIndependentParameters()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var targets = x.Select(value => (2.0 * value) + (3.0 * value * value)).ToArray();
        var data = new RegressionData(new DataFrame([new Series<double>("x", x)]), new Series<double>("y", targets));
        var symbol = new EvolvableConstantSymbol();
        var sharedConstant = new NumericConstantExpressionNode(symbol, 0.25);
        var variable = Variable("x").Build().Root;
        var linearTerm = new BinaryExpressionNode(Symbols.Multiplication, sharedConstant, variable);
        var squaredVariable = new BinaryExpressionNode(Symbols.Multiplication, variable, variable);
        var quadraticTerm = new BinaryExpressionNode(Symbols.Multiplication, sharedConstant, squaredVariable);
        var expression = new ExpressionTree(new BinaryExpressionNode(Symbols.Addition, linearTerm, quadraticTerm));

        ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, TestContext.Current.CancellationToken).ShouldBeTrue();

        optimizedExpression.ShouldNotBeNull();
        var linearConstant = optimizedExpression.RootPoint.Child(0).Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>();
        var quadraticConstant = optimizedExpression.RootPoint.Child(1).Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>();
        linearConstant.ShouldNotBeSameAs(quadraticConstant);
        linearConstant.Value.ShouldBe(2.0, 1e-8);
        quadraticConstant.Value.ShouldBe(3.0, 1e-8);
        sharedConstant.Value.ShouldBe(0.25);
        ExpressionInterpreter.Interpret(optimizedExpression.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void OptimizeReportsUnsupportedExpressionOperation()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = Sqrt(Variable("x") + Constant(1.0)).Build();

        var exception = Should.Throw<NotSupportedException>(() => ConstantOptimizer.Optimize(expression, data, 100, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("Sqrt");
    }

    [Fact]
    public void DetailedTryOptimizeReportsExpressionCompilationFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = Sqrt(Variable("x") + Constant(1.0)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        optimizedExpression.ShouldBeNull();
        var compilation = failure.ShouldBeOfType<ConstantOptimizationFailure.Compilation>();
        compilation.Failure.UnsupportedOperation.ShouldBe(OpCode.Sqrt);
    }

    [Fact]
    public void ConvenienceTryOptimizeReturnsFalseForExpressionCompilationFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = Sqrt(Variable("x") + Constant(1.0)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        optimizedExpression.ShouldBeNull();
    }

    [Fact]
    public void OptimizeReportsMissingExpressionVariable()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (Variable("missing") + Constant(1.0)).Build();

        var exception = Should.Throw<ArgumentException>(() => ConstantOptimizer.Optimize(expression, data, 100, TestContext.Current.CancellationToken));

        exception.ParamName.ShouldBe("data");
        exception.Message.ShouldContain("missing");
    }

    [Fact]
    public void DetailedTryOptimizeReportsVariableBindingFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (Variable("missing") + Constant(1.0)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        optimizedExpression.ShouldBeNull();
        var binding = failure.ShouldBeOfType<ConstantOptimizationFailure.VariableBinding>();
        binding.Failure.VariableName.ShouldBe("missing");
        binding.Failure.Reason.ShouldBe(VariableBindingFailureReason.Missing);
    }

    [Fact]
    public void ConvenienceTryOptimizeReturnsFalseForVariableBindingFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (Variable("missing") + Constant(1.0)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        optimizedExpression.ShouldBeNull();
    }

    [Fact]
    public void OptimizeReportsAnIncompatibleExpressionVariableType()
    {
        var data = new RegressionData(new DataFrame([new Series<int>("x", [1])]), new Series<double>("y", [1.0]));
        var expression = (Variable("x") + Constant(1.0)).Build();

        var exception = Should.Throw<ArgumentException>(() => ConstantOptimizer.Optimize(expression, data, 100, TestContext.Current.CancellationToken));

        exception.ParamName.ShouldBe("data");
        exception.Message.ShouldContain("not a double series");
    }

    [Fact]
    public void DetailedTryOptimizeReportsAnIncompatibleVariableBindingFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<int>("x", [1])]), new Series<double>("y", [1.0]));
        var expression = (Variable("x") + Constant(1.0)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        optimizedExpression.ShouldBeNull();
        var binding = failure.ShouldBeOfType<ConstantOptimizationFailure.VariableBinding>();
        binding.Failure.VariableName.ShouldBe("x");
        binding.Failure.Reason.ShouldBe(VariableBindingFailureReason.IncompatibleType);
    }

    [Fact]
    public void OptimizeReportsNumericalOptimizationFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (FixedConstant(1.0) / Constant(0.0)).Build();

        var exception = Should.Throw<InvalidOperationException>(() => ConstantOptimizer.Optimize(expression, data, 10, TestContext.Current.CancellationToken));

        exception.Message.ShouldStartWith("Numerical constant optimization failed:");
    }

    [Fact]
    public void DetailedTryOptimizeReportsNumericalOptimizationFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (FixedConstant(1.0) / Constant(0.0)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 10, out var optimizedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        optimizedExpression.ShouldBeNull();
        failure.ShouldBeOfType<ConstantOptimizationFailure.NumericalOptimization>().Failure.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ConvenienceTryOptimizeReturnsFalseForNumericalOptimizationFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (FixedConstant(1.0) / Constant(0.0)).Build();

        var success = ConstantOptimizer.TryOptimize(expression, data, 10, out var optimizedExpression, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        optimizedExpression.ShouldBeNull();
    }

    [Fact]
    public void TryOptimizeRejectsInvalidArgumentsBeforeAnIdentityShortcut()
    {
        var nonEmptyData = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var emptyData = new RegressionData(new DataFrame([new Series<double>("x", [])]), new Series<double>("y", []));
        var expression = FixedConstant(1.0).Build();

        Should.Throw<ArgumentOutOfRangeException>(() => ConstantOptimizer.TryOptimize(expression, nonEmptyData, -1, out _)).ParamName.ShouldBe("maximumIterations");
        Should.Throw<ArgumentException>(() => ConstantOptimizer.TryOptimize(expression, emptyData, 10, out _, out _)).ParamName.ShouldBe("data");
    }

    [Fact]
    public void TryOptimizePropagatesCancellationBeforeAnIdentityShortcut()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = FixedConstant(1.0).Build();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Should.Throw<OperationCanceledException>(() => ConstantOptimizer.TryOptimize(expression, data, 10, out _, out _, cancellation.Token));
    }

    [Fact]
    public void TryOptimizeReturnsTheSameExpressionWhenThereAreNoEvolvableConstants()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = Sqrt(Variable("unbound")).Build();

        ConstantOptimizer.TryOptimize(expression, data, 100, out var optimizedExpression, TestContext.Current.CancellationToken).ShouldBeTrue();

        optimizedExpression.ShouldBeSameAs(expression);
    }
}
