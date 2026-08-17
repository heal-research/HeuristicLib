using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Tests.Optimization;

/// <summary>
/// Pins how objective values and vectors are ordered: what <see cref="DominanceRelation.Equal"/> means, that a
/// vector never dominates itself, and that <see cref="double.NaN"/> is the worst value in both objective directions
/// while the infinities take part in the ordinary order.
/// </summary>
public class ObjectiveVectorComparisonTests
{
    [Fact]
    public void DefaultDominanceRelation_IsIncomparable()
    {
        // The zero value must assert no relation, so an unset value cannot be read as a claim that one vector is
        // better than or equal to another.
        default(DominanceRelation).ShouldBe(DominanceRelation.Incomparable);
    }

    [Fact]
    public void AVector_IsEqualToItselfRatherThanDominatingIt()
    {
        var vector = Vector(1.0, 2.0);

        vector.CompareTo(vector, MultiObjective.Minimize(2)).ShouldBe(DominanceRelation.Equal);
        vector.Dominates(vector, MultiObjective.Minimize(2)).ShouldBeFalse();
    }

    /// <summary>
    /// The reference-equality fast path must agree with the loop it skips, including for values the loop treats
    /// specially.
    /// </summary>
    [Fact]
    public void AVectorCarryingNaN_IsEqualToItself()
    {
        var vector = Vector(double.NaN, 1.0);

        vector.CompareTo(vector, MultiObjective.Minimize(2)).ShouldBe(DominanceRelation.Equal);
        vector.CompareTo(Vector(double.NaN, 1.0), MultiObjective.Minimize(2)).ShouldBe(DominanceRelation.Equal);
    }

    /// <summary>
    /// The fast path sits below the argument checks, so comparing a vector with itself still validates its arguments.
    /// </summary>
    [Fact]
    public void ComparingAVectorWithItself_StillValidatesTheDirections()
    {
        var vector = Vector(1.0, 2.0);

        Should.Throw<ArgumentException>(() => vector.CompareTo(vector, MultiObjective.Minimize(3)));
    }

    /// <summary>
    /// <see cref="DominanceRelation.Equal"/> is componentwise equality. A pair that trades off is
    /// <see cref="DominanceRelation.Incomparable"/> instead, which is the distinction the two members exist to draw.
    /// </summary>
    [Fact]
    public void Equal_MeansEqualOnEveryObjective()
    {
        var directions = MultiObjective.Minimize(2);

        Vector(1.0, 2.0).CompareTo(Vector(1.0, 2.0), directions).ShouldBe(DominanceRelation.Equal);
        Vector(1.0, 3.0).CompareTo(Vector(2.0, 1.0), directions).ShouldBe(DominanceRelation.Incomparable);
        Vector(1.0, 1.0).CompareTo(Vector(1.0, 2.0), directions).ShouldBe(DominanceRelation.Dominates);
        Vector(1.0, 2.0).CompareTo(Vector(1.0, 1.0), directions).ShouldBe(DominanceRelation.IsDominatedBy);
    }

    [Theory]
    [InlineData(ObjectiveDirection.Minimize)]
    [InlineData(ObjectiveDirection.Maximize)]
    public void NaN_IsTheWorstValueInBothDirections(ObjectiveDirection direction)
    {
        ObjectiveValue.Compare(double.NaN, 1.0, direction).ShouldBeGreaterThan(0);
        ObjectiveValue.Compare(1.0, double.NaN, direction).ShouldBeLessThan(0);
        ObjectiveValue.Compare(double.NaN, double.NaN, direction).ShouldBe(0);
    }

    /// <summary>
    /// NaN is worse than the worst ordered value, including an infinity pointing the wrong way. It is therefore the
    /// one value that <see cref="ObjectiveValue.WorstValue"/> does not bound.
    /// </summary>
    [Theory]
    [InlineData(ObjectiveDirection.Minimize)]
    [InlineData(ObjectiveDirection.Maximize)]
    public void NaN_IsWorseThanTheWorstOrderedValue(ObjectiveDirection direction)
    {
        ObjectiveValue.Compare(double.NaN, ObjectiveValue.WorstValue(direction).Value, direction).ShouldBeGreaterThan(0);
        ObjectiveValue.Compare(double.NaN, double.PositiveInfinity, direction).ShouldBeGreaterThan(0);
        ObjectiveValue.Compare(double.NaN, double.NegativeInfinity, direction).ShouldBeGreaterThan(0);
    }

    /// <summary>
    /// The sentinels are genuine bounds of the ordered range: no ordinary value, finite or infinite, is better than
    /// <see cref="ObjectiveValue.BestValue"/> or worse than <see cref="ObjectiveValue.WorstValue"/>. An extreme finite
    /// sentinel would fail this, because an objective value can legitimately be infinite.
    /// </summary>
    [Theory]
    [InlineData(ObjectiveDirection.Minimize)]
    [InlineData(ObjectiveDirection.Maximize)]
    public void TheSentinels_BoundEveryOrderedValue(ObjectiveDirection direction)
    {
        var best = ObjectiveValue.BestValue(direction).Value;
        var worst = ObjectiveValue.WorstValue(direction).Value;

        foreach (var value in new[] { 0.0, double.MaxValue, double.MinValue, double.PositiveInfinity, double.NegativeInfinity })
        {
            ObjectiveValue.Compare(best, value, direction).ShouldBeLessThanOrEqualTo(0);
            ObjectiveValue.Compare(worst, value, direction).ShouldBeGreaterThanOrEqualTo(0);
        }

        // They are also the identity elements for accumulating a best-so-far and worst-so-far value.
        ObjectiveValue.Compare(best, best, direction).ShouldBe(0);
        ObjectiveValue.Compare(worst, worst, direction).ShouldBe(0);
    }

