using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Selectors;

public class EliteSelectorTests
{
    [Fact]
    public void Select_TakesElitesFirstThenAsksChildForRemainingPlaces()
    {
        var recording = new RecordingSelector();
        var selected = Select(recording, elites: 1, count: 3);

        selected.Select(candidate => candidate.Candidate).ShouldBe(["best", "worst", "worst"]);
        recording.RequestedCounts.ShouldBe([2]);
    }

    [Fact]
    public void Select_WhenElitesFillRequestedCount_DoesNotInvokeChildSelector()
    {
        var recording = new RecordingSelector();
        var selected = Select(recording, elites: 3, count: 3);

        selected.Count.ShouldBe(3);
        recording.RequestedCounts.ShouldBeEmpty();
    }

    /// <summary>
    /// Elites above the requested count must not enlarge the selection, and must never ask the child for a
    /// negative number of places.
    /// </summary>
    [Fact]
    public void Select_WhenElitesExceedRequestedCount_ReturnsExactlyRequestedCountWithoutInvokingChildSelector()
    {
        var recording = new RecordingSelector();
        var selected = Select(recording, elites: 10, count: 2);

        selected.Count.ShouldBe(2);
        recording.RequestedCounts.ShouldBeEmpty();
    }

    [Fact]
    public void Select_WithNonpositiveElites_AsksChildForTheCompleteSelection()
    {
        var recording = new RecordingSelector();
        var selected = Select(recording, elites: 0, count: 3);

        selected.Count.ShouldBe(3);
        recording.RequestedCounts.ShouldBe([3]);
    }

    private static IReadOnlyList<EvaluatedCandidate<string>> Select(RecordingSelector childSelector, int elites, int count)
    {
        var population = new[]
        {
            EvaluatedCandidate.From("best", new ObjectiveVector(0)),
            EvaluatedCandidate.From("middle", new ObjectiveVector(1)),
            EvaluatedCandidate.From("worst", new ObjectiveVector(2))
        };

        var selector = new EliteSelector<string, ISearchSpace<string>, IProblem<string, ISearchSpace<string>>>(childSelector) { Elites = elites };
        var instance = selector.CreateExecutionInstance(new ExecutionInstanceRegistry());

        return instance.Select(population, SingleObjective.Minimize, count, new SequenceRandomNumberGenerator(0.5), null!, null!);
    }

    /// <summary>
    /// Records every requested count so the tests can assert that the child is asked only for the places that
    /// actually remain, and is not called at all when none do.
    /// </summary>
    private sealed record RecordingSelector : StatelessSelector<string>
    {
        public List<int> RequestedCounts { get; } = [];

        public override IReadOnlyList<EvaluatedCandidate<string>> Select(IReadOnlyList<EvaluatedCandidate<string>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            RequestedCounts.Add(count);
            return [.. Enumerable.Repeat(population[^1], count)];
        }
    }
}
