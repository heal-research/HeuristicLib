using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.SearchSpaces;

public class RealVectorInvariantTests
{
    private static BoundedRealVectorSearchSpace UnitBox => new(length: 3, minimum: -1.0, maximum: 1.0);

    [Fact]
    public void TheSearchSpaceStatesItsLengthAndBounds()
    {
        UnitBox.Invariants.Select(invariant => invariant.Name).ShouldBe(["Length(3)", "Bounds([-1], [1])"]);
    }

    [Fact]
    public void BoundsDescribeTheCandidatesInsideThem()
    {
        var bounds = new RealVectorBounds(new RealVector(-1.0), new RealVector(1.0));

        bounds.IsSatisfiedBy(new RealVector(0.5, -0.5, 1.0)).ShouldBeTrue();
        bounds.IsSatisfiedBy(new RealVector(0.5, -0.5, 1.5)).ShouldBeFalse();
    }

    /// <summary>
    /// Tighter bounds entail looser ones. This is the direction that lets an operator producing within a narrow box be
    /// used over a wider search space, and stops the reverse.
    /// </summary>
    [Fact]
    public void TighterBoundsEntailLooserOnes()
    {
        var narrow = new RealVectorBounds(new RealVector(-0.5), new RealVector(0.5));
        var wide = new RealVectorBounds(new RealVector(-1.0), new RealVector(1.0));

        narrow.Entails(wide).ShouldBeTrue();
        wide.Entails(narrow).ShouldBeFalse();
        narrow.Entails(narrow).ShouldBeTrue();
    }

    [Fact]
    public void ClampingOperatorsAreUsableOverTheSearchSpace()
    {
        SearchSpaceCompatibility.IsCompatible(new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.1), UnitBox).ShouldBeTrue();
        SearchSpaceCompatibility.IsCompatible(new PolynomialMutator(), UnitBox).ShouldBeTrue();
        SearchSpaceCompatibility.IsCompatible(new SinglePointCrossover(), UnitBox).ShouldBeTrue();
        SearchSpaceCompatibility.IsCompatible(new AlphaBetaBlendCrossover(), UnitBox).ShouldBeTrue();
    }

    /// <summary>
    /// An alpha outside <c>[0, 1]</c> extrapolates beyond the parents, but the result is clamped, so bounds still
    /// hold. The contract says so rather than guessing, and verification confirms it. The commented alternative in
    /// <see cref="AlphaBetaBlendCrossover"/> shows the contract this operator would carry without that clamp.
    /// </summary>
    [Fact]
    public void BlendCrossoverKeepsItsGuaranteeOutsideTheOrdinaryAlphaRange()
    {
        var extrapolating = new AlphaBetaBlendCrossover { Alpha = 3.0 };

        SearchSpaceCompatibility.IsCompatible(extrapolating, UnitBox).ShouldBeTrue();

        var random = RandomNumberGenerator.Create(seed: 5);
        var crossed = AlphaBetaBlendCrossover.Cross(new RealVector(1.0, 1.0, 1.0), new RealVector(-1.0, -1.0, -1.0), random, UnitBox, alpha: 3.0);

        UnitBox.Contains(crossed).ShouldBeTrue();
    }

    /// <summary>
    /// A creator whose own bounds reach outside the search space is reported when the configuration is validated. The
    /// run would otherwise throw when it created its first candidate.
    /// </summary>
    [Fact]
    public void ACreatorOverridingBoundsBeyondTheSearchSpaceIsRejected()
    {
        var insideTheSpace = new UniformDistributedCreator { Minimum = new RealVector(-0.5), Maximum = new RealVector(0.5) };
        var beyondTheSpace = new UniformDistributedCreator { Minimum = new RealVector(-5.0), Maximum = new RealVector(5.0) };

        SearchSpaceCompatibility.IsCompatible(insideTheSpace, UnitBox).ShouldBeTrue();

        var incompatibility = SearchSpaceCompatibility.Check(beyondTheSpace, UnitBox).ShouldHaveSingleItem();
        incompatibility.Reason.ShouldBe(IncompatibilityReason.OutputMayLeaveTheSearchSpace);
        incompatibility.InvariantName.ShouldBe("Bounds([-1], [1])");
    }

    /// <summary>
    /// A creator that overrides nothing takes both length and bounds from the search space, so it answers for whatever
    /// space it is checked against rather than making a claim of its own.
    /// </summary>
    [Fact]
    public void ACreatorWithoutOverriddenBoundsTakesBothFromTheSearchSpace()
    {
        var creator = new UniformDistributedCreator();

        creator.Ensures(new RealVectorLength(3)).ShouldBe(true);
        creator.Ensures(new RealVectorBounds(new RealVector(-1.0), new RealVector(1.0))).ShouldBe(true);
        SearchSpaceCompatibility.IsCompatible(creator, UnitBox).ShouldBeTrue();
        SearchSpaceCompatibility.IsCompatible(creator, new BoundedRealVectorSearchSpace(length: 7, minimum: -9.0, maximum: 9.0)).ShouldBeTrue();
    }

    /// <summary>
    /// Every declaration is checked against behavior, so the compatibility results above rest on contracts that have
    /// been exercised rather than only asserted.
    /// </summary>
    [Fact]
    public void DeclaredContractsHoldWhenTheOperatorsAreRun()
    {
        var samples = Samples().ToArray();
        var random = RandomNumberGenerator.Create(seed: 11);

        InvariantContractVerification.Verify(new GaussianMutator(mutationRate: 1.0, mutationStrength: 5.0), UnitBox, samples,
            candidate => GaussianMutator.Mutate(candidate, random, UnitBox, mutationRate: 1.0, mutationStrength: 5.0)).ShouldBeEmpty();

        InvariantContractVerification.Verify(new AlphaBetaBlendCrossover { Alpha = 3.0 }, UnitBox, samples,
            candidate => AlphaBetaBlendCrossover.Cross(candidate, new RealVector(-1.0, -1.0, -1.0), random, UnitBox, alpha: 3.0)).ShouldBeEmpty();

        InvariantContractVerification.Verify(new PolynomialMutator(), UnitBox, samples,
            candidate => new PolynomialMutator().MutateCandidate(candidate, random, UnitBox)).ShouldBeEmpty();
    }

    private static IEnumerable<RealVector> Samples()
    {
        yield return new RealVector(0.0, 0.0, 0.0);
        yield return new RealVector(1.0, 1.0, 1.0);
        yield return new RealVector(-1.0, -1.0, -1.0);
        yield return new RealVector(0.75, -0.25, 0.5);
        yield return new RealVector(-0.99, 0.99, 0.01);
    }
}