    /// <summary>
    /// The infinities are ordinary participants in the order and therefore swap roles with the direction, which is
    /// exactly what NaN must not do.
    /// </summary>
    [Fact]
    public void TheInfinities_SwapRolesWithTheObjectiveDirection()
    {
        ObjectiveValue.Compare(double.NegativeInfinity, 0.0, ObjectiveDirection.Minimize).ShouldBeLessThan(0);
        ObjectiveValue.Compare(double.PositiveInfinity, 0.0, ObjectiveDirection.Minimize).ShouldBeGreaterThan(0);

        ObjectiveValue.Compare(double.PositiveInfinity, 0.0, ObjectiveDirection.Maximize).ShouldBeLessThan(0);
        ObjectiveValue.Compare(double.NegativeInfinity, 0.0, ObjectiveDirection.Maximize).ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(ObjectiveDirection.Minimize)]
    [InlineData(ObjectiveDirection.Maximize)]
    public void AVectorCarryingNaN_IsDominatedByAFiniteVector(ObjectiveDirection direction)
    {
        var directions = SingleObjective.Create(direction);

        Vector(double.NaN).CompareTo(Vector(1.0), directions).ShouldBe(DominanceRelation.IsDominatedBy);
        Vector(1.0).CompareTo(Vector(double.NaN), directions).ShouldBe(DominanceRelation.Dominates);
    }

    /// <summary>
    /// The total-order comparers rank NaN worst too, so a NaN candidate sorts to the end of a population rather than
    /// to its front.
    /// </summary>
    [Theory]
    [InlineData(ObjectiveDirection.Minimize)]
    [InlineData(ObjectiveDirection.Maximize)]
    public void TheSingleObjectiveComparer_RanksNaNLast(ObjectiveDirection direction)
    {
        var comparer = SingleObjective.Create(direction).TotalOrderComparer;

        comparer.Compare(Vector(double.NaN), Vector(1.0)).ShouldBeGreaterThan(0);
        comparer.Compare(Vector(1.0), Vector(double.NaN)).ShouldBeLessThan(0);
    }

    [Fact]
    public void TheLexicographicComparer_RanksNaNLastInEveryDimension()
    {
        var directions = new[] { ObjectiveDirection.Minimize, ObjectiveDirection.Maximize };
        var comparer = MultiObjective.Lexicographic(directions, order: null).TotalOrderComparer;

        comparer.Compare(Vector(double.NaN, 1.0), Vector(1.0, 1.0)).ShouldBeGreaterThan(0);
        comparer.Compare(Vector(1.0, double.NaN), Vector(1.0, 1.0)).ShouldBeGreaterThan(0);
        comparer.Compare(Vector(1.0, 1.0), Vector(1.0, double.NaN)).ShouldBeLessThan(0);
    }

    [Fact]
    public void TheWeightedSumComparer_RanksANaNSumLast()
    {
        var directions = new[] { ObjectiveDirection.Minimize, ObjectiveDirection.Minimize };
        var comparer = MultiObjective.WeightedSum(directions, weights: null).TotalOrderComparer;

        comparer.Compare(Vector(double.NaN, 1.0), Vector(1.0, 1.0)).ShouldBeGreaterThan(0);
        comparer.Compare(Vector(1.0, 1.0), Vector(double.NaN, 1.0)).ShouldBeLessThan(0);
    }

    /// <summary>
    /// A zero weight excludes its objective, so an infinite value there must not affect the ranking. Multiplying it
    /// out would make the whole sum NaN and rank the vector last.
    /// </summary>
    [Fact]
    public void TheWeightedSumComparer_IgnoresAnExcludedObjectiveEvenWhenItIsInfinite()
    {
        var directions = new[] { ObjectiveDirection.Minimize, ObjectiveDirection.Minimize };
        var comparer = MultiObjective.WeightedSum(directions, [1.0, 0.0]).TotalOrderComparer;

        comparer.Compare(Vector(1.0, double.PositiveInfinity), Vector(2.0, 0.0)).ShouldBeLessThan(0);
        comparer.Compare(Vector(1.0, double.NaN), Vector(2.0, 0.0)).ShouldBeLessThan(0);
        comparer.Compare(Vector(1.0, double.PositiveInfinity), Vector(1.0, 0.0)).ShouldBe(0);
    }

    /// <summary>
    /// A weighted objective that is genuinely NaN still ranks its vector last; only an excluded objective is ignored.
    /// </summary>
    [Fact]
    public void TheWeightedSumComparer_StillRanksAWeightedNaNLast()
    {
        var directions = new[] { ObjectiveDirection.Minimize, ObjectiveDirection.Minimize };
        var comparer = MultiObjective.WeightedSum(directions, [1.0, 1.0]).TotalOrderComparer;

        comparer.Compare(Vector(1.0, double.NaN), Vector(2.0, 0.0)).ShouldBeGreaterThan(0);
    }

    private static ObjectiveVector Vector(params double[] values) => new(values);
}
