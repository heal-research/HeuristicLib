using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.PythonInterop;

namespace HEAL.HeuristicLib.Tests.Scenarios.PythonInterop;

public class ExtendedSymbolicRegressionProblemTest
{
    [Fact]
    public void RunMagicProblem()
    {
        var file = Path.Combine("TestData", "192_vineyard.tsv");

        //take the original r2 and add 4 dummy objectives that we will ignore in this test, but could be used for other things in a real scenario
        Func<SymbolicExpressionTree, ObjectiveVector, double[]> individualCallback = (_, o) => [o[0], 0, 0, 0, 0];
        Func<SymbolicExpressionTree[], ObjectiveVector[], double[][]> populationCallback = (_, os) => os.Select(o => new[] { o[0], 0, 0, 0, 0 }).ToArray();

        var pop = ExtendedSymbolicRegressionProblem.RunDefault(file, 40, individualCallback, populationCallback);
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
}
