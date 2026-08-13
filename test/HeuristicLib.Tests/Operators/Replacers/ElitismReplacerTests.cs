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
}
