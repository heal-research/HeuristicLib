using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Objectives;

namespace HEAL.HeuristicLib.Tests.Algorithms.AutoEC;

public class DynamicRacingQualityTraceTests
{
    [Fact]
    public void AddAndAddRace_PreserveTracePointAndRaceSummary()
    {
        var trace = new DynamicRacingQualityTrace<int>();
        var configuration = new CompositeGenotype<RealVector, IntegerVector>(
            RealVector.FromOwnedArray([0.1]),
            IntegerVector.FromOwnedArray([24]));

        trace.Add(new DynamicRacingQualityTracePoint<int>(
            Race: 3,
            Epoch: 5,
            ContenderIndex: 1,
            IsIncumbent: false,
            Origin: DynamicRacingConfigurationOrigin.HallOfFame,
            Configuration: configuration,
            Candidate: 42,
            ObjectiveVector: new ObjectiveVector(0.25),
            ObjectiveValue: 0.25,
            ProblemEvaluationCount: 120,
            ContenderEvaluationCount: 24));
        trace.AddRace(new DynamicRacingRaceSummary(
            Race: 3,
            Epoch: 5,
            TerminationReason: DynamicRacingRaceTerminationReason.EarlyTermination,
            WinnerContenderIndex: 1,
            WinnerConfiguration: configuration,
            ProblemEvaluationCount: 144));

        var point = trace.Points.ShouldHaveSingleItem();
        point.Origin.ShouldBe(DynamicRacingConfigurationOrigin.HallOfFame);
        point.IsIncumbent.ShouldBeFalse();
        point.ContenderEvaluationCount.ShouldBe(24);

        var race = trace.Races.ShouldHaveSingleItem();
        race.TerminationReason.ShouldBe(DynamicRacingRaceTerminationReason.EarlyTermination);
        race.WinnerContenderIndex.ShouldBe(1);
    }
}
