using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators;

public class PolynomialMutatorTests
{
    [Fact]
    public void Configuration_IsPubliclyInspectable()
    {
        var mutator = new PolynomialMutator(eta: 30, atLeastOnce: true);

        mutator.Eta.ShouldBe(30);
        mutator.AtLeastOnce.ShouldBeTrue();
    }

    [Fact]
    public void Mutate_FixedVariable_DoesNotMutate()
    {
        var parent = RealVector.Create(1.0);
        var bounds = RealVector.Create(1.0);

        var result = PolynomialMutator.Mutate(
            parent,
            RandomNumberGenerator.Create(42),
            bounds,
            bounds,
            20,
            true);

        result.ShouldBe(parent);
    }

    [Fact]
    public void Mutate_AtLeastOnceForcesOnePositionWhenTheMaskIsEmpty()
    {
        var parent = RealVector.Create(0.5, 0.5);
        var random = new SequenceRandomNumberGenerator(0.9, 0.9, 0.1, 0.0);

        var result = PolynomialMutator.Mutate(
            parent,
            random,
            minimum: 0,
            maximum: 1,
            eta: 20,
            atLeastOnce: true);

        result.ShouldBe(RealVector.Create(0, 0.5));
    }
}
