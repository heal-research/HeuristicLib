using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Encodings.Vectors;

namespace HEAL.HeuristicLib.Tests.Genotypes.Vectors;

public sealed class RealVectorTests
{
    [Fact]
    public void Create_FromArray_CopiesElements()
    {
        var elements = new[] { 1.0, 2.0, 3.0 };

        var vector = RealVector.Create(elements);
        elements[0] = 99.0;

        vector.ToArray().ShouldBe(new[] { 1.0, 2.0, 3.0 });
    }

    [Fact]
    public void Create_FromEnumerable_CopiesElements()
    {
        var elements = new List<double>
        {
            1.0,
            2.0,
            3.0
        };

        var vector = RealVector.Create(elements);
        elements[0] = 99.0;

        vector.ToArray().ShouldBe(new[] { 1.0, 2.0, 3.0 });
    }

    [Fact]
    public void FromOwnedArray_UsesProvidedArray()
    {
        var elements = new[] { 1.0, 2.0, 3.0 };

        var vector = RealVector.FromOwnedArray(elements);
        elements[0] = 99.0;

        vector.ToArray().ShouldBe(new[] { 99.0, 2.0, 3.0 });
    }

    [Fact]
    public void All_ReturnsWhetherEveryElementMatchesAndStopsAtFirstFailure()
    {
        var vector = RealVector.Create(1.0, 2.0, -1.0, 4.0);
        var visited = 0;

        var result = vector.All(value =>
        {
            visited++;
            return value >= 0;
        });

        result.ShouldBeFalse();
        visited.ShouldBe(3);
    }

    [Fact]
    public void All_EmptyVector_ReturnsTrue()
    {
        RealVector.Create().All(static _ => false).ShouldBeTrue();
    }

