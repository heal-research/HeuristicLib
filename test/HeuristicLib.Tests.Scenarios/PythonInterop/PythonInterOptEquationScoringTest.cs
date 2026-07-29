using HEAL.HeuristicLib.PythonInterop;

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
        var p = PythonInterOptEquationScoring.DefaultConf(file, 30, (_, y) => [y[0], y[0], 0.9, 0.9, 0.9]);
        var pop = PythonInterOptEquationScoring.RunDefault(p);
        pop.EvaluatedCandidates.Length.ShouldBe(300);
        pop.EvaluatedCandidates.All(solution => solution.ObjectiveVector.Count == 5).ShouldBeTrue();
        pop.EvaluatedCandidates.All(solution => solution.ObjectiveVector.All(double.IsFinite)).ShouldBeTrue();
        var best = pop.EvaluatedCandidates.OrderByDescending(x => x.ObjectiveVector[0]).First();

        (best.ObjectiveVector[0] > 0.4).ShouldBeTrue();
        //parameters are nonsense just for testing comparison values from sklearn
        // Linear Regression Pearson r^2 (train): 0.4294
        // Random Forest Pearson r^2 (train): 0.8288
    }
}
