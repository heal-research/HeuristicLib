using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Selectors;

public class ProportionalSelectorTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ANaNFitness_IsNeverSelected(bool windowing)
    {
        var population = new[] { Candidate("nan", double.NaN), Candidate("good", 1.0), Candidate("bad", 10.0) };

        var selected = ProportionalSelector.Select(
            population,
            SingleObjective.Minimize,
            count: 4,
            new SequenceRandomNumberGenerator(0.0, 0.25, 0.5, 0.999),
            windowing);

        selected.Select(candidate => candidate.Candidate).ShouldNotContain("nan");
    }

    [Fact]
    public void WhenEveryFitnessIsNaN_SelectionStillReturnsCandidates()
    {
        var population = new[] { Candidate("a", double.NaN), Candidate("b", double.NaN) };

        var selected = ProportionalSelector.Select(
            population,
            SingleObjective.Minimize,
            count: 2,
            new SequenceRandomNumberGenerator(0.0, 0.9));

        selected.Count.ShouldBe(2);
        selected.ShouldAllBe(candidate => candidate != null);
    }

    [Fact]
    public void AnInfinitelyBadFitness_IsNeverSelected()
    {
        var population = new[] { Candidate("inf", double.PositiveInfinity), Candidate("good", 1.0), Candidate("ok", 2.0) };

        var selected = ProportionalSelector.Select(
            population,
            SingleObjective.Minimize,
            count: 4,
            new SequenceRandomNumberGenerator(0.0, 0.25, 0.5, 0.999),
            windowing: false);

        selected.Select(candidate => candidate.Candidate).ShouldNotContain("inf");
    }

    [Fact]
    public void AnInfinitelyGoodFitness_TakesTheWholeWheel()
    {
        var population = new[] { Candidate("worst", 5.0), Candidate("best", double.NegativeInfinity), Candidate("ok", 2.0) };

        var selected = ProportionalSelector.Select(
            population,
            SingleObjective.Minimize,
            count: 3,
            new SequenceRandomNumberGenerator(0.0, 0.5, 0.999));

        selected.ShouldAllBe(candidate => candidate.Candidate == "best");
    }

    [Fact]
    public void WithFiniteFitnesses_TheBetterCandidateTakesTheLargerShare()
    {
        var population = new[] { Candidate("good", 1.0), Candidate("bad", 3.0) };

        var selected = ProportionalSelector.Select(
            population,
            SingleObjective.Minimize,
            count: 1,
            new SequenceRandomNumberGenerator(0.5));

        selected[0].Candidate.ShouldBe("good");
    }

    private static EvaluatedCandidate<string> Candidate(string name, double fitness) =>
        new(name, new ObjectiveVector(fitness));
}
