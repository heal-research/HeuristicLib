using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

public class PolynomialMutatorTests
{
    [Fact]
    public void Mutate_FixedVariable_DoesNotMutate()
    {
        var parent = RealVector.Create(1.0);
        var bounds = RealVector.Create(1.0);

        var result = PolynomialMutator.Mutate(
          parent,
          RandomNumberGenerator.Create(42),
          20,
          true,
          bounds,
          bounds);

        result.ShouldBe(parent);
    }
}
