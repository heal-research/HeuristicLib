using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Tests;

public class MultiObjectiveTests {
  [Fact]
  public void EmptyInput_ReturnsNoFronts_AndEmptyRank() {
    var solutions = Array.Empty<Solution<object>>();
    var objective = MinimizeAll(2);

    var fronts = DominationCalculator.CalculateAllParetoFronts(solutions, objective, out var rank, dominateOnEqualQualities: true);

    Assert.Empty(fronts);
    Assert.Empty(rank);
  }

  [Fact]
  public void TwoObjectives_ThreeFronts_SimpleSeparation() {
    // Arrange: minimize both objectives.
    // A dominates everyone; {C,D,E} are mutually non-dominating; B is dominated by E
    // Points: A(1,1), B(2,2), C(1,3), D(3,1), E(2,1.5)
    var solutions = new[] { Sol("A", 1.0, 1.0), Sol("B", 2.0, 2.0), Sol("C", 1.0, 3.0), Sol("D", 3.0, 1.0), Sol("E", 2.0, 1.5), };

    var objective = MinimizeAll(2);

    // Act
    var fronts = DominationCalculator.CalculateAllParetoFronts(solutions, objective, out var rank, dominateOnEqualQualities: true);

    // Assert
    var ids = FrontIds(fronts);
    Assert.Equal(3, fronts.Count);
    Assert.Equal(["A"], ids[0]); // Front 0
    Assert.Equal(["C", "D", "E"], ids[1]); // Front 1
    Assert.Equal(["B"], ids[2]); // Front 2

    // Rank array matches positions: A=0; C/D/E=1; B=2
    var map = solutions.Select((s, i) => (s.Genotype, i)).ToDictionary(x => x.Genotype, x => x.i);
    Assert.Equal(0, rank[map["A"]]);
    Assert.All(["C", "D", "E"], id => Assert.Equal(1, rank[map[id]]));
    Assert.Equal(2, rank[map["B"]]);
  }

  [Fact]
  public void DuplicatePoints_AreBothNonDominated_WhenDominateOnEqualFalse() {
    // Arrange: A(1,1), B(1,1), C(2,2). When equal doesn't dominate, A and B share front 0.
    var solutions = new[] { Sol("A", 1.0, 1.0), Sol("B", 1.0, 1.0), Sol("C", 2.0, 2.0), };
    var objective = MinimizeAll(2);

    // Act
    var fronts = DominationCalculator.CalculateAllParetoFronts(solutions, objective, out var rank, dominateOnEqualQualities: false);

    // Assert
    var ids = FrontIds(fronts);
    Assert.Equal(2, fronts.Count);
    Assert.Equal(["A", "B"], ids[0]); // both non-dominated
    Assert.Equal(["C"], ids[1]);

    var map = solutions.Select((s, i) => (s.Genotype, i)).ToDictionary(x => x.Genotype, x => x.i);
    Assert.Equal(0, rank[map["A"]]);
    Assert.Equal(0, rank[map["B"]]);
    Assert.Equal(1, rank[map["C"]]);
  }

  [Fact]
  public void DuplicatePoints_OneIsDominated_WhenDominateOnEqualTrue() {
    // Arrange: Same set, but equal qualities *do* dominate.
    var solutions = new[] { Sol("A", 1.0, 1.0), Sol("B", 1.0, 1.0), Sol("C", 2.0, 2.0), };
    var objective = MinimizeAll(2);

    // Act
    var fronts = DominationCalculator.CalculateAllParetoFronts(solutions, objective, out var rank, dominateOnEqualQualities: true);

    // Assert
    Assert.Equal(3, fronts.Count);
    var ids = FrontIds(fronts);
    Assert.Equal(["A", "B"], ids[0].Append(ids[1]).OrderBy(x => x)); // Implementation may break ties arbitrarily
    Assert.Equal(["C"], ids[2]);
  }

  private static string[][] FrontIds(List<List<Solution<string>>> fronts)
    => fronts.Select(f => f.Select(s => s.Genotype).OrderBy(x => x).ToArray()).ToArray();

  private static Solution<string> Sol(string id, params double[] values) => new(id, values);
  private static Objective MinimizeAll(int i) => new(Enumerable.Repeat(ObjectiveDirection.Minimize, i).ToArray(), new NoTotalOrderComparer());
}
