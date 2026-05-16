using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;

namespace HEAL.HeuristicLib.Tests.Problems.TestFunctions.MetaFunctions;

public class RotatedTestFunctionTests
{
  [Fact]
  public void Rotate_ShouldMultiplyMatrixAndVector()
  {
    var rotation = new double[,] {
      { 1, 2 },
      { 3, 4 }
    };
    RealVector vector = [5.0, 6.0];

    var result = RotatedTestFunction.Rotate(rotation, vector);

    result[0].ShouldBe(17.0, 1e-12); // 1*5 + 2*6
    result[1].ShouldBe(39.0, 1e-12); // 3*5 + 4*6
  }

  [Fact]
  public void Evaluate_ShouldEvaluateInnerFunctionOnRotatedVector()
  {
    var rotation = new double[,] {
      { 0, -1 },
      { 1, 0 }
    };
    var inner = new SphereFunction(2);
    var function = new RotatedTestFunction(rotation, inner);
    RealVector x = [3.0, 4.0];

    var result = function.Evaluate(x);

    // Rotation gives (-4, 3), sphere value stays 25
    result.ShouldBe(25.0, 1e-12);
  }

  [Fact]
  public void Dimension_ShouldBeTakenFromRotationRows()
  {
    var rotation = new double[,] {
      { 1, 0 },
      { 0, 1 }
    };
    var inner = new SphereFunction(2);
    var function = new RotatedTestFunction(rotation, inner);

    function.Dimension.ShouldBe(2);
  }

  [Fact]
  public void Rotate_ShouldThrow_WhenVectorLengthDoesNotMatchMatrixColumns()
  {
    var rotation = new double[,] {
      { 1, 0 },
      { 0, 1 }
    };
    RealVector vector = [1.0, 2.0, 3.0];

    Should.Throw<ArgumentOutOfRangeException>(() => RotatedTestFunction.Rotate(rotation, vector));
  }
}
