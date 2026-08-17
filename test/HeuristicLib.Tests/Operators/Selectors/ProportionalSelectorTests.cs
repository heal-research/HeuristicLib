using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Selectors;

/// <summary>
/// Pins how proportional selection treats non-finite fitness values. A share of the wheel has to be a non-negative
/// finite number, which neither NaN nor an infinite fitness produces on its own.
/// </summary>
public class ProportionalSelectorTests
{
    /// <summary>
    /// One NaN fitness must cost only its own candidate a share. Taking the window over it makes every weight NaN, so
    /// the whole population becomes unselectable.
    /// </summary>
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

    /// <summary>
    /// Every fitness being NaN leaves nothing to rank, so the draw falls back to a uniform one rather than producing
    /// an empty wheel.
    /// </summary>
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

    /// <summary>
    /// An infinitely bad fitness must not receive a negative share, which the non-windowing minimized form produces by
    /// subtracting it from a finite limit.
    /// </summary>
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

    /// <summary>
    /// An infinitely good fitness takes the whole wheel in the limit, so it is selected rather than yielding an
    /// infinite share that no draw can resolve.
    /// </summary>
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

    /// <summary>
    /// The ordinary finite case is unchanged: a better candidate under minimization receives the larger share.
    /// </summary>
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