    [Fact]
    public void CreateUniform_CollapsesEqualAndReversedBoundsPerCoordinate()
    {
        var random = new StubRandomNumberGenerator([0.5]);

        RealVector low = RealVector.Create(10, 20, 30);
        RealVector high = RealVector.Create(10, 15, 32);

        var result = RealVector.CreateUniform(3, low, high, random);

        result.ShouldBe(RealVector.Create(10, 20, 31));
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        RealVector v = RealVector.Create(1.0, 2.0, 3.0);

        v.Equals(v).ShouldBeTrue();
#pragma warning disable CS1718
        (v == v).ShouldBeTrue();
        (v != v).ShouldBeFalse();
#pragma warning restore CS1718
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        RealVector v = RealVector.Create(1.0, 2.0, 3.0);

        v.Equals(null).ShouldBeFalse();
        v.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameElements_ReturnsTrue()
    {
        RealVector a = RealVector.Create(1.0, 2.0, 3.0);
        RealVector b = RealVector.Create(1.0, 2.0, 3.0);

        a.Equals(b).ShouldBeTrue();
        b.Equals(a).ShouldBeTrue();
        a.Equals((object)b).ShouldBeTrue();
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentLengths_ReturnsFalse()
    {
        RealVector a = RealVector.Create(1.0, 2.0, 3.0);
        RealVector b = RealVector.Create(1.0, 2.0);

        a.Equals(b).ShouldBeFalse();
        b.Equals(a).ShouldBeFalse();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentElements_ReturnsFalse()
    {
        RealVector a = RealVector.Create(1.0, 2.0, 3.0);
        RealVector b = RealVector.Create(1.0, 2.0, 4.0);

        a.Equals(b).ShouldBeFalse();
        b.Equals(a).ShouldBeFalse();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_IsTransitive()
    {
        RealVector a = RealVector.Create(1.0, 2.0, 3.0);
        RealVector b = RealVector.Create(1.0, 2.0, 3.0);
        RealVector c = RealVector.Create(1.0, 2.0, 3.0);

        a.Equals(b).ShouldBeTrue();
        b.Equals(c).ShouldBeTrue();
        a.Equals(c).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_EqualVectors_HaveSameHashCode()
    {
        RealVector a = RealVector.Create(1.0, 2.0, 3.0);
        RealVector b = RealVector.Create(1.0, 2.0, 3.0);

        b.GetHashCode().ShouldBe(a.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameInstance_IsStable()
    {
        RealVector v = RealVector.Create(1.0, 2.0, 3.0);

        var h1 = v.GetHashCode();
        var h2 = v.GetHashCode();

        h2.ShouldBe(h1);
    }

    [Fact]
    public void HashSet_ContainsEquivalentVector()
    {
        var set = new HashSet<RealVector>();
        RealVector a = RealVector.Create(1.0, 2.0, 3.0);
        RealVector b = RealVector.Create(1.0, 2.0, 3.0);

        set.Add(a);

        set.ShouldContain(b);
    }

    [Fact]
    public void HashSet_AddEquivalentVector_DoesNotIncreaseCount()
    {
        var set = new HashSet<RealVector>();
        RealVector a = RealVector.Create(1.0, 2.0, 3.0);
        RealVector b = RealVector.Create(1.0, 2.0, 3.0);

        set.Add(a);
        set.Add(b);

        set.ShouldHaveSingleItem();
    }

    [Fact]
    public void Dictionary_CanUseEquivalentVectorAsKey()
    {
        var dict = new Dictionary<RealVector, string>();
        RealVector key1 = RealVector.Create(1.0, 2.0, 3.0);
        RealVector key2 = RealVector.Create(1.0, 2.0, 3.0);

        dict[key1] = "value";

        dict.ContainsKey(key2).ShouldBeTrue();
        dict[key2].ShouldBe("value");
    }

    [Fact]
    public void Count_Indexer_AndEnumeration_WorkCorrectly()
    {
        RealVector v = RealVector.Create(1.5, 2.5, 3.5);

        v.Count.ShouldBe(3);
        v[0].ShouldBe(1.5);
        v[1].ShouldBe(2.5);
        v[2].ShouldBe(3.5);
        v.ToArray().ShouldBe(new[] { 1.5, 2.5, 3.5 });
    }

    [Fact]
    public void Indexer_WithIndexFromEnd_WorksCorrectly()
    {
        RealVector v = RealVector.Create(10.0, 20.0, 30.0);

        v[^1].ShouldBe(30.0);
        v[^2].ShouldBe(20.0);
        v[^3].ShouldBe(10.0);
    }

    [Fact]
    public void Contains_UsesElementEquality()
    {
        RealVector v = RealVector.Create(1.0, 2.0, 3.0);

        v.Contains(2.0).ShouldBeTrue();
        v.Contains(4.0).ShouldBeFalse();
    }

    [Fact]
    public void Add_SameLength_AddsElementwise()
    {
        RealVector a = RealVector.Create(1.0, 2.0, 3.0);
        RealVector b = RealVector.Create(10.0, 20.0, 30.0);

        var result = RealVector.Add(a, b);

        result.ToArray().ShouldBe(new[] { 11.0, 22.0, 33.0 });
    }

    [Fact]
    public void Add_BroadcastsScalarLeft()
    {
        RealVector scalar = 2.0;
        RealVector vector = RealVector.Create(10.0, 20.0, 30.0);

        var result = RealVector.Add(scalar, vector);

        result.ToArray().ShouldBe(new[] { 12.0, 22.0, 32.0 });
    }

    [Fact]
    public void Add_BroadcastsScalarRight()
    {
        RealVector vector = RealVector.Create(10.0, 20.0, 30.0);
        RealVector scalar = 2.0;

        var result = RealVector.Add(vector, scalar);

        result.ToArray().ShouldBe(new[] { 12.0, 22.0, 32.0 });
    }

    [Fact]
    public void Add_IncompatibleLengths_Throws()
    {
        RealVector a = RealVector.Create(1.0, 2.0);
        RealVector b = RealVector.Create(10.0, 20.0, 30.0);

        Should.Throw<ArgumentException>(() => RealVector.Add(a, b));
    }

    [Fact]
    public void Subtract_SameLength_SubtractsElementwise()
    {
        RealVector a = RealVector.Create(10.0, 20.0, 30.0);
        RealVector b = RealVector.Create(1.0, 2.0, 3.0);

        var result = RealVector.Subtract(a, b);

        result.ToArray().ShouldBe(new[] { 9.0, 18.0, 27.0 });
    }

    [Fact]
    public void Multiply_SameLength_MultipliesElementwise()
    {
        RealVector a = RealVector.Create(2.0, 3.0, 4.0);
        RealVector b = RealVector.Create(10.0, 20.0, 30.0);

        var result = RealVector.Multiply(a, b);

        result.ToArray().ShouldBe(new[] { 20.0, 60.0, 120.0 });
    }

    [Fact]
    public void Divide_SameLength_DividesElementwise()
    {
        RealVector a = RealVector.Create(10.0, 20.0, 30.0);
        RealVector b = RealVector.Create(2.0, 4.0, 5.0);

        var result = RealVector.Divide(a, b);

        result.ToArray().ShouldBe(new[] { 5.0, 5.0, 6.0 });
    }

    [Fact]
    public void Operators_DelegateToArithmeticMethods()
    {
        RealVector a = RealVector.Create(10.0, 20.0, 30.0);
        RealVector b = RealVector.Create(2.0, 4.0, 5.0);

        (a + b).ToArray().ShouldBe(new[] { 12.0, 24.0, 35.0 });
        (a - b).ToArray().ShouldBe(new[] { 8.0, 16.0, 25.0 });
        (a * b).ToArray().ShouldBe(new[] { 20.0, 80.0, 150.0 });
        (a / b).ToArray().ShouldBe(new[] { 5.0, 5.0, 6.0 });
    }

    [Fact]
    public void AreBroadcastable_ReturnsTrue_ForSameLength()
    {
        RealVector a = RealVector.Create(1.0, 2.0);
        RealVector b = RealVector.Create(3.0, 4.0);

        Vector.AreBroadcastable(a, b).ShouldBeTrue();
    }

    [Fact]
    public void AreBroadcastable_ReturnsTrue_WhenOneIsScalar()
    {
        RealVector scalar = 1.0;
        RealVector vector = RealVector.Create(3.0, 4.0);

        Vector.AreBroadcastable(scalar, vector).ShouldBeTrue();
        Vector.AreBroadcastable(vector, scalar).ShouldBeTrue();
    }

    [Fact]
    public void AreBroadcastable_ReturnsFalse_ForDifferentNonScalarLengths()
    {
        RealVector a = RealVector.Create(1.0, 2.0);
        RealVector b = RealVector.Create(3.0, 4.0, 5.0);

        Vector.AreBroadcastable(a, b).ShouldBeFalse();
    }

    [Fact]
    public void BroadcastLength_ReturnsNonScalarLength()
    {
        RealVector scalar = 1.0;
        RealVector vector = RealVector.Create(3.0, 4.0, 5.0);

        Vector.BroadcastLength(scalar, vector).ShouldBe(3);
        Vector.BroadcastLength(vector, scalar).ShouldBe(3);
    }

    [Fact]
    public void BroadcastLength_ScalarAndEmptyVector_ReturnsZero()
    {
        RealVector scalar = 1.0;
        var empty = RealVector.Create();

        Vector.BroadcastLength(scalar, empty).ShouldBe(0);
        Vector.BroadcastLength(empty, scalar).ShouldBe(0);
        (scalar + empty).ShouldBeEmpty();
        (empty + scalar).ShouldBeEmpty();
    }

    [Fact]
    public void Clamp_BothBoundsNull_ReturnsSameInstance()
    {
        RealVector input = RealVector.Create(1.0, 2.0, 3.0);

        var result = RealVector.Clamp(input, null, null);

        result.ShouldBeSameAs(input);
    }

    [Fact]
    public void Clamp_EmptyInput_AcceptsEmptyAndScalarBounds()
    {
        var input = RealVector.Create();

        var result = RealVector.Clamp(input, RealVector.Create(), 1.0);

        result.ShouldBeSameAs(input);
    }

    [Fact]
    public void Clamp_NonEmptyInput_RejectsEmptyBound()
    {
        var input = RealVector.Create(1.0, 2.0);

        Should.Throw<ArgumentException>(() => RealVector.Clamp(input, RealVector.Create(), 3.0));
    }

    [Fact]
    public void Clamp_NoValueNeedsClamping_ReturnsSameInstance()
    {
        RealVector input = RealVector.Create(1.0, 2.0, 3.0);
        RealVector min = 0.0;
        RealVector max = 5.0;

        var result = RealVector.Clamp(input, min, max);

        result.ShouldBeSameAs(input);
    }

    [Fact]
    public void Clamp_ScalarBounds_ClampsValues()
    {
        RealVector input = RealVector.Create(-1.0, 2.0, 10.0);
        RealVector min = 0.0;
        RealVector max = 5.0;

        var result = RealVector.Clamp(input, min, max);

        result.ToArray().ShouldBe(new[] { 0.0, 2.0, 5.0 });
        result.ShouldNotBeSameAs(input);
    }

    [Fact]
    public void Clamp_VectorBounds_ClampsValues()
    {
        RealVector input = RealVector.Create(-1.0, 2.0, 10.0);
        RealVector min = RealVector.Create(0.0, 1.0, 2.0);
        RealVector max = RealVector.Create(5.0, 3.0, 8.0);

        var result = RealVector.Clamp(input, min, max);

        result.ToArray().ShouldBe(new[] { 0.0, 2.0, 8.0 });
    }

    [Fact]
    public void Clamp_MinLengthMismatch_Throws()
    {
        RealVector input = RealVector.Create(1.0, 2.0, 3.0);
        RealVector min = RealVector.Create(0.0, 1.0);
        RealVector max = 5.0;

        Should.Throw<ArgumentException>(() => RealVector.Clamp(input, min, max));
    }

    [Fact]
    public void Clamp_MaxLengthMismatch_Throws()
    {
        RealVector input = RealVector.Create(1.0, 2.0, 3.0);
        RealVector min = 0.0;
        RealVector max = RealVector.Create(5.0, 6.0);

        Should.Throw<ArgumentException>(() => RealVector.Clamp(input, min, max));
    }

    [Fact]
    public void Clamp_InstanceMethod_DelegatesToStaticBehavior()
    {
        RealVector input = RealVector.Create(-1.0, 2.0, 10.0);
        RealVector min = 0.0;
        RealVector max = 5.0;

        var result = input.Clamp(min, max);

        result.ToArray().ShouldBe(new[] { 0.0, 2.0, 5.0 });
    }

    [Fact]
    public void ClampAt_UsesDimensionBounds()
    {
        RealVector input = RealVector.Create(-1.0, 2.0, 10.0);
        RealVector min = RealVector.Create(0.0, 1.0, 2.0);
        RealVector max = RealVector.Create(5.0, 3.0, 8.0);

        input.ClampAt(min, max, 0).ShouldBe(0.0);
        input.ClampAt(min, max, 1).ShouldBe(2.0);
        input.ClampAt(min, max, 2).ShouldBe(8.0);
    }

    [Fact]
    public void FloorCeilRound_InstanceMethods_ReturnExpectedValues()
    {
        RealVector input = RealVector.Create(1.2, -1.8, 0.5);

        input.Floor().ToArray().ShouldBe(new[] { 1.0, -2.0, 0.0 });
        input.Ceil().ToArray().ShouldBe(new[] { 2.0, -1.0, 1.0 });
        input.Round().ToArray().ShouldBe(new[] { 1.0, -2.0, 1.0 });
    }

    [Fact]
    public void FloorCeilRoundAt_InstanceMethods_ReturnExpectedValues()
    {
        RealVector input = RealVector.Create(1.2, -1.8, 0.5);

        input.FloorAt(0).ShouldBe(1.0);
        input.CeilAt(1).ShouldBe(-1.0);
        input.RoundAt(2).ShouldBe(1.0);
    }

    [Fact]
    public void RoundToIntegerVector_InstanceMethod_ConvertsWithBounds()
    {
        RealVector input = RealVector.Create(1.6, -2.8, 0.2);
        IntegerVector min = -2;
        IntegerVector max = 2;

        var result = input.RoundToIntegerVector(min, max);

        result.ToArray().ShouldBe(new[] { 2, -2, 0 });
    }

    [Fact]
    public void ScalarToIntegerConversions_RespectBounds()
    {
        RealVector.RoundToInteger(1.6, -2, 2).ShouldBe(2);
        RealVector.FloorToInteger(1.6, -2, 2).ShouldBe(1);
        RealVector.CeilToInteger(-1.8, -2, 2).ShouldBe(-1);
    }

    [Theory]
    [InlineData(-10.0, 0)]
    [InlineData(0.0, 0)]
    [InlineData(0.1, 0)]
    [InlineData(1.9, 1)]
    [InlineData(2.0, 2)]
    [InlineData(3.9, 3)]
    [InlineData(4.0, 4)]
    [InlineData(10.0, 10)]
    [InlineData(99.0, 10)]
    public void FloorToInteger_ClampsAndFloorsWithinBounds(double value, int expected)
    {
        RealVector.FloorToInteger(value, 0, 10).ShouldBe(expected);
    }

    [Theory]
    [InlineData(-10.0, 0)]
    [InlineData(0.0, 0)]
    [InlineData(0.1, 1)]
    [InlineData(1.9, 2)]
    [InlineData(2.0, 2)]
    [InlineData(2.1, 3)]
    [InlineData(9.9, 10)]
    [InlineData(10.0, 10)]
    [InlineData(99.0, 10)]
    public void CeilToInteger_ClampsAndCeilsWithinBounds(double value, int expected)
    {
        RealVector.CeilToInteger(value, 0, 10).ShouldBe(expected);
    }

    [Theory]
    [InlineData(-10.0, 0)]
    [InlineData(0.0, 0)]
    [InlineData(0.49, 0)]
    [InlineData(0.5, 1)]
    [InlineData(1.49, 1)]
    [InlineData(1.5, 2)]
    [InlineData(9.49, 9)]
    [InlineData(9.5, 10)]
    [InlineData(10.0, 10)]
    [InlineData(99.0, 10)]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 10)]
    [InlineData(double.NegativeInfinity, 0)]
    public void RoundToInteger_ClampsAndRoundsWithinBounds(double value, int expected)
    {
        RealVector.RoundToInteger(value, 0, 10).ShouldBe(expected);
    }

    [Theory]
    [InlineData(-5, 10, 0)]
    [InlineData(5, 10, 5)]
    [InlineData(-10, -5, -5)]
    public void RoundToInteger_ClampsNaNReplacementToBounds(int minimum, int maximum, int expected)
    {
        RealVector.RoundToInteger(double.NaN, minimum, maximum).ShouldBe(expected);
    }

    [Fact]
    public void ScalarToIntegerConversions_TolerateBoundaryNoise()
    {
        RealVector.FloorToInteger(1.0 - 1e-13, -2, 2).ShouldBe(1);
        RealVector.CeilToInteger(1.0 + 1e-13, -2, 2).ShouldBe(1);
        RealVector.CeilToInteger(1.0 + 1e-10, -2, 2).ShouldBe(2);
        RealVector.FloorToInteger(1.0 - 1e-10, -2, 2).ShouldBe(0);
    }

    [Fact]
    public void RoundToIntegerAt_UsesDimensionBounds()
    {
        IntegerVector min = IntegerVector.Create(-2, -1, -5);
        IntegerVector max = IntegerVector.Create(2, 3, 0);

        RealVector.RoundToIntegerAt(1.6, min, max, 0).ShouldBe(2);
        RealVector.RoundToIntegerAt(-2.8, min, max, 1).ShouldBe(-1);
        RealVector.RoundToIntegerAt(0.2, min, max, 2).ShouldBe(0);
    }

    [Fact]
    public void FloorAndCeilToIntegerAt_UseDimensionBounds()
    {
        IntegerVector min = IntegerVector.Create(0, 10, 100);
        IntegerVector max = IntegerVector.Create(10, 20, 110);

        RealVector.FloorToIntegerAt(1.9, min, max, 0).ShouldBe(1);
        RealVector.FloorToIntegerAt(17.6, min, max, 1).ShouldBe(17);
        RealVector.FloorToIntegerAt(108.4, min, max, 2).ShouldBe(108);

        RealVector.CeilToIntegerAt(1.9, min, max, 0).ShouldBe(2);
        RealVector.CeilToIntegerAt(17.6, min, max, 1).ShouldBe(18);
        RealVector.CeilToIntegerAt(108.4, min, max, 2).ShouldBe(109);
    }

    [Fact]
    public void RoundToIntegerVector_UsesPerDimensionBounds()
    {
        RealVector input = RealVector.Create(3.1, 17.6, 108.4);
        IntegerVector min = IntegerVector.Create(0, 10, 100);
        IntegerVector max = IntegerVector.Create(10, 20, 110);

        var rounded = RealVector.RoundToIntegerVector(input, min, max);

        rounded.ToArray().ShouldBe(new[] { 3, 18, 108 });
    }

    [Fact]
    public void ComparisonOperators_SameLength_WorkElementwise()
    {
        RealVector a = RealVector.Create(1.0, 5.0, 3.0);
        RealVector b = RealVector.Create(2.0, 5.0, 1.0);

        (a > b).ToArray().ShouldBe(new[] { false, false, true });
        (a < b).ToArray().ShouldBe(new[] { true, false, false });
        (a >= b).ToArray().ShouldBe(new[] { false, true, true });
        (a <= b).ToArray().ShouldBe(new[] { true, true, false });
    }

    [Fact]
    public void ComparisonOperators_BroadcastScalar_WorkElementwise()
    {
        RealVector a = RealVector.Create(1.0, 5.0, 3.0);
        RealVector scalar = 3.0;

        (a > scalar).ToArray().ShouldBe(new[] { false, true, false });
        (a < scalar).ToArray().ShouldBe(new[] { true, false, false });
        (a >= scalar).ToArray().ShouldBe(new[] { false, true, true });
        (a <= scalar).ToArray().ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void ComparisonOperators_IncompatibleLengths_Throw()
    {
        RealVector a = RealVector.Create(1.0, 2.0);
        RealVector b = RealVector.Create(1.0, 2.0, 3.0);

        Should.Throw<ArgumentException>(() => a > b);
        Should.Throw<ArgumentException>(() => a < b);
        Should.Throw<ArgumentException>(() => a >= b);
        Should.Throw<ArgumentException>(() => a <= b);
    }

    [Fact]
    public void Repeat_CreatesVectorWithRepeatedValue()
    {
        var result = RealVector.Repeat(2.5, 4);

        result.ToArray().ShouldBe(new[] { 2.5, 2.5, 2.5, 2.5 });
    }

    [Fact]
    public void Dot_ComputesDotProduct()
    {
        RealVector a = RealVector.Create(1.0, 2.0, 3.0);
        RealVector b = RealVector.Create(4.0, 5.0, 6.0);

        a.Dot(b).ShouldBe(32.0, 1e-12);
    }

    [Fact]
    public void Norm_ComputesEuclideanNorm()
    {
        RealVector v = RealVector.Create(3.0, 4.0);

        v.Norm().ShouldBe(5.0, 1e-12);
    }

    [Fact]
    public void Angle_ComputesAngleBetweenVectors()
    {
        RealVector x = RealVector.Create(1.0, 0.0);
        RealVector y = RealVector.Create(0.0, 1.0);

        x.Angle(y).ShouldBe(Math.PI / 2.0, 1e-12);
    }

    [Fact]
    public void AsIntegerVector_RoundsElements()
    {
        RealVector v = RealVector.Create(1.2, 1.5, 2.6, -1.5);

        var result = v.AsIntegerVector();

        result.ToArray().ShouldBe(new[] { 1, 2, 3, -2 });
    }

    [Fact]
    public void ToString_FormatsElements()
    {
        RealVector v = RealVector.Create(1.0, 2.0, 3.0);

        v.ToString().ShouldBe("[1, 2, 3]");
    }

    [Fact]
    public void ImplicitConversion_FromScalar_CreatesSingleElementVector()
    {
        RealVector v = 42.0;

        v.ShouldHaveSingleItem();
        v[0].ShouldBe(42.0);
    }

    [Fact]
    public void ImplicitConversion_FromArray_CreatesVectorWithArrayValues()
    {
        RealVector v = RealVector.Create(1.0, 2.0, 3.0);

        v.Count.ShouldBe(3);
        v.ToArray().ShouldBe(new[] { 1.0, 2.0, 3.0 });
    }

    [Fact]
    public void Equality_EmptyVectors_AreEqual_AndHaveSameHashCode()
    {
        RealVector a = RealVector.Create();
        RealVector b = RealVector.Create();

        a.Equals(b).ShouldBeTrue();
        b.GetHashCode().ShouldBe(a.GetHashCode());
    }

    [Fact]
    public void Equals_WithNaNElements_FollowsSequenceEqualSemantics()
    {
        RealVector a = RealVector.Create(double.NaN);
        RealVector b = RealVector.Create(double.NaN);

        (a.GetHashCode() == b.GetHashCode() || a.Equals(b)).ShouldBe(a.Equals(b));
        a.Equals(b).ShouldBeTrue(); // current .NET double equality semantics treat NaN.Equals(NaN) as true
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        RealVector? a = null;
        RealVector? b = null;
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_LeftNull_RightNonNull_ReturnsFalse()
    {
        RealVector? a = null;
        RealVector b = RealVector.Create(1.0);
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void CreateNormal_ReturnsVectorOfRequestedLength()
    {
        var rng = new StubRandomNumberGenerator(
            nextDoubleSequence: new[] { 0.5, 0.25, 0.6, 0.75, 0.7, 0.1 });

        RealVector mean = 0.0;
        RealVector std = 1.0;

        var result = RealVector.CreateNormal(3, mean, std, rng);

        result.Count.ShouldBe(3);
    }

    [Fact]
    public void CreateNormal_BroadcastsScalarMeanAndStd()
    {
        var rng = new StubRandomNumberGenerator(
            nextDoubleSequence: new[] { 0.5, 0.25, 0.6, 0.75 });

        RealVector mean = 10.0;
        RealVector std = 2.0;

        var result = RealVector.CreateNormal(2, mean, std, rng);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public void CreateNormal_ThrowsAfter50InvalidAttempts_ForSingleDimension()
    {
        var invalid = Enumerable.Repeat(0.0, 50).ToArray();
        var rng = new StubRandomNumberGenerator(nextDoubleSequence: invalid);

        RealVector mean = 0.0;
        RealVector std = 1.0;

        Should.Throw<InvalidOperationException>(() => RealVector.CreateNormal(1, mean, std, rng));
    }

    [Fact]
    public void Sqrt_ReturnsElementwiseSquareRoot()
    {
        RealVector input = RealVector.Create(0.0, 1.0, 4.0, 9.0);

        var result = RealVector.Sqrt(input);

        result.ToArray().ShouldBe(new[] { 0.0, 1.0, 2.0, 3.0 });
    }

    [Fact]
    public void Sqrt_OfNegativeValue_ReturnsNaNForThatElement()
    {
        RealVector input = RealVector.Create(4.0, -1.0, 9.0);

        var result = RealVector.Sqrt(input).ToArray();

        result[0].ShouldBe(2.0);
        double.IsNaN(result[1]).ShouldBeTrue();
        result[2].ShouldBe(3.0);
    }

    [Fact]
    public void Log_ReturnsElementwiseNaturalLogarithm()
    {
        RealVector input = RealVector.Create(1.0, Math.E, Math.E * Math.E);

        var result = RealVector.Log(input);

        result[0].ShouldBe(0.0, 1e-12);
        result[1].ShouldBe(1.0, 1e-12);
        result[2].ShouldBe(2.0, 1e-12);
    }

    [Fact]
    public void Log_OfZeroAndNegativeValue_FollowsMathLogSemantics()
    {
        RealVector input = RealVector.Create(0.0, -1.0, 1.0);

        var result = RealVector.Log(input).ToArray();

        double.IsNegativeInfinity(result[0]).ShouldBeTrue();
        double.IsNaN(result[1]).ShouldBeTrue();
        result[2].ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void Sin_ReturnsElementwiseSine()
    {
        RealVector input = RealVector.Create(0.0, Math.PI / 2.0, Math.PI);

        var result = RealVector.Sin(input);

        result[0].ShouldBe(0.0, 1e-12);
        result[1].ShouldBe(1.0, 1e-12);
        result[2].ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public void AreBroadcastable_VectorAndEnumerable_ReturnsTrue_WhenAllAreBroadcastable()
    {
        RealVector vector = RealVector.Create(1.0, 2.0, 3.0);
        var others = new[] { RealVector.Create(4.0, 5.0, 6.0), 7.0, RealVector.Create(8.0, 9.0, 10.0) };

        var result = Vector.AreBroadcastable(vector, others);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreBroadcastable_VectorAndEnumerable_ReturnsFalse_WhenAtLeastOneIsNotBroadcastable()
    {
        RealVector vector = RealVector.Create(1.0, 2.0, 3.0);
        var others = new[] { RealVector.Create(4.0, 5.0, 6.0), RealVector.Create(7.0, 8.0) };

        var result = Vector.AreBroadcastable(vector, others);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreBroadcastable_VectorAndEnumerable_ReturnsFalse_WhenScalarPrecedesDifferentLengths()
    {
        RealVector scalar = 1.0;
        var others = new[] { RealVector.Create(1.0, 2.0), RealVector.Create(3.0, 4.0, 5.0) };

        Vector.AreBroadcastable(scalar, others).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => Vector.BroadcastLength(scalar, others));
    }

    [Fact]
    public void AreBroadcastable_VectorAndEmptyEnumerable_ReturnsTrue()
    {
        RealVector vector = RealVector.Create(1.0, 2.0, 3.0);
        var others = Array.Empty<RealVector>();

        var result = Vector.AreBroadcastable(vector, others);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreBroadcastableTo_ReturnsTrue_WhenAllMatchLengthOrAreScalar()
    {
        var vectors = new[] { RealVector.Create(1.0, 2.0, 3.0), 4.0, RealVector.Create(5.0, 6.0, 7.0) };

        var result = Vector.AreBroadcastableTo(3, vectors);

        result.ShouldBeTrue();
    }

    [Fact]
    public void AreBroadcastableTo_ReturnsFalse_WhenAtLeastOneIsIncompatible()
    {
        var vectors = new[] { RealVector.Create(1.0, 2.0, 3.0), RealVector.Create(4.0, 5.0) };

        var result = Vector.AreBroadcastableTo(3, vectors);

        result.ShouldBeFalse();
    }

    [Fact]
    public void AreBroadcastableTo_AcceptsScalarsAndMatchingLengths()
    {
        Vector.AreBroadcastableTo(3, RealVector.Create(1), RealVector.Create(1, 2, 3)).ShouldBeTrue();
        Vector.AreBroadcastableTo(3, RealVector.Create(1, 2)).ShouldBeFalse();
    }

    [Fact]
    public void BroadcastLength_VectorAndEnumerable_ReturnsVectorLength_WhenOthersAreScalar()
    {
        RealVector vector = RealVector.Create(1.0, 2.0, 3.0);
        var others = new[] { 4.0, (RealVector)5.0 };

        var result = Vector.BroadcastLength(vector, others);

        result.ShouldBe(3);
    }

    [Fact]
    public void BroadcastLength_VectorAndEnumerable_ReturnsMaximumCompatibleLength()
    {
        RealVector vector = 1.0;
        var others = new[] { RealVector.Create(1.0, 2.0, 3.0, 4.0), 2.0, RealVector.Create(5.0, 6.0, 7.0, 8.0) };

        var result = Vector.BroadcastLength(vector, others);

        result.ShouldBe(4);
    }

    [Fact]
    public void BroadcastLength_VectorAndEnumerable_WithScalarAndEmptyVector_ReturnsZero()
    {
        RealVector scalar = 1.0;
        var others = new[] { RealVector.Create(), (RealVector)2.0 };

        Vector.BroadcastLength(scalar, others).ShouldBe(0);
    }

    [Fact]
    public void BroadcastLength_VectorAndEnumerable_WithEmptyEnumerable_ReturnsVectorLength()
    {
        RealVector vector = RealVector.Create(1.0, 2.0, 3.0);
        var others = Array.Empty<RealVector>();

        var result = Vector.BroadcastLength(vector, others);

        result.ShouldBe(3);
    }

    [Fact]
    public void BroadcastLength_VectorAndEnumerable_Throws_WhenIncompatible()
    {
        RealVector vector = RealVector.Create(1.0, 2.0, 3.0);
        var others = new[] { RealVector.Create(4.0, 5.0) };

        Should.Throw<ArgumentException>(() => Vector.BroadcastLength(vector, others));
    }

    private sealed class StubRandomNumberGenerator : IRandomNumberGenerator
    {
        private readonly Queue<double> doubles;

        public StubRandomNumberGenerator(IEnumerable<double> nextDoubleSequence)
        {
            doubles = new Queue<double>(nextDoubleSequence);
        }

        public double NextDouble()
        {
            if (doubles.Count == 0)
            {
                throw new InvalidOperationException("No more test doubles available.");
            }

            return doubles.Dequeue();
        }

        public int NextInt() => throw new NotSupportedException();

        public IRandomNumberGenerator Fork(ulong forkKey) => throw new NotSupportedException();

        // Add the remaining interface members as needed for your codebase,
        // typically throwing NotSupportedException if the tests do not use them.
    }
}
