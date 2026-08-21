namespace HEAL.HeuristicLib.Tests.Optimization;

public class ObjectiveVectorComparisonTests
{
    [Fact]
    public void DefaultDominanceRelation_IsIncomparable()
    {
        default(DominanceRelation).ShouldBe(DominanceRelation.Incomparable);
    }

    [Fact]
    public void AVector_IsEqualToItselfRatherThanDominatingIt()
    {
        var vector = Vector(1.0, 2.0);

        vector.CompareTo(vector, MultiObjective.Minimize(2)).ShouldBe(DominanceRelation.Equal);
        vector.Dominates(vector, MultiObjective.Minimize(2)).ShouldBeFalse();
    }

    [Fact]
    public void AVectorCarryingNaN_IsEqualToItself()
    {
        var vector = Vector(double.NaN, 1.0);

        vector.CompareTo(vector, MultiObjective.Minimize(2)).ShouldBe(DominanceRelation.Equal);
        vector.CompareTo(Vector(double.NaN, 1.0), MultiObjective.Minimize(2)).ShouldBe(DominanceRelation.Equal);
    }

    [Fact]
    public void ComparingAVectorWithItself_StillValidatesTheDirections()
    {
        var vector = Vector(1.0, 2.0);

        Should.Throw<ArgumentException>(() => vector.CompareTo(vector, MultiObjective.Minimize(3)));
    }

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

    [Theory]
    [InlineData(ObjectiveDirection.Minimize)]
    [InlineData(ObjectiveDirection.Maximize)]
    public void NaN_IsWorseThanTheWorstOrderedValue(ObjectiveDirection direction)
    {
        ObjectiveValue.Compare(double.NaN, ObjectiveValue.WorstValue(direction).Value, direction).ShouldBeGreaterThan(0);
        ObjectiveValue.Compare(double.NaN, double.PositiveInfinity, direction).ShouldBeGreaterThan(0);
        ObjectiveValue.Compare(double.NaN, double.NegativeInfinity, direction).ShouldBeGreaterThan(0);
    }

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

        ObjectiveValue.Compare(best, best, direction).ShouldBe(0);
        ObjectiveValue.Compare(worst, worst, direction).ShouldBe(0);
    }

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

    [Fact]
    public void TheWeightedSumComparer_IgnoresAnExcludedObjectiveEvenWhenItIsInfinite()
    {
        var directions = new[] { ObjectiveDirection.Minimize, ObjectiveDirection.Minimize };
        var comparer = MultiObjective.WeightedSum(directions, [1.0, 0.0]).TotalOrderComparer;

        comparer.Compare(Vector(1.0, double.PositiveInfinity), Vector(2.0, 0.0)).ShouldBeLessThan(0);
        comparer.Compare(Vector(1.0, double.NaN), Vector(2.0, 0.0)).ShouldBeLessThan(0);
        comparer.Compare(Vector(1.0, double.PositiveInfinity), Vector(1.0, 0.0)).ShouldBe(0);
    }

    [Fact]
    public void TheWeightedSumComparer_StillRanksAWeightedNaNLast()
    {
        var directions = new[] { ObjectiveDirection.Minimize, ObjectiveDirection.Minimize };
        var comparer = MultiObjective.WeightedSum(directions, [1.0, 1.0]).TotalOrderComparer;

        comparer.Compare(Vector(1.0, double.NaN), Vector(2.0, 0.0)).ShouldBeGreaterThan(0);
    }

    private static ObjectiveVector Vector(params double[] values) => new(values);
}
