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
            (x, y) => [y[0], y[0], 0.9, 0.9, 0.9],
            parameterOptimizationIterations: 0);
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
    public void DefaultConfiguration_ReportsUnavailableParameterOptimization()
    {
        Should.Throw<NotImplementedException>(() => PythonInterOptEquationScoring.DefaultConf(
            "unused.csv",
            30,
            (x, y) => [y[0], y[0], 0.9, 0.9, 0.9]));
    }
}
