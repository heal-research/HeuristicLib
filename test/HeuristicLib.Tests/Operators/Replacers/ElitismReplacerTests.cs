using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Tests.Operators.Replacers;

public class ElitismReplacerTests
{
    [Fact]
    public void NegativeElites_RetainsNoPreviousCandidatesAndRequestsAdditionalOffspring()
    {
        var previous = new[]
        {
            EvaluatedCandidate.From("previous", new ObjectiveVector(0))
        };
        var offspring = new[]
        {
            EvaluatedCandidate.From("offspring-1", new ObjectiveVector(1)),
            EvaluatedCandidate.From("offspring-2", new ObjectiveVector(2)),
            EvaluatedCandidate.From("offspring-3", new ObjectiveVector(3))
        };

        var result = ElitismReplacer.Replace(previous, offspring, SingleObjective.Minimize, count: 2, elites: -1);

        result.Select(candidate => candidate.Candidate).ShouldBe(["offspring-1", "offspring-2", "offspring-3"]);
    }

    /// <summary>
    /// Elites above the requested count must not enlarge the replacement.
    /// </summary>
    [Fact]
    public void ElitesExceedingRequestedCount_ReturnsExactlyRequestedCountAndNoOffspring()
    {
        var previous = new[]
        {
            EvaluatedCandidate.From("previous-1", new ObjectiveVector(0)),
            EvaluatedCandidate.From("previous-2", new ObjectiveVector(1)),
            EvaluatedCandidate.From("previous-3", new ObjectiveVector(2))
        };
        var offspring = new[]
        {
            EvaluatedCandidate.From("offspring-1", new ObjectiveVector(3))
        };

        var result = ElitismReplacer.Replace(previous, offspring, SingleObjective.Minimize, count: 2, elites: 10);

        result.Select(candidate => candidate.Candidate).ShouldBe(["previous-1", "previous-2"]);
    }

    [Fact]
    public void ElitesBelowRequestedCount_FillsRemainingPlacesFromOffspring()
    {
        var previous = new[]
        {
            EvaluatedCandidate.From("previous-1", new ObjectiveVector(0)),
            EvaluatedCandidate.From("previous-2", new ObjectiveVector(1))
        };
        var offspring = new[]
        {
            EvaluatedCandidate.From("offspring-1", new ObjectiveVector(2)),
            EvaluatedCandidate.From("offspring-2", new ObjectiveVector(3))
        };

        var result = ElitismReplacer.Replace(previous, offspring, SingleObjective.Minimize, count: 3, elites: 1);

        result.Select(candidate => candidate.Candidate).ShouldBe(["previous-1", "offspring-1", "offspring-2"]);
    }
}
