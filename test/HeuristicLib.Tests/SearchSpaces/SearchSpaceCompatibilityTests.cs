using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.SearchSpaces;

public class SearchSpaceCompatibilityTests
{
    /// <summary>
    /// Stands in for an operator that states no contract at all.
    /// </summary>
    private sealed record NothingDeclared : IOperator;

    /// <summary>
    /// Answers <see langword="true"/> for the named kinds, <see langword="false"/> for anything the search space
    /// states that is not among them, and <see langword="null"/> when <paramref name="opinionated"/> is false.
    /// </summary>
    private sealed record Declared(Type[] Ensured, IReadOnlyList<ISearchInvariant<BoolVector>> RequiredInputInvariants, bool opinionated = true) : IOperator, IInvariantContract<BoolVector>
    {
        public bool? Ensures(ISearchInvariant<BoolVector> invariant) =>
            !opinionated ? null : Ensured.Contains(invariant.GetType());
    }

    [Fact]
    public void SpaceWithoutInvariants_AcceptsAnOperatorThatDeclaresNothing()
    {
        SearchSpaceCompatibility.IsCompatible(new NothingDeclared(), new NoInvariantSpace()).ShouldBeTrue();
    }

    /// <summary>
    /// Declaring is opt in. An operator that says nothing keeps whatever answer the type system already gave, so a
    /// search space can adopt invariants without invalidating operators written before them.
    /// </summary>
    [Fact]
    public void SpaceWithInvariants_DoesNotCheckAnOperatorThatDeclaresNothing()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);

        SearchSpaceCompatibility.Check(new NothingDeclared(), space).ShouldBeEmpty();
        SearchSpaceCompatibility.Check(new Declared([], [], opinionated: false), space).ShouldBeEmpty();
    }

    /// <summary>
    /// A contract may depend on the operator's own configured values, so a guarantee that holds only over part of a
    /// parameter range can be stated honestly rather than dropped or overclaimed.
    /// </summary>
    [Fact]
    public void AContractMayDependOnTheOperatorsOwnParameters()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);

        SearchSpaceCompatibility.IsCompatible(new ParameterDependent(Strength: 0.5), space).ShouldBeTrue();

        var incompatibility = SearchSpaceCompatibility.Check(new ParameterDependent(Strength: 3.0), space).ShouldHaveSingleItem();
        incompatibility.Reason.ShouldBe(IncompatibilityReason.OutputMayLeaveTheSearchSpace);
        incompatibility.InvariantName.ShouldBe("Cardinality(2)");
    }

    /// <summary>
    /// Stands in for an operator whose guarantee depends on how it is configured.
    /// </summary>
    /// <param name="Strength">
    /// Gets the perturbation strength. Larger values search more widely and are an ordinary choice; the range over
    /// which cardinality is still preserved is stated by <see cref="SatisfiedInvariantKinds"/>.
    /// </param>
    private sealed record ParameterDependent(double Strength) : IOperator, IInvariantContract<BoolVector>
    {
        /// <summary>
        /// Cardinality survives only while <see cref="Strength"/> stays within <c>[0, 1]</c>. Length always survives.
        /// </summary>
        /// <remarks>
        /// This is the range in which the guarantee holds, not a recommendation. A strength above one remains a valid
        /// configuration; it simply stops keeping candidates inside a fixed cardinality search space, and validation
        /// reports that against the configuration rather than treating it as a defect.
        /// </remarks>
        public bool? Ensures(ISearchInvariant<BoolVector> invariant) => invariant switch
        {
            BoolVectorLength => true,
            BoolVectorCardinality => Strength is >= 0.0 and <= 1.0,
            _ => null
        };
    }

    [Fact]
    public void PreservedKinds_AreMatchedByTypeNotByValue()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 9, cardinality: 3);
        var declared = new Declared([typeof(BoolVectorLength), typeof(BoolVectorCardinality)], []);

        SearchSpaceCompatibility.IsCompatible(declared, space).ShouldBeTrue();
    }

    [Fact]
    public void RequiredInputInvariant_IsSatisfiedByAStrongerSpaceInvariant()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 5, cardinality: 3);
        var declared = new Declared([typeof(BoolVectorLength), typeof(BoolVectorCardinality)], [new BoolVectorMinimumSetElements(2)]);

        SearchSpaceCompatibility.IsCompatible(declared, space).ShouldBeTrue();
    }

    [Fact]
    public void RequiredInputInvariant_IsNotSatisfiedByAWeakerSpaceInvariant()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 5, cardinality: 1);
        var declared = new Declared([typeof(BoolVectorLength), typeof(BoolVectorCardinality)], [new BoolVectorMinimumSetElements(2)]);

        var incompatibility = SearchSpaceCompatibility.Check(declared, space).ShouldHaveSingleItem();

        incompatibility.Reason.ShouldBe(IncompatibilityReason.InputMayNotBeAccepted);
        incompatibility.InvariantName.ShouldBe("AtLeastSet(2)");
    }

    [Fact]
    public void CardinalityEntailsAMinimumItMeetsAndNothingStronger()
    {
        var exactlyThree = new BoolVectorCardinality(3);

        exactlyThree.Entails(new BoolVectorMinimumSetElements(2)).ShouldBeTrue();
        exactlyThree.Entails(new BoolVectorMinimumSetElements(3)).ShouldBeTrue();
        exactlyThree.Entails(new BoolVectorMinimumSetElements(4)).ShouldBeFalse();
        exactlyThree.Entails(new BoolVectorCardinality(3)).ShouldBeTrue();
        exactlyThree.Entails(new BoolVectorCardinality(2)).ShouldBeFalse();
        exactlyThree.Entails(new BoolVectorLength(3)).ShouldBeFalse();
    }

    [Fact]
    public void InvariantsDescribeTheCandidatesTheyName()
    {
        var twoOfFour = BoolVector.Create(true, false, true, false);

        new BoolVectorLength(4).IsSatisfiedBy(twoOfFour).ShouldBeTrue();
        new BoolVectorLength(3).IsSatisfiedBy(twoOfFour).ShouldBeFalse();
        new BoolVectorCardinality(2).IsSatisfiedBy(twoOfFour).ShouldBeTrue();
        new BoolVectorMinimumSetElements(2).IsSatisfiedBy(twoOfFour).ShouldBeTrue();
        new BoolVectorMinimumSetElements(3).IsSatisfiedBy(twoOfFour).ShouldBeFalse();
    }

    /// <summary>
    /// A composition is only as strong as its weakest child: one child breaking an invariant decides the answer, and
    /// requirements accumulate across all of them.
    /// </summary>
    [Fact]
    public void ComposedContract_IsDecidedByTheWeakestChild()
    {
        var ensuresBoth = new Declared([typeof(BoolVectorLength), typeof(BoolVectorCardinality)], []);
        var ensuresLengthOnly = new Declared([typeof(BoolVectorLength)], [new BoolVectorMinimumSetElements(2)]);

        InvariantContractComposition.Ensures([ensuresBoth, ensuresLengthOnly], new BoolVectorLength(4)).ShouldBe(true);
        InvariantContractComposition.Ensures([ensuresBoth, ensuresLengthOnly], new BoolVectorCardinality(2)).ShouldBe(false);
        InvariantContractComposition.RequiredInputInvariants<BoolVector>([ensuresBoth, ensuresLengthOnly]).ShouldBe([new BoolVectorMinimumSetElements(2)]);
    }

    /// <summary>
    /// A child with no opinion makes the composition undecided rather than incompatible, so a composition of operators
    /// that never opted in stays unchecked.
    /// </summary>
    [Fact]
    public void ComposedContract_IsUndecidedWhenAChildHasNoOpinion()
    {
        var silent = new Declared([], [], opinionated: false);
        var ensuresLength = new Declared([typeof(BoolVectorLength)], []);

        InvariantContractComposition.Ensures([silent, silent], new BoolVectorLength(4)).ShouldBeNull();
        InvariantContractComposition.Ensures([silent, ensuresLength], new BoolVectorLength(4)).ShouldBeNull();
        InvariantContractComposition.Ensures([ensuresLength, ensuresLength], new BoolVectorLength(4)).ShouldBe(true);
    }

    private sealed record NoInvariantSpace : SearchSpace<BoolVector>
    {
        public override bool Contains(BoolVector candidate) => true;
    }
}
