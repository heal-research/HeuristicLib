using HEAL.HeuristicLib.DataAnalysis;
using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions.AutomaticDifferentiation;
using HEAL.HeuristicLib.Numerics;
using HEAL.HeuristicLib.Numerics.Optimization;
using static HEAL.HeuristicLib.Genotypes.SymbolicExpressions.ExpressionDraft;

namespace HEAL.HeuristicLib.Tests.DataAnalysis.Regression;

public sealed class NumericParameterFitterTests
{
    [Fact]
    public void TryFitAdjustsEvolvableConstantsAndRebuildsTheExpression()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        double[] targets = [-1.0, 1.0, 3.0, 5.0, 7.0];
        var data = new RegressionData(new DataFrame([Series<double>.FromOwnedArray("x", x)]), Series<double>.FromOwnedArray("y", targets));
        var expression = (Constant(0.25) * Variable("x") + Constant(-0.5)).Build();

        var success = NumericParameterFitter.TryFit(expression, data, 100, out var fittedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        fittedExpression.ShouldNotBeNull();
        failure.ShouldBeNull();
        fittedExpression.ShouldNotBeSameAs(expression);
        var predictions = ExpressionInterpreter.Interpret(fittedExpression.Compile(optimize: false), data.Inputs);
        predictions.ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void TryFitAdjustsAnInputFreeConstantToEveryTargetRow()
    {
        double[] targets = [1.0, 2.0, 3.0, 4.0, 5.0];
        var data = new RegressionData(new DataFrame([new Series<double>("unused", new double[targets.Length])]), new Series<double>("y", targets));
        var expression = Constant(-1.0).Build();

        var success = NumericParameterFitter.TryFit(expression, data, 100, out var fittedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        fittedExpression.ShouldNotBeNull();
        failure.ShouldBeNull();
        ExpressionInterpreter.Interpret(fittedExpression.Compile(optimize: false), data.Inputs).ShouldBe([3.0, 3.0, 3.0, 3.0, 3.0], tolerance: 1e-8);
    }

    [Fact]
    public void TryFitLeavesFixedConstantsUnchanged()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var targets = x.Select(value => 10.0 + (3.0 * value)).ToArray();
        var data = new RegressionData(new DataFrame([new Series<double>("x", x)]), new Series<double>("y", targets));
        var expression = (FixedConstant(10.0) + Constant(0.5) * Variable("x")).Build();
        var fixedConstant = expression.RootPoint.Child(0).Node;

        NumericParameterFitter.TryFit(expression, data, 100, out var fittedExpression, TestContext.Current.CancellationToken).ShouldBeTrue();

        fittedExpression.ShouldNotBeNull();
        fittedExpression.RootPoint.Child(0).Node.ShouldBeSameAs(fixedConstant);
        fittedExpression.RootPoint.Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(10.0);
        ExpressionInterpreter.Interpret(fittedExpression.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void RepeatedFittingDoesNotModifyTheSourceExpression()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var targets = x.Select(value => 2.0 * value).ToArray();
        var data = new RegressionData(new DataFrame([new Series<double>("x", x)]), new Series<double>("y", targets));
        var expression = (Constant(0.5) * Variable("x")).Build();

        NumericParameterFitter.TryFit(expression, data, 100, out var first, TestContext.Current.CancellationToken).ShouldBeTrue();
        NumericParameterFitter.TryFit(expression, data, 100, out var second, TestContext.Current.CancellationToken).ShouldBeTrue();

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        expression.RootPoint.Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(0.5);
        ExpressionInterpreter.Interpret(first.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
        ExpressionInterpreter.Interpret(second.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void TryFitPreservesASuccessfulNonFiniteResult()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("unused", [1.0, 2.0])]), new Series<double>("y", [0.0, 0.0]));
        var expression = (Constant(0.0) / FixedConstant(0.0)).Build();

        var success = NumericParameterFitter.TryFit(expression, data, 10, out var fittedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        fittedExpression.ShouldNotBeNull();
        failure.ShouldBeNull();
        ExpressionInterpreter.Interpret(fittedExpression.Compile(optimize: false), data.Inputs).ShouldAllBe(value => double.IsNaN(value));
    }

    [Fact]
    public void TryFitReturnsTheOriginalExpressionWhenMaximumIterationsIsZero()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = Sqrt(Variable("unbound") + Constant(1.0)).Build();

        var success = NumericParameterFitter.TryFit(expression, data, 0, out var fittedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeTrue();
        fittedExpression.ShouldBeSameAs(expression);
        failure.ShouldBeNull();
    }

    [Fact]
    public void TryFitPreservesAMacroAroundAFittedParameter()
    {
        double[] x = [-2.0, -1.0, 0.0, 1.0, 2.0];
        var targets = x.Select(value => 1.0 / (1.0 + Math.Exp(-1.5 * value))).ToArray();
        var data = new RegressionData(new DataFrame([new Series<double>("x", x)]), new Series<double>("y", targets));
        var expression = Sigmoid(Constant(0.2) * Variable("x")).Build();

        NumericParameterFitter.TryFit(expression, data, 100, out var fittedExpression, TestContext.Current.CancellationToken).ShouldBeTrue();

        fittedExpression.ShouldNotBeNull();
        fittedExpression.Root.Symbol.ShouldBeSameAs(Symbols.Sigmoid);
        expression.RootPoint.Child(0).Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>().Value.ShouldBe(0.2);
        ExpressionInterpreter.Interpret(fittedExpression.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void TryFitTreatsReferenceSharedConstantsAtDifferentPathsAsIndependentParameters()
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

        NumericParameterFitter.TryFit(expression, data, 100, out var fittedExpression, TestContext.Current.CancellationToken).ShouldBeTrue();

        fittedExpression.ShouldNotBeNull();
        var linearConstant = fittedExpression.RootPoint.Child(0).Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>();
        var quadraticConstant = fittedExpression.RootPoint.Child(1).Child(0).Node.ShouldBeOfType<NumericConstantExpressionNode>();
        linearConstant.ShouldNotBeSameAs(quadraticConstant);
        linearConstant.Value.ShouldBe(2.0, 1e-8);
        quadraticConstant.Value.ShouldBe(3.0, 1e-8);
        sharedConstant.Value.ShouldBe(0.25);
        ExpressionInterpreter.Interpret(fittedExpression.Compile(optimize: false), data.Inputs).ShouldBe(targets, tolerance: 1e-8);
    }

    [Fact]
    public void FitReportsMissingExpressionVariable()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (Variable("missing") + Constant(1.0)).Build();

        var exception = Should.Throw<ArgumentException>(() => NumericParameterFitter.Fit(expression, data, 100, TestContext.Current.CancellationToken));

        exception.ParamName.ShouldBe("data");
        exception.Message.ShouldContain("missing");
    }

    [Fact]
    public void DetailedTryFitReportsVariableBindingFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (Variable("missing") + Constant(1.0)).Build();

        var success = NumericParameterFitter.TryFit(expression, data, 100, out var fittedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        fittedExpression.ShouldBeNull();
        var binding = failure.ShouldBeOfType<NumericParameterFittingFailure.VariableBinding>();
        binding.Failure.VariableName.ShouldBe("missing");
        binding.Failure.Reason.ShouldBe(VariableBindingFailureReason.Missing);
    }

    [Fact]
    public void ConvenienceTryFitReturnsFalseForVariableBindingFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (Variable("missing") + Constant(1.0)).Build();

        var success = NumericParameterFitter.TryFit(expression, data, 100, out var fittedExpression, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        fittedExpression.ShouldBeNull();
    }

    [Fact]
    public void FitReportsAnIncompatibleExpressionVariableType()
    {
        var data = new RegressionData(new DataFrame([new Series<int>("x", [1])]), new Series<double>("y", [1.0]));
        var expression = (Variable("x") + Constant(1.0)).Build();

        var exception = Should.Throw<ArgumentException>(() => NumericParameterFitter.Fit(expression, data, 100, TestContext.Current.CancellationToken));

        exception.ParamName.ShouldBe("data");
        exception.Message.ShouldContain("not a double series");
    }

    [Fact]
    public void DetailedTryFitReportsAnIncompatibleVariableBindingFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<int>("x", [1])]), new Series<double>("y", [1.0]));
        var expression = (Variable("x") + Constant(1.0)).Build();

        var success = NumericParameterFitter.TryFit(expression, data, 100, out var fittedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        fittedExpression.ShouldBeNull();
        var binding = failure.ShouldBeOfType<NumericParameterFittingFailure.VariableBinding>();
        binding.Failure.VariableName.ShouldBe("x");
        binding.Failure.Reason.ShouldBe(VariableBindingFailureReason.IncompatibleType);
    }

    [Fact]
    public void FitReportsNumericalOptimizationFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (FixedConstant(1.0) / Constant(0.0)).Build();

        var exception = Should.Throw<InvalidOperationException>(() => NumericParameterFitter.Fit(expression, data, 10, TestContext.Current.CancellationToken));

        exception.Message.ShouldStartWith("Numeric parameter fitting failed:");
    }

    [Fact]
    public void DetailedTryFitReportsNumericalOptimizationFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (FixedConstant(1.0) / Constant(0.0)).Build();

        var success = NumericParameterFitter.TryFit(expression, data, 10, out var fittedExpression, out var failure, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        fittedExpression.ShouldBeNull();
        failure.ShouldBeOfType<NumericParameterFittingFailure.NumericalOptimization>().Failure.Message.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ConvenienceTryFitReturnsFalseForNumericalOptimizationFailure()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = (FixedConstant(1.0) / Constant(0.0)).Build();

        var success = NumericParameterFitter.TryFit(expression, data, 10, out var fittedExpression, TestContext.Current.CancellationToken);

        success.ShouldBeFalse();
        fittedExpression.ShouldBeNull();
    }

    [Fact]
    public void TryFitRejectsInvalidArgumentsBeforeAnIdentityShortcut()
    {
        var nonEmptyData = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var emptyData = new RegressionData(new DataFrame([new Series<double>("x", [])]), new Series<double>("y", []));
        var expression = FixedConstant(1.0).Build();

        Should.Throw<ArgumentOutOfRangeException>(() => NumericParameterFitter.TryFit(expression, nonEmptyData, -1, out _)).ParamName.ShouldBe("maximumIterations");
        Should.Throw<ArgumentException>(() => NumericParameterFitter.TryFit(expression, emptyData, 10, out _, out _)).ParamName.ShouldBe("data");
    }

    [Fact]
    public void TryFitPropagatesCancellationBeforeAnIdentityShortcut()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = FixedConstant(1.0).Build();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Should.Throw<OperationCanceledException>(() => NumericParameterFitter.TryFit(expression, data, 10, out _, out _, cancellation.Token));
    }

    [Fact]
    public void TryFitReturnsTheSameExpressionWhenThereAreNoEvolvableConstants()
    {
        var data = new RegressionData(new DataFrame([new Series<double>("x", [1.0])]), new Series<double>("y", [1.0]));
        var expression = Sqrt(Variable("unbound")).Build();

        NumericParameterFitter.TryFit(expression, data, 100, out var fittedExpression, TestContext.Current.CancellationToken).ShouldBeTrue();

        fittedExpression.ShouldBeSameAs(expression);
    }

    // No expression can currently produce a compilation failure, because every opcode carrying arity metadata has a
    // differentiation rule. The mapping is still reachable from a constructed failure, and stays covered for the first
    // operation that arrives without one.
    [Fact]
    public void CreateExceptionReportsTheSymbolAndOperationOfACompilationFailure()
    {
        var expression = Sqrt(Variable("x")).Build();
        var failure = new NumericParameterFittingFailure.Compilation(
            new ExpressionCompilationFailure(expression.RootPoint, Operation.Sqrt));

        var exception = NumericParameterFitter.CreateException(failure, "data").ShouldBeOfType<NotSupportedException>();

        exception.Message.ShouldContain(expression.RootPoint.Node.Symbol.Name);
        exception.Message.ShouldContain(nameof(Operation.Sqrt));
    }
}
