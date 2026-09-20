using HEAL.HeuristicLib.Encodings.BoolVectors;

namespace HEAL.HeuristicLib.Tests.Operators.Mutators.BoolVectorMutators;

public class BitSwapMutatorTests
{
    [Fact]
    public void MutateCandidate_KeepsTheResultInTheSearchSpace()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 6, cardinality: 3);
        var mutator = new BitSwapMutator();
        var parent = BoolVector.Create(true, true, true, false, false, false);

        for (var seed = 0; seed < 25; seed++)
        {
            var child = mutator.MutateCandidate(parent, RandomNumberGenerator.Create(seed), space);

            space.Contains(child).ShouldBeTrue();
        }
    }

    [Fact]
    public void MutateCandidate_ChangesTheCandidate_WhenASwapIsAvailable()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 6, cardinality: 3);
        var mutator = new BitSwapMutator();
        var parent = BoolVector.Create(true, true, true, false, false, false);

        var child = mutator.MutateCandidate(parent, RandomNumberGenerator.Create(42), space);

        child.ShouldNotBe(parent);
    }

    [Fact]
    public void MutateCandidate_LeavesTheParentUnchanged()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality: 2);
        var mutator = new BitSwapMutator();
        var parent = BoolVector.Create(true, true, false, false);

        mutator.MutateCandidate(parent, RandomNumberGenerator.Create(7), space);

        parent.ShouldBe(BoolVector.Create(true, true, false, false));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void MutateCandidate_ReturnsTheInput_WhenTheSpaceHasOnlyOneMember(int cardinality)
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 4, cardinality);
        var mutator = new BitSwapMutator();
        var parent = BoolVector.Create(Enumerable.Repeat(cardinality == 4, 4));

        var child = mutator.MutateCandidate(parent, RandomNumberGenerator.Create(3), space);

        child.ShouldBe(parent);
    }

    /// <summary>
    /// The mutator reads the target cardinality from the search space rather than from the candidate, so a candidate
    /// that arrived off cardinality moves toward the space instead of keeping its error.
    /// </summary>
    [Theory]
    [InlineData(4, 3)]
    [InlineData(1, 2)]
    public void Mutate_MovesAnOffCardinalityCandidateTowardTheTarget(int setCount, int expectedSetCount)
    {
        var candidate = BoolVector.Create(
            Enumerable.Range(0, 6).Select(index => index < setCount));

        var child = BitSwapMutator.Mutate(candidate, RandomNumberGenerator.Create(11), cardinality: 3);

        child.Count(element => element).ShouldBe(expectedSetCount);
    }

    [Fact]
    public void Mutate_ConvergesToTheTargetCardinality()
    {
        var space = new FixedCardinalityBoolVectorSearchSpace(length: 8, cardinality: 3);
        var random = RandomNumberGenerator.Create(2026);
        var candidate = BoolVector.Create(Enumerable.Repeat(true, 8));

        for (var step = 0; step < 5; step++)
        {
            candidate = BitSwapMutator.Mutate(candidate, random, space);
        }

        space.Contains(candidate).ShouldBeTrue();
    }
}
