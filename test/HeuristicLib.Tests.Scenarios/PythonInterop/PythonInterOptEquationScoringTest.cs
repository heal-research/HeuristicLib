using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.PythonInterop;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Tests.Scenarios.PythonInterop;

public class PythonInterOptEquationScoringTest
{
    /// <summary>
    ///  takes about 25 seconds on my machine
    /// </summary>
    [Fact]
    public void RunProblem()
    {
        var file = Path.Combine("TestData", "192_vineyard.tsv");
        var p = PythonInterOptEquationScoring.DefaultConf(
            file,
            30,
            (x, y) => [y[0], y[0], 0.9, 0.9, 0.9]);
        p.SearchSpace.MaximumLength.ShouldBe(40);
        p.SearchSpace.MaximumDepth.ShouldBe(20);
        p.Objective.Directions.ShouldBe(Enumerable.Repeat(ObjectiveDirection.Maximize, 5));
        p.SearchSpace.Symbols.OfType<OperationSymbol>().ShouldBe(
        [
            Symbols.Addition,
            Symbols.Subtraction,
            Symbols.Multiplication,
            Symbols.Division,
            Symbols.SquareRoot,
            Symbols.Logarithm
        ]);
        var constant = p.SearchSpace.Symbols.OfType<EvolvableConstantSymbol>().Single();
        constant.InitialDistribution.ShouldBe(new UniformDoubleDistribution(-20.0, 20.0));
        constant.Perturbation.ShouldBe(new ChooseNumericPerturbation(
        [
            (new AdditiveNumericPerturbation(new NormalDoubleDistribution(0.0, 1.0)), 0.5),
            (new MultiplicativeNumericPerturbation(new NormalDoubleDistribution(0.0, 0.03)), 0.5)
        ]));
        p.InnerProblem.UseLinearScaling.ShouldBeTrue();
        p.ParameterOptimizationIterations.ShouldBe(5);
        var pop = PythonInterOptEquationScoring.RunDefault(p);
        pop.EvaluatedCandidates.Count.ShouldBe(300);
        pop.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 5).ShouldBeTrue();
        pop.EvaluatedCandidates.All(solution => solution.ObjectiveVector.All(double.IsFinite)).ShouldBeTrue();
        var best = pop.EvaluatedCandidates.OrderByDescending(x => x.ObjectiveVector[0]).First();

        (best.ObjectiveVector[0] > 0.4).ShouldBeTrue();
        //parameters are nonsense just for testing comparison values from sklearn
        // Linear Regression Pearson r^2 (train): 0.4294
        // Random Forest Pearson r^2 (train): 0.8288
    }

    [Fact]
    public void RunDefault_FitsNumericParametersBeforePythonCallback()
    {
        var file = Path.Combine("TestData", "192_vineyard.tsv");
        var unfitted = CaptureFirstPopulation(file, parameterOptimizationIterations: 0);
        var fitted = CaptureFirstPopulation(file, parameterOptimizationIterations: 5);

        unfitted.Candidates.Select(Structure).ShouldBe(fitted.Candidates.Select(Structure));

        var changedCandidates = unfitted.Candidates.Zip(fitted.Candidates)
            .Where(pair => EvolvableConstantValues(pair.First).Count > 0)
            .Where(pair => !EvolvableConstantValues(pair.First).SequenceEqual(EvolvableConstantValues(pair.Second)))
            .ToArray();

        changedCandidates.ShouldNotBeEmpty();
        changedCandidates.Any(pair =>
            MeanSquaredError(pair.Second, fitted.Problem.InnerProblem.TrainingData) <
            MeanSquaredError(pair.First, unfitted.Problem.InnerProblem.TrainingData)).ShouldBeTrue();
    }

    private static (IReadOnlyList<ExpressionTree> Candidates, PythonInterOptEquationScoring Problem) CaptureFirstPopulation(
        string file,
        int parameterOptimizationIterations)
    {
        var captured = new List<ExpressionTree>();
        var problem = PythonInterOptEquationScoring.DefaultConf(
            file,
            30,
            (candidate, objective) =>
            {
                captured.Add(candidate);
                if (captured.Count == 300)
                    throw new OperationCanceledException();

                return [objective[0], objective[0], 0.9, 0.9, 0.9];
            },
            parameterOptimizationIterations: parameterOptimizationIterations);

        Should.Throw<OperationCanceledException>(() => PythonInterOptEquationScoring.RunDefault(problem));

        return (captured, problem);
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
