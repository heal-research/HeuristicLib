using HEAL.HeuristicLib.PythonInterop;

namespace HEAL.HeuristicLib.Tests.Scenarios.GenealogyAnalysis;

public sealed class BestSoFarInteropScenarios
{
    [Fact]
    public void TestFunctionResultIncludesTheBestSoFarCurve()
    {
        var result = PythonGenealogyAnalysis.RunTestFunctionConfigurable("", new TestFunctionExperimentParameters
        {
            AlgorithmName = "ga",
            Problem = 1,
            Dimension = 2,
            Iterations = 3,
            PopulationSize = 8,
            Elites = 0,
            Seed = 42,
            TrackPopulations = true
        });
        var expected = new List<(long Evaluations, double Value)>();
        long evaluated = 0;
        var best = double.PositiveInfinity;
        for (var i = 0; i < result.AllPopulations.Count; i++)
        {
            // With no elites, each generation evaluates exactly the candidates in its returned population.
            result.AllPopulations[i].Count.ShouldBe(8);
            evaluated += result.AllPopulations[i].Count;
            var value = result.AllPopulations[i].Min(candidate => candidate.ObjectiveVector[0]);
            if (value < best)
            {
                best = value;
                expected.Add((evaluated, value));
            }
        }
        evaluated.ShouldBe(24);
        result.QualityCurve.Select(point => (point.Evaluations, point.Value[0])).ShouldBe(expected);
        result.QualityCurve[0].Evaluations.ShouldBe(8);
        result.QualityCurve[^1].Value.ShouldBe(result.BestMedianWorst[^1].Best.ObjectiveVector);
        result.AllPopulations.Count.ShouldBe(3);
    }
}
