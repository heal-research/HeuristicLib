using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Tests.Optimization;

public class HyperVolumeCalculatorTests
{
  private static ObjectiveVector Vec(params double[] values) => new(values);

  private static Objective Obj(params ObjectiveDirection[] directions) => new(directions, new LexicographicComparer(directions));

  [Fact]
  public void Calculate_EmptyFront_ReturnsZero()
  {
    var hv = HyperVolumeCalculator.Calculate(
      [],
      Vec(1, 1),
      Obj(ObjectiveDirection.Minimize, ObjectiveDirection.Minimize));

    Assert.Equal(0.0, hv);
  }

  [Fact]
  public void Calculate_2D_Minimization_SinglePoint_ReturnsRectangleArea()
  {
    // Rectangle from point (0.25, 0.5) to reference (1, 1):
    // width = 0.75, height = 0.5, area = 0.375
    var hv = HyperVolumeCalculator.Calculate(
      [Vec(0.25, 0.5)],
      Vec(1, 1),
      Obj(ObjectiveDirection.Minimize, ObjectiveDirection.Minimize));

    Assert.Equal(0.375, hv, precision: 12);
  }

  [Fact]
  public void Calculate_2D_Minimization_TwoNonDominatedPoints_ReturnsUnionArea()
  {
    // Points: (0.2, 0.8), (0.6, 0.3), reference: (1, 1)
    //
    // Expected:
    // [0.2..0.6] * [0.8..1.0] = 0.4 * 0.2 = 0.08
    // [0.6..1.0] * [0.3..1.0] = 0.4 * 0.7 = 0.28
    // total = 0.36
    var hv = HyperVolumeCalculator.Calculate(
      [Vec(0.6, 0.3), Vec(0.2, 0.8)],
      Vec(1, 1),
      Obj(ObjectiveDirection.Minimize, ObjectiveDirection.Minimize));

    Assert.Equal(0.36, hv, precision: 12);
  }

  [Fact]
  public void Calculate_2D_IgnoresPointsThatDoNotDominateReference()
  {
    var hv = HyperVolumeCalculator.Calculate(
      [
        Vec(0.25, 0.5),
        Vec(1.2, 0.5), // does not dominate reference in minimization
        Vec(0.5, 1.1) // does not dominate reference in minimization
      ],
      Vec(1, 1),
      Obj(ObjectiveDirection.Minimize, ObjectiveDirection.Minimize));

    Assert.Equal(0.375, hv, precision: 12);
  }

  [Fact]
  public void Calculate_2D_Maximization_SinglePoint_ReturnsRectangleArea()
  {
    // Maximization: reference must be dominated by front point.
    // Rectangle from reference (0, 0) to point (0.4, 0.75):
    // area = 0.3
    var hv = HyperVolumeCalculator.Calculate(
      [Vec(0.4, 0.75)],
      Vec(0, 0),
      Obj(ObjectiveDirection.Maximize, ObjectiveDirection.Maximize));

    Assert.Equal(0.3, hv, precision: 12);
  }

  [Fact]
  public void Calculate_MoreThanTwoDimensions_WithMaximization_Throws()
  {
    var ex = Assert.Throws<NotImplementedException>(() =>
      HyperVolumeCalculator.Calculate(
        [Vec(1, 1, 1)],
        Vec(0, 0, 0),
        Obj(
          ObjectiveDirection.Maximize,
          ObjectiveDirection.Maximize,
          ObjectiveDirection.Maximize)));

    Assert.Contains("more than two dimensions", ex.Message);
  }

  [Fact]
  public void Calculate_3D_Minimization_SinglePoint_ReturnsCuboidVolume()
  {
    // Cuboid from point (0.2, 0.3, 0.4) to reference (1, 1, 1):
    // 0.8 * 0.7 * 0.6 = 0.336
    var hv = HyperVolumeCalculator.Calculate(
      [Vec(0.2, 0.3, 0.4)],
      Vec(1, 1, 1),
      Obj(
        ObjectiveDirection.Minimize,
        ObjectiveDirection.Minimize,
        ObjectiveDirection.Minimize));

    Assert.Equal(0.336, hv, precision: 12);
  }
}
