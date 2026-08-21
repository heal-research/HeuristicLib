using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.PythonInterop;

namespace HEAL.HeuristicLib.Tests.Scenarios.PythonInterop;

public class ExtendedSymbolicRegressionProblemTest
{
    [Fact]
    public void RunMagicProblem()
    {
        var file = Path.Combine("TestData", "192_vineyard.tsv");

        //take the original r2 and add 4 dummy objectives that we will ignore in this test, but could be used for other things in a real scenario
        Func<ExpressionTree, ObjectiveVector, double[]>? individualCallback = null;
        Func<ExpressionTree[], ObjectiveVector[], double[][]> populationCallback = (ts, os) => os.Select(o => new double[] { o[0], 0, 0, 0, 0 }).ToArray();

        var pop = ExtendedSymbolicRegressionProblem.RunDefault(
            file,
            40,
            individualCallback,
            populationCallback);
        pop.EvaluatedCandidates.Count.ShouldBe(300);
        pop.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 5).ShouldBeTrue();
        pop.EvaluatedCandidates.All(solution => solution.ObjectiveVector.All(double.IsFinite)).ShouldBeTrue();
        var best = pop.EvaluatedCandidates.OrderByDescending(x => x.ObjectiveVector[0]).First();

        (best.ObjectiveVector[0] > 0.4).ShouldBeTrue();

        //parameters are nonsense,
        //but just for comparison, here values from sklearn:
        // Linear Regression Pearson r^2 (train): 0.4294
        // Random Forest Pearson r^2 (train): 0.8288
    }

    [Fact]
    public void RunDefault_FitsNumericParametersBeforePythonCallback()
    {
        var file = Path.Combine("TestData", "192_vineyard.tsv");
        var unfitted = CaptureFirstPopulation(file, parameterOptimizationIterations: 0);
        var fitted = CaptureFirstPopulation(file, parameterOptimizationIterations: 5);

        unfitted.Select(Structure).ShouldBe(fitted.Select(Structure));

        var changedCandidates = unfitted.Zip(fitted)
            .Where(pair => EvolvableConstantValues(pair.First).Count > 0)
            .Where(pair => !EvolvableConstantValues(pair.First).SequenceEqual(EvolvableConstantValues(pair.Second)))
            .ToArray();

        changedCandidates.ShouldNotBeEmpty();

        var fittingProblem = PythonInterOptEquationScoring.DefaultConf(
            file,
            40,
            (_, objective) => objective.ToArray(),
            parameterOptimizationIterations: 0).InnerProblem;

        changedCandidates.Any(pair =>
            MeanSquaredError(pair.Second, fittingProblem.TrainingData) <
            MeanSquaredError(pair.First, fittingProblem.TrainingData)).ShouldBeTrue();
    }

    private static ExpressionTree[] CaptureFirstPopulation(string file, int parameterOptimizationIterations)
    {
        ExpressionTree[]? population = null;

        Should.Throw<OperationCanceledException>(() => ExtendedSymbolicRegressionProblem.RunDefault(
            file,
            40,
            individualPythonCallback: null,
            populationwidePythonCallback: (trees, _) =>
            {
                population = trees;
                throw new OperationCanceledException();
            },
            parameterOptimizationIterations: parameterOptimizationIterations));

        return population.ShouldNotBeNull();
    }

    private static string Structure(ExpressionTree tree) =>
        string.Join(",", tree.TraversePreOrder().Select(node => node switch
        {
            NumericConstantExpressionNode => $"{node.Symbol.Name}:constant",
            VariableExpressionNode variable => $"{node.Symbol.Name}:{variable.VariableName}",
            _ => node.Symbol.Name
        }));

    private static IReadOnlyList<double> EvolvableConstantValues(ExpressionTree tree) =>
        tree.TraversePreOrder()
            .OfType<NumericConstantExpressionNode>()
            .Where(node => node.Symbol is EvolvableConstantSymbol)
            .Select(node => node.Value)
            .ToArray();

    private static double MeanSquaredError(ExpressionTree tree, RegressionData data)
    {
        var predictions = tree.Evaluate(data.Inputs);
        var targets = data.Target.Values.Span;
        var squaredError = 0.0;
        for (var i = 0; i < predictions.Length; i++)
        {
            var error = predictions[i] - targets[i];
            squaredError += error * error;
        }

        return squaredError / predictions.Length;
    }
}
