using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Selectors;

public class EliteSelectorTests
{
    [Fact]
    public void Select_TakesElitesFirstThenAsksChildForRemainingPlaces()
    {
        var requestedCounts = new List<int>();
        var recording = new RecordingSelector(requestedCounts.Add);
        var selected = Select(recording, elites: 1, count: 3);

        selected.Select(candidate => candidate.Candidate).ShouldBe(["best", "worst", "worst"]);
        requestedCounts.ShouldBe([2]);
    }

    [Fact]
    public void Select_WhenElitesFillRequestedCount_DoesNotInvokeChildSelector()
    {
        var requestedCounts = new List<int>();
        var recording = new RecordingSelector(requestedCounts.Add);
        var selected = Select(recording, elites: 3, count: 3);

        selected.Count.ShouldBe(3);
        requestedCounts.ShouldBeEmpty();
    }

    /// <summary>
    /// Elites above the requested count must not enlarge the selection, and must never ask the child for a
    /// negative number of places.
    /// </summary>
    [Fact]
    public void Select_WhenElitesExceedRequestedCount_ReturnsExactlyRequestedCountWithoutInvokingChildSelector()
    {
        var requestedCounts = new List<int>();
        var recording = new RecordingSelector(requestedCounts.Add);
        var selected = Select(recording, elites: 10, count: 2);

        selected.Count.ShouldBe(2);
        requestedCounts.ShouldBeEmpty();
    }

    [Fact]
    public void Select_WithNonpositiveElites_AsksChildForTheCompleteSelection()
    {
        var requestedCounts = new List<int>();
        var recording = new RecordingSelector(requestedCounts.Add);
        var selected = Select(recording, elites: 0, count: 3);

        selected.Count.ShouldBe(3);
        requestedCounts.ShouldBe([3]);
    }

    private static IReadOnlyList<EvaluatedCandidate<string>> Select(RecordingSelector childSelector, int elites, int count)
    {
        var population = new[]
        {
            EvaluatedCandidate.From("best", new ObjectiveVector(0)),
            EvaluatedCandidate.From("middle", new ObjectiveVector(1)),
            EvaluatedCandidate.From("worst", new ObjectiveVector(2))
        };

        var selector = new EliteSelector<string>(childSelector) { Elites = elites };
        var problem = FuncProblem.Create(static (string _) => 0.0, HEAL.HeuristicLib.Tests.TestSupport.Mocks.DummySearchSpace<string>.Instance, SingleObjective.Minimize);
        var executor = ResolutionScope.Create().Resolve<string, ISearchSpace<string>, IProblem<string, ISearchSpace<string>>>(selector, problem);

        return executor.Select(population, count, new SequenceRandomNumberGenerator(0.5));
    }

    /// <summary>
    /// Records every requested count so the tests can assert that the child is asked only for the places that
    /// actually remain, and is not called at all when none do.
    /// </summary>
    private sealed record RecordingSelector(Action<int> RecordRequestedCount) : StatelessSelector<string>
    {
        public override IReadOnlyList<EvaluatedCandidate<string>> Select(IReadOnlyList<EvaluatedCandidate<string>> population, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            RecordRequestedCount(count);
            return [.. Enumerable.Repeat(population[^1], count)];
        }
    }
}
