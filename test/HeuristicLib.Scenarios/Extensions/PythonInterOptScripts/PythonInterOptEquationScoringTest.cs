using HEAL.HeuristicLib.PythonInterOptScripts;

namespace HEAL.HeuristicLib.Scenarios.Extensions.PythonInterOptScripts;

public class PythonInterOptEquationScoringTest
{
    /// <summary>
    ///  takes about 25 seconds on my machine
    /// </summary>
    [Fact]
    public void RunProblem()
    {
        var file = Path.Combine("TestData", "192_vineyard.tsv");
        var p = PythonInterOptEquationScoring.DefaultConf(file, 30, (x, y) => [y[0], y[0], 0.9, 0.9, 0.9]);
        var pop = PythonInterOptEquationScoring.RunDefault(p, 42);
        pop.Solutions.Length.ShouldBe(300);
        pop.Solutions.All(solution => solution.ObjectiveVector.Count == 5).ShouldBeTrue();
        pop.Solutions.All(solution => solution.ObjectiveVector.All(double.IsFinite)).ShouldBeTrue();
        var best = pop.Solutions.OrderByDescending(x => x.ObjectiveVector[0]).First();

        (best.ObjectiveVector[0] > 0.4).ShouldBeTrue();
        //parameters are nonsense just for testing comparison values from sklearn
        // Linear Regression Pearson r^2 (train): 0.4294
        // Random Forest Pearson r^2 (train): 0.8288
    }
}
