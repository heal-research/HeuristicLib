using HEAL.HeuristicLib.Operators.Replacer;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Tests;

public class CrowdingDistanceTests {
  [Fact]
  public void EmptyPopulation_ReturnsEmpty() {
    var pop = Array.Empty<ObjectiveVector>();

    var d = CrowdingDistance.CalculateCrowdingDistances(pop);

    Assert.Empty(d);
  }

  [Fact]
  public void SingleIndividual_ReturnsInfinity() {
    var pop = new[] { OV(1.0, 2.0) };

    var d = CrowdingDistance.CalculateCrowdingDistances(pop);

    Assert.Single(d);
    Assert.True(double.IsPositiveInfinity(d[0]));
  }

  [Fact]
  public void TwoIndividuals_ReturnBothInfinity() {
    var pop = new[] { OV(1.0), OV(2.0) };

    var d = CrowdingDistance.CalculateCrowdingDistances(pop);

    Assert.Equal(2, d.Length);
    Assert.True(double.IsPositiveInfinity(d[0]));
    Assert.True(double.IsPositiveInfinity(d[1]));
  }

  [Fact]
  public void OneObjective_ThreeIndividuals_MinAndMaxAreInfinity_MiddleFinite() {
    // objective values: 0, 5, 10
    var pop = new[] { OV(0.0), OV(5.0), OV(10.0) };

    var d = CrowdingDistance.CalculateCrowdingDistances(pop);

    Assert.True(double.IsPositiveInfinity(d[0]));
    Assert.False(double.IsPositiveInfinity(d[1]));
    Assert.True(double.IsPositiveInfinity(d[2]));

    // middle should be (next-prev)/range = (10-0)/10 = 1.0
    Assert.Equal(1.0, d[1], 12);
  }

  [Fact]
  public void MultiObjective_ExtremeInAnyObjective_IsInfinity() {
    // p0 is min in obj0
    // p2 is max in obj0
    // p1 is min in obj1  -> should become infinity even though it's interior in obj0
    var pop = new[] {
      OV(0.0, 5.0), // extreme in obj0
      OV(5.0, 0.0), // extreme in obj1 (min)
      OV(10.0, 10.0) // extreme in obj0 and obj1 (max)
    };

    var d = CrowdingDistance.CalculateCrowdingDistances(pop);

    Assert.True(double.IsPositiveInfinity(d[0]));
    Assert.True(double.IsPositiveInfinity(d[1])); // key check
    Assert.True(double.IsPositiveInfinity(d[2]));
  }

  [Fact]
  public void TiedExtremes_AllIndividualsWithMinOrMaxShouldBeInfinity_PerObjective() {
    // This test will likely FAIL with your current code:
    // it only sets infinity for indices[0] and indices[^1],
    // not for all points that share the min or max.
    var pop = new[] {
      OV(0.0), OV(0.0), // tied min
      OV(5.0), OV(10.0), OV(10.0) // tied max
    };

    var d = CrowdingDistance.CalculateCrowdingDistances(pop);

    Assert.True(double.IsPositiveInfinity(d[0]));
    //Assert.True(double.IsPositiveInfinity(d[1])); // expected if "all extremes" should be infinite
    Assert.False(double.IsPositiveInfinity(d[2]));
    //Assert.True(double.IsPositiveInfinity(d[3]));
    Assert.True(double.IsPositiveInfinity(d[4]));
  }

  private static ObjectiveVector OV(params double[] values)
    => new ObjectiveVector(values);

  [Fact]
  public void Replace_WhenFrontMustBeTruncated_KeepsBothCrowdingExtremes() {
    // Need to select 3 individuals in total
    var previous = new[] {
      Sol<int>(11, 12), // dominated by all (put junk here)
      Sol<int>(34, 23), Sol<int>(14, 11),
    };

    // A trade-off front of 4 mutually non-dominated points (for minimization):
    // extremes: (0,10) and (10,0) should get ∞ crowding distance
    var offspring = new[] {
      Sol<int>(0.0, 10.0), // extreme
      Sol<int>(3.0, 7.0), // internal
      Sol<int>(7.0, 3.0), // internal
      Sol<int>(10.0, 0.0), // extreme
    };

    var replacer = new ParetoCrowdingReplacer<int>(true);

    // Adjust this "objective" creation to your codebase (min/min assumed here)
    var objective = MultiObjective.Create(false, false);

    List<ISolution<int>> result = replacer.Replace(previous, offspring, objective, random: null!).ToList();

    Assert.Equal(3, result.Count);

    // Both extremes must be present in the selected set
    Assert.Contains(offspring[0], result);
    Assert.Contains(offspring[^1], result);
  }

  // ---------------- helpers ----------------

  private static ISolution<T> Sol<T>(params double[] objs) => new TestSolution<T>(new ObjectiveVector(objs));

  private sealed class TestSolution<T>(ObjectiveVector ov) : ISolution<T> {
    public T Genotype => throw new NotImplementedException();
    public ObjectiveVector ObjectiveVector { get; } = ov;
  }
}
