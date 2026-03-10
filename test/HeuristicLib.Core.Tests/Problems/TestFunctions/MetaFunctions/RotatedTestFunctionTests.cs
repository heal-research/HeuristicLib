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
    var vector = new RealVector(5.0, 6.0);

    var result = RotatedTestFunction.Rotate(rotation, vector);

    Assert.Equal(17.0, result[0], 12); // 1*5 + 2*6
    Assert.Equal(39.0, result[1], 12); // 3*5 + 4*6
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
    var x = new RealVector(3.0, 4.0);

    var result = function.Evaluate(x);

    // Rotation gives (-4, 3), sphere value stays 25
    Assert.Equal(25.0, result, 12);
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

    Assert.Equal(2, function.Dimension);
  }

  [Fact]
  public void Rotate_ShouldThrow_WhenVectorLengthDoesNotMatchMatrixColumns()
  {
    var rotation = new double[,] {
      { 1, 0 },
      { 0, 1 }
    };
    var vector = new RealVector(1.0, 2.0, 3.0);

    Assert.Throws<ArgumentOutOfRangeException>(() => RotatedTestFunction.Rotate(rotation, vector));
  }
}
