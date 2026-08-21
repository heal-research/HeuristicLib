using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.PythonInterop;

namespace HEAL.HeuristicLib.Tests.Optimization;

public class MultiObjectiveTests
{
    [Fact]
    public void Angle()
    {
        RealVector r = [1, 2, 3];
        RealVector r2 = [0, 0, 1];
        var a = r.Angle(r2);

        RealVector zeroes = [-1.71235326, -3.13907273, 3.12722378, 1.74460128, 0.77062594, -3.19114179, -4.53671968, -4.0406267, -2.83460651, 4.12024721];
        var p = ProblemGeneration.SphereRastriginProblem(zeroes.Count, -5, 5, 0.5);
        var zeroes1 = zeroes + 0.5;
        RealVector up = [0, 1];
        RealVector down = [0, -1];
        var maxangle = up.Angle(down);
        var angles = PythonCorrelationAnalysis.GetPseudoCorrelations([zeroes, zeroes1], p);
    }

    [Fact]
    public void EmptyInput_ReturnsNoFronts_AndEmptyRank()
    {
        var solutions = Array.Empty<EvaluatedCandidate<object>>();
        var objective = MinimizeAll(2);

        var fronts = DominationCalculator.CalculateAllParetoFronts(solutions, objective, out var rank);

        fronts.ShouldBeEmpty();
        rank.ShouldBeEmpty();
    }

    [Fact]
    public void TwoObjectives_ThreeFronts_SimpleSeparation()
    {
        // Arrange: minimize both objectives.
        // A dominates everyone; {C,D,E} are mutually non-dominating; B is dominated by E
        // Points: A(1,1), B(2,2), C(1,3), D(3,1), E(2,1.5)
        var solutions = new[] { Sol("A", 1.0, 1.0), Sol("B", 2.0, 2.0), Sol("C", 1.0, 3.0), Sol("D", 3.0, 1.0), Sol("E", 2.0, 1.5) };

        var objective = MinimizeAll(2);

        // Act
        var fronts = DominationCalculator.CalculateAllParetoFronts(solutions, objective, out var rank, true);

        // Assert
        var ids = FrontIds(fronts);
        fronts.Count.ShouldBe(3);
        ids[0].ShouldBe(["A"]); // Front 0
        ids[1].ShouldBe(["C", "D", "E"]); // Front 1
        ids[2].ShouldBe(["B"]); // Front 2

        // Rank array matches positions: A=0; C/D/E=1; B=2
        var map = solutions.Select((s, i) => (s.Candidate, i)).ToDictionary(keySelector: x => x.Candidate, elementSelector: x => x.i);
        rank[map["A"]].ShouldBe(0);
        new[] { "C", "D", "E" }.ShouldAllBe(id => rank[map[id]] == 1);
        rank[map["B"]].ShouldBe(2);
    }

    [Fact]
    public void DuplicatePoints_AreBothNonDominated_WhenDominateOnEqualFalse()
    {
        // Arrange: A(1,1), B(1,1), C(2,2). When equal doesn't dominate, A and B share front 0.
        var solutions = new[] { Sol("A", 1.0, 1.0), Sol("B", 1.0, 1.0), Sol("C", 2.0, 2.0) };
        var objective = MinimizeAll(2);

        // Act
        var fronts = DominationCalculator.CalculateAllParetoFronts(solutions, objective, out var rank, false);

        // Assert
        var ids = FrontIds(fronts);
        fronts.Count.ShouldBe(2);
        ids[0].ShouldBe(["A", "B"]); // both non-dominated
        ids[1].ShouldBe(["C"]);

        var map = solutions.Select((s, i) => (s.Candidate, i)).ToDictionary(keySelector: x => x.Candidate, elementSelector: x => x.i);
        rank[map["A"]].ShouldBe(0);
        rank[map["B"]].ShouldBe(0);
        rank[map["C"]].ShouldBe(1);
    }

    [Fact]
    public void DuplicatePoints_OneIsDominated_WhenDominateOnEqualTrue()
    {
        // Arrange: Same set, but equal qualities *do* dominate.
        var solutions = new[] { Sol("A", 1.0, 1.0), Sol("B", 1.0, 1.0), Sol("C", 2.0, 2.0) };
        var objective = MinimizeAll(2);

        // Act
        var fronts = DominationCalculator.CalculateAllParetoFronts(solutions, objective, out var rank, true);

        // Assert
        fronts.Count.ShouldBe(3);
        var ids = FrontIds(fronts);
        ids[0].Concat(ids[1]).OrderBy(x => x).ShouldBe(["A", "B"]); // Implementation may break ties arbitrarily
        ids[2].ShouldBe(["C"]);
    }

    private static string[][] FrontIds(List<List<EvaluatedCandidate<string>>> fronts)
      => fronts.Select(f => f.Select(s => s.Candidate).OrderBy(x => x).ToArray()).ToArray();

    private static EvaluatedCandidate<string> Sol(string id, params double[] values) => new(id, values);

    private static ObjectiveDirections MinimizeAll(int i) => new(Enumerable.Repeat(ObjectiveDirection.Minimize, i).ToArray(), NoTotalOrderComparer.Instance);
}
