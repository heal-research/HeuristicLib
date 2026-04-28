using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Genotypes.Vectors;

public sealed class RealVectorTests
{
  [Fact]
  public void Equals_SameReference_ReturnsTrue()
  {
    RealVector v = new[] { 1.0, 2.0, 3.0 };

    Assert.True(v.Equals(v));
    Assert.True(v == v);
    Assert.False(v != v);
  }

  [Fact]
  public void Equals_Null_ReturnsFalse()
  {
    RealVector v = new[] { 1.0, 2.0, 3.0 };

    Assert.False(v.Equals(null));
    Assert.False(v.Equals((object?)null));
  }

  [Fact]
  public void Equals_ObjectOfDifferentType_ReturnsFalse()
  {
    RealVector v = new[] { 1.0, 2.0, 3.0 };

    Assert.False(v.Equals("not a vector"));
  }

  [Fact]
  public void Equals_SameElements_ReturnsTrue()
  {
    RealVector a = new[] { 1.0, 2.0, 3.0 };
    RealVector b = new[] { 1.0, 2.0, 3.0 };

    Assert.True(a.Equals(b));
    Assert.True(b.Equals(a));
    Assert.True(a.Equals((object)b));
    Assert.True(a == b);
    Assert.False(a != b);
  }

  [Fact]
  public void Equals_DifferentLengths_ReturnsFalse()
  {
    RealVector a = new[] { 1.0, 2.0, 3.0 };
    RealVector b = new[] { 1.0, 2.0 };

    Assert.False(a.Equals(b));
    Assert.False(b.Equals(a));
    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void Equals_DifferentElements_ReturnsFalse()
  {
    RealVector a = new[] { 1.0, 2.0, 3.0 };
    RealVector b = new[] { 1.0, 2.0, 4.0 };

    Assert.False(a.Equals(b));
    Assert.False(b.Equals(a));
    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void Equals_IsTransitive()
  {
    RealVector a = new[] { 1.0, 2.0, 3.0 };
    RealVector b = new[] { 1.0, 2.0, 3.0 };
    RealVector c = new[] { 1.0, 2.0, 3.0 };

    Assert.True(a.Equals(b));
    Assert.True(b.Equals(c));
    Assert.True(a.Equals(c));
  }

  [Fact]
  public void GetHashCode_EqualVectors_HaveSameHashCode()
  {
    RealVector a = new[] { 1.0, 2.0, 3.0 };
    RealVector b = new[] { 1.0, 2.0, 3.0 };

    Assert.Equal(a.GetHashCode(), b.GetHashCode());
  }

  [Fact]
  public void GetHashCode_SameInstance_IsStable()
  {
    RealVector v = new[] { 1.0, 2.0, 3.0 };

    var h1 = v.GetHashCode();
    var h2 = v.GetHashCode();

    Assert.Equal(h1, h2);
  }

  [Fact]
  public void HashSet_ContainsEquivalentVector()
  {
    var set = new HashSet<RealVector>();
    RealVector a = new[] { 1.0, 2.0, 3.0 };
    RealVector b = new[] { 1.0, 2.0, 3.0 };

    set.Add(a);

    Assert.Contains(b, set);
  }

  [Fact]
  public void HashSet_AddEquivalentVector_DoesNotIncreaseCount()
  {
    var set = new HashSet<RealVector>();
    RealVector a = new[] { 1.0, 2.0, 3.0 };
    RealVector b = new[] { 1.0, 2.0, 3.0 };

    set.Add(a);
    set.Add(b);

    Assert.Single(set);
  }

  [Fact]
  public void Dictionary_CanUseEquivalentVectorAsKey()
  {
    var dict = new Dictionary<RealVector, string>();
    RealVector key1 = new[] { 1.0, 2.0, 3.0 };
    RealVector key2 = new[] { 1.0, 2.0, 3.0 };

    dict[key1] = "value";

    Assert.True(dict.ContainsKey(key2));
    Assert.Equal("value", dict[key2]);
  }

  [Fact]
  public void Count_Indexer_AndEnumeration_WorkCorrectly()
  {
    RealVector v = new[] { 1.5, 2.5, 3.5 };

    Assert.Equal(3, v.Count);
    Assert.Equal(1.5, v[0]);
    Assert.Equal(2.5, v[1]);
    Assert.Equal(3.5, v[2]);
    Assert.Equal(new[] { 1.5, 2.5, 3.5 }, v.ToArray());
  }

  [Fact]
  public void Indexer_WithIndexFromEnd_WorksCorrectly()
  {
    RealVector v = new[] { 10.0, 20.0, 30.0 };

    Assert.Equal(30.0, v[^1]);
    Assert.Equal(20.0, v[^2]);
    Assert.Equal(10.0, v[^3]);
  }

  [Fact]
  public void Contains_UsesElementEquality()
  {
    RealVector v = new[] { 1.0, 2.0, 3.0 };

    Assert.True(v.Contains(2.0));
    Assert.False(v.Contains(4.0));
  }

  [Fact]
  public void Add_SameLength_AddsElementwise()
  {
    RealVector a = new[] { 1.0, 2.0, 3.0 };
    RealVector b = new[] { 10.0, 20.0, 30.0 };

    var result = RealVector.Add(a, b);

    Assert.Equal(new[] { 11.0, 22.0, 33.0 }, result.ToArray());
  }

  [Fact]
  public void Add_BroadcastsScalarLeft()
  {
    RealVector scalar = 2.0;
    RealVector vector = new[] { 10.0, 20.0, 30.0 };

    var result = RealVector.Add(scalar, vector);

    Assert.Equal(new[] { 12.0, 22.0, 32.0 }, result.ToArray());
  }

  [Fact]
  public void Add_BroadcastsScalarRight()
  {
    RealVector vector = new[] { 10.0, 20.0, 30.0 };
    RealVector scalar = 2.0;

    var result = RealVector.Add(vector, scalar);

    Assert.Equal(new[] { 12.0, 22.0, 32.0 }, result.ToArray());
  }

  [Fact]
  public void Add_IncompatibleLengths_Throws()
  {
    RealVector a = new[] { 1.0, 2.0 };
    RealVector b = new[] { 10.0, 20.0, 30.0 };

    Assert.Throws<ArgumentException>(() => RealVector.Add(a, b));
  }

  [Fact]
  public void Subtract_SameLength_SubtractsElementwise()
  {
    RealVector a = new[] { 10.0, 20.0, 30.0 };
    RealVector b = new[] { 1.0, 2.0, 3.0 };

    var result = RealVector.Subtract(a, b);

    Assert.Equal(new[] { 9.0, 18.0, 27.0 }, result.ToArray());
  }

  [Fact]
  public void Multiply_SameLength_MultipliesElementwise()
  {
    RealVector a = new[] { 2.0, 3.0, 4.0 };
    RealVector b = new[] { 10.0, 20.0, 30.0 };

    var result = RealVector.Multiply(a, b);

    Assert.Equal(new[] { 20.0, 60.0, 120.0 }, result.ToArray());
  }

  [Fact]
  public void Divide_SameLength_DividesElementwise()
  {
    RealVector a = new[] { 10.0, 20.0, 30.0 };
    RealVector b = new[] { 2.0, 4.0, 5.0 };

    var result = RealVector.Divide(a, b);

    Assert.Equal(new[] { 5.0, 5.0, 6.0 }, result.ToArray());
  }

  [Fact]
  public void Operators_DelegateToArithmeticMethods()
  {
    RealVector a = new[] { 10.0, 20.0, 30.0 };
    RealVector b = new[] { 2.0, 4.0, 5.0 };

    Assert.Equal(new[] { 12.0, 24.0, 35.0 }, (a + b).ToArray());
    Assert.Equal(new[] { 8.0, 16.0, 25.0 }, (a - b).ToArray());
    Assert.Equal(new[] { 20.0, 80.0, 150.0 }, (a * b).ToArray());
    Assert.Equal(new[] { 5.0, 5.0, 6.0 }, (a / b).ToArray());
  }

  [Fact]
  public void AreCompatible_ReturnsTrue_ForSameLength()
  {
    RealVector a = new[] { 1.0, 2.0 };
    RealVector b = new[] { 3.0, 4.0 };

    Assert.True(RealVector.AreCompatible(a, b));
  }

  [Fact]
  public void AreCompatible_ReturnsTrue_WhenOneIsScalar()
  {
    RealVector scalar = 1.0;
    RealVector vector = new[] { 3.0, 4.0 };

    Assert.True(RealVector.AreCompatible(scalar, vector));
    Assert.True(RealVector.AreCompatible(vector, scalar));
  }

  [Fact]
  public void AreCompatible_ReturnsFalse_ForDifferentNonScalarLengths()
  {
    RealVector a = new[] { 1.0, 2.0 };
    RealVector b = new[] { 3.0, 4.0, 5.0 };

    Assert.False(RealVector.AreCompatible(a, b));
  }

  [Fact]
  public void BroadcastLength_ReturnsMaxLength()
  {
    RealVector scalar = 1.0;
    RealVector vector = new[] { 3.0, 4.0, 5.0 };

    Assert.Equal(3, RealVector.BroadcastLength(scalar, vector));
    Assert.Equal(3, RealVector.BroadcastLength(vector, scalar));
  }

  [Fact]
  public void Clamp_BothBoundsNull_ReturnsSameInstance()
  {
    RealVector input = new[] { 1.0, 2.0, 3.0 };

    var result = RealVector.Clamp(input, null, null);

    Assert.Same(input, result);
  }

  [Fact]
  public void Clamp_NoValueNeedsClamping_ReturnsSameInstance()
  {
    RealVector input = new[] { 1.0, 2.0, 3.0 };
    RealVector min = 0.0;
    RealVector max = 5.0;

    var result = RealVector.Clamp(input, min, max);

    Assert.Same(input, result);
  }

  [Fact]
  public void Clamp_ScalarBounds_ClampsValues()
  {
    RealVector input = new[] { -1.0, 2.0, 10.0 };
    RealVector min = 0.0;
    RealVector max = 5.0;

    var result = RealVector.Clamp(input, min, max);

    Assert.Equal(new[] { 0.0, 2.0, 5.0 }, result.ToArray());
    Assert.NotSame(input, result);
  }

  [Fact]
  public void Clamp_VectorBounds_ClampsValues()
  {
    RealVector input = new[] { -1.0, 2.0, 10.0 };
    RealVector min = new[] { 0.0, 1.0, 2.0 };
    RealVector max = new[] { 5.0, 3.0, 8.0 };

    var result = RealVector.Clamp(input, min, max);

    Assert.Equal(new[] { 0.0, 2.0, 8.0 }, result.ToArray());
  }

  [Fact]
  public void Clamp_MinLengthMismatch_Throws()
  {
    RealVector input = new[] { 1.0, 2.0, 3.0 };
    RealVector min = new[] { 0.0, 1.0 };
    RealVector max = 5.0;

    Assert.Throws<ArgumentException>(() => RealVector.Clamp(input, min, max));
  }

  [Fact]
  public void Clamp_MaxLengthMismatch_Throws()
  {
    RealVector input = new[] { 1.0, 2.0, 3.0 };
    RealVector min = 0.0;
    RealVector max = new[] { 5.0, 6.0 };

    Assert.Throws<ArgumentException>(() => RealVector.Clamp(input, min, max));
  }

  [Fact]
  public void Clamp_InstanceMethod_DelegatesToStaticBehavior()
  {
    RealVector input = new[] { -1.0, 2.0, 10.0 };
    RealVector min = 0.0;
    RealVector max = 5.0;

    var result = input.Clamp(min, max);

    Assert.Equal(new[] { 0.0, 2.0, 5.0 }, result.ToArray());
  }

  [Fact]
  public void ClampAt_UsesDimensionBounds()
  {
    RealVector input = new[] { -1.0, 2.0, 10.0 };
    RealVector min = new[] { 0.0, 1.0, 2.0 };
    RealVector max = new[] { 5.0, 3.0, 8.0 };

    Assert.Equal(0.0, input.ClampAt(min, max, 0));
    Assert.Equal(2.0, input.ClampAt(min, max, 1));
    Assert.Equal(8.0, input.ClampAt(min, max, 2));
  }

  [Fact]
  public void FloorCeilRound_InstanceMethods_ReturnExpectedValues()
  {
    RealVector input = new[] { 1.2, -1.8, 0.5 };

    Assert.Equal(new[] { 1.0, -2.0, 0.0 }, input.Floor().ToArray());
    Assert.Equal(new[] { 2.0, -1.0, 1.0 }, input.Ceil().ToArray());
    Assert.Equal(new[] { 1.0, -2.0, 1.0 }, input.Round().ToArray());
  }

  [Fact]
  public void FloorCeilRoundAt_InstanceMethods_ReturnExpectedValues()
  {
    RealVector input = new[] { 1.2, -1.8, 0.5 };

    Assert.Equal(1.0, input.FloorAt(0));
    Assert.Equal(-1.0, input.CeilAt(1));
    Assert.Equal(1.0, input.RoundAt(2));
  }

  [Fact]
  public void RoundToIntegerVector_InstanceMethod_ConvertsWithBounds()
  {
    RealVector input = new[] { 1.6, -2.8, 0.2 };
    IntegerVector min = -2;
    IntegerVector max = 2;

    var result = input.RoundToIntegerVector(min, max);

    Assert.Equal(new[] { 2, -2, 0 }, result.ToArray());
  }

  [Fact]
  public void ScalarToIntegerConversions_RespectBounds()
  {
    Assert.Equal(2, RealVector.RoundToInteger(1.6, -2, 2));
    Assert.Equal(1, RealVector.FloorToInteger(1.6, -2, 2));
    Assert.Equal(-1, RealVector.CeilToInteger(-1.8, -2, 2));
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
    Assert.Equal(expected, RealVector.FloorToInteger(value, 0, 10));
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
    Assert.Equal(expected, RealVector.CeilToInteger(value, 0, 10));
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
  public void RoundToInteger_ClampsAndRoundsWithinBounds(double value, int expected)
  {
    Assert.Equal(expected, RealVector.RoundToInteger(value, 0, 10));
  }

  [Fact]
  public void ScalarToIntegerConversions_TolerateBoundaryNoise()
  {
    Assert.Equal(1, RealVector.FloorToInteger(1.0 - 1e-13, -2, 2));
    Assert.Equal(1, RealVector.CeilToInteger(1.0 + 1e-13, -2, 2));
    Assert.Equal(2, RealVector.CeilToInteger(1.0 + 1e-10, -2, 2));
    Assert.Equal(0, RealVector.FloorToInteger(1.0 - 1e-10, -2, 2));
  }

  [Fact]
  public void RoundToIntegerAt_UsesDimensionBounds()
  {
    IntegerVector min = new[] { -2, -1, -5 };
    IntegerVector max = new[] { 2, 3, 0 };

    Assert.Equal(2, RealVector.RoundToIntegerAt(1.6, min, max, 0));
    Assert.Equal(-1, RealVector.RoundToIntegerAt(-2.8, min, max, 1));
    Assert.Equal(0, RealVector.RoundToIntegerAt(0.2, min, max, 2));
  }

  [Fact]
  public void FloorAndCeilToIntegerAt_UseDimensionBounds()
  {
    IntegerVector min = new[] { 0, 10, 100 };
    IntegerVector max = new[] { 10, 20, 110 };

    Assert.Equal(1, RealVector.FloorToIntegerAt(1.9, min, max, 0));
    Assert.Equal(17, RealVector.FloorToIntegerAt(17.6, min, max, 1));
    Assert.Equal(108, RealVector.FloorToIntegerAt(108.4, min, max, 2));

    Assert.Equal(2, RealVector.CeilToIntegerAt(1.9, min, max, 0));
    Assert.Equal(18, RealVector.CeilToIntegerAt(17.6, min, max, 1));
    Assert.Equal(109, RealVector.CeilToIntegerAt(108.4, min, max, 2));
  }

  [Fact]
  public void RoundToIntegerVector_UsesPerDimensionBounds()
  {
    RealVector input = new[] { 3.1, 17.6, 108.4 };
    IntegerVector min = new[] { 0, 10, 100 };
    IntegerVector max = new[] { 10, 20, 110 };

    var rounded = RealVector.RoundToIntegerVector(input, min, max);

    Assert.Equal(new[] { 3, 18, 108 }, rounded.ToArray());
  }

  [Fact]
  public void ComparisonOperators_SameLength_WorkElementwise()
  {
    RealVector a = new[] { 1.0, 5.0, 3.0 };
    RealVector b = new[] { 2.0, 5.0, 1.0 };

    Assert.Equal(new[] { false, false, true }, (a > b).ToArray());
    Assert.Equal(new[] { true, false, false }, (a < b).ToArray());
    Assert.Equal(new[] { false, true, true }, (a >= b).ToArray());
    Assert.Equal(new[] { true, true, false }, (a <= b).ToArray());
  }

  [Fact]
  public void ComparisonOperators_BroadcastScalar_WorkElementwise()
  {
    RealVector a = new[] { 1.0, 5.0, 3.0 };
    RealVector scalar = 3.0;

    Assert.Equal(new[] { false, true, false }, (a > scalar).ToArray());
    Assert.Equal(new[] { true, false, false }, (a < scalar).ToArray());
    Assert.Equal(new[] { false, true, true }, (a >= scalar).ToArray());
    Assert.Equal(new[] { true, false, true }, (a <= scalar).ToArray());
  }

  [Fact]
  public void ComparisonOperators_IncompatibleLengths_Throw()
  {
    RealVector a = new[] { 1.0, 2.0 };
    RealVector b = new[] { 1.0, 2.0, 3.0 };

    Assert.Throws<ArgumentException>(() => a > b);
    Assert.Throws<ArgumentException>(() => a < b);
    Assert.Throws<ArgumentException>(() => a >= b);
    Assert.Throws<ArgumentException>(() => a <= b);
  }

  [Fact]
  public void Repeat_CreatesVectorWithRepeatedValue()
  {
    var result = RealVector.Repeat(2.5, 4);

    Assert.Equal(new[] { 2.5, 2.5, 2.5, 2.5 }, result.ToArray());
  }

  [Fact]
  public void Dot_ComputesDotProduct()
  {
    RealVector a = new[] { 1.0, 2.0, 3.0 };
    RealVector b = new[] { 4.0, 5.0, 6.0 };

    Assert.Equal(32.0, a.Dot(b), precision: 12);
  }

  [Fact]
  public void Norm_ComputesEuclideanNorm()
  {
    RealVector v = new[] { 3.0, 4.0 };

    Assert.Equal(5.0, v.Norm(), precision: 12);
  }

  [Fact]
  public void Angle_ComputesAngleBetweenVectors()
  {
    RealVector x = new[] { 1.0, 0.0 };
    RealVector y = new[] { 0.0, 1.0 };

    Assert.Equal(Math.PI / 2.0, x.Angle(y), precision: 12);
  }

  [Fact]
  public void AsIntegerVector_RoundsElements()
  {
    RealVector v = new[] { 1.2, 1.5, 2.6, -1.5 };

    var result = v.AsIntegerVector();

    Assert.Equal(new[] { 1, 2, 3, -2 }, result.ToArray());
  }

  [Fact]
  public void ToString_FormatsElements()
  {
    RealVector v = new[] { 1.0, 2.0, 3.0 };

    Assert.Equal("[1, 2, 3]", v.ToString());
  }

  [Fact]
  public void ImplicitConversion_FromScalar_CreatesSingleElementVector()
  {
    RealVector v = 42.0;

    Assert.Single(v);
    Assert.Equal(42.0, v[0]);
  }

  [Fact]
  public void ImplicitConversion_FromArray_CreatesVectorWithArrayValues()
  {
    RealVector v = new[] { 1.0, 2.0, 3.0 };

    Assert.Equal(3, v.Count);
    Assert.Equal(new[] { 1.0, 2.0, 3.0 }, v.ToArray());
  }

  [Fact]
  public void Equality_EmptyVectors_AreEqual_AndHaveSameHashCode()
  {
    RealVector a = Array.Empty<double>();
    RealVector b = Array.Empty<double>();

    Assert.True(a.Equals(b));
    Assert.Equal(a.GetHashCode(), b.GetHashCode());
  }

  [Fact]
  public void Equals_WithNaNElements_FollowsSequenceEqualSemantics()
  {
    RealVector a = new[] { double.NaN };
    RealVector b = new[] { double.NaN };

    Assert.Equal(a.Equals(b), a.GetHashCode() == b.GetHashCode() || a.Equals(b));
    Assert.True(a.Equals(b)); // current .NET double equality semantics treat NaN.Equals(NaN) as true
  }

  [Fact]
  public void EqualityOperator_BothNull_ReturnsTrue()
  {
    RealVector? a = null;
    RealVector? b = null;
    Assert.True(a == b);
  }

  [Fact]
  public void EqualityOperator_LeftNull_RightNonNull_ReturnsFalse()
  {
    RealVector? a = null;
    RealVector b = new[] { 1.0 };
    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void CreateNormal_ReturnsVectorOfRequestedLength()
  {
    var rng = new StubRandomNumberGenerator(
      nextDoubleSequence: new[] {
        0.5, 0.25,
        0.6, 0.75,
        0.7, 0.1
      });

    RealVector mean = 0.0;
    RealVector std = 1.0;

    var result = RealVector.CreateNormal(3, mean, std, rng);

    Assert.Equal(3, result.Count);
  }

  [Fact]
  public void CreateNormal_BroadcastsScalarMeanAndStd()
  {
    var rng = new StubRandomNumberGenerator(
      nextDoubleSequence: new[] {
        0.5, 0.25,
        0.6, 0.75
      });

    RealVector mean = 10.0;
    RealVector std = 2.0;

    var result = RealVector.CreateNormal(2, mean, std, rng);

    Assert.Equal(2, result.Count);
  }

  [Fact]
  public void CreateNormal_ThrowsAfter50InvalidAttempts_ForSingleDimension()
  {
    var invalid = Enumerable.Repeat(0.0, 50).ToArray();
    var rng = new StubRandomNumberGenerator(nextDoubleSequence: invalid);

    RealVector mean = 0.0;
    RealVector std = 1.0;

    Assert.Throws<InvalidOperationException>(() => RealVector.CreateNormal(1, mean, std, rng));
  }

  [Fact]
  public void Sqrt_ReturnsElementwiseSquareRoot()
  {
    RealVector input = new[] { 0.0, 1.0, 4.0, 9.0 };

    var result = RealVector.Sqrt(input);

    Assert.Equal(new[] { 0.0, 1.0, 2.0, 3.0 }, result.ToArray());
  }

  [Fact]
  public void Sqrt_OfNegativeValue_ReturnsNaNForThatElement()
  {
    RealVector input = new[] { 4.0, -1.0, 9.0 };

    var result = RealVector.Sqrt(input).ToArray();

    Assert.Equal(2.0, result[0]);
    Assert.True(double.IsNaN(result[1]));
    Assert.Equal(3.0, result[2]);
  }

  [Fact]
  public void Log_ReturnsElementwiseNaturalLogarithm()
  {
    RealVector input = new[] { 1.0, Math.E, Math.E * Math.E };

    var result = RealVector.Log(input);

    Assert.Equal(0.0, result[0], 12);
    Assert.Equal(1.0, result[1], 12);
    Assert.Equal(2.0, result[2], 12);
  }

  [Fact]
  public void Log_OfZeroAndNegativeValue_FollowsMathLogSemantics()
  {
    RealVector input = new[] { 0.0, -1.0, 1.0 };

    var result = RealVector.Log(input).ToArray();

    Assert.True(double.IsNegativeInfinity(result[0]));
    Assert.True(double.IsNaN(result[1]));
    Assert.Equal(0.0, result[2], 12);
  }

  [Fact]
  public void Sin_ReturnsElementwiseSine()
  {
    RealVector input = new[] { 0.0, Math.PI / 2.0, Math.PI };

    var result = RealVector.Sin(input);

    Assert.Equal(0.0, result[0], 12);
    Assert.Equal(1.0, result[1], 12);
    Assert.Equal(0.0, result[2], 12);
  }

  [Fact]
  public void AreCompatible_VectorAndEnumerable_ReturnsTrue_WhenAllAreCompatible()
  {
    RealVector vector = new[] { 1.0, 2.0, 3.0 };
    var others = new[] {
      new[] { 4.0, 5.0, 6.0 },
      7.0,
      (RealVector)new[] { 8.0, 9.0, 10.0 }
    };

    var result = RealVector.AreCompatible(vector, others);

    Assert.True(result);
  }

  [Fact]
  public void AreCompatible_VectorAndEnumerable_ReturnsFalse_WhenAtLeastOneIsIncompatible()
  {
    RealVector vector = new[] { 1.0, 2.0, 3.0 };
    var others = new[] {
      new[] { 4.0, 5.0, 6.0 },
      (RealVector)new[] { 7.0, 8.0 }
    };

    var result = RealVector.AreCompatible(vector, others);

    Assert.False(result);
  }

  [Fact]
  public void AreCompatible_VectorAndEmptyEnumerable_ReturnsTrue()
  {
    RealVector vector = new[] { 1.0, 2.0, 3.0 };
    var others = Array.Empty<RealVector>();

    var result = RealVector.AreCompatible(vector, others);

    Assert.True(result);
  }

  [Fact]
  public void AreCompatible_LengthAndEnumerable_ReturnsTrue_WhenAllMatchLengthOrAreScalar()
  {
    var vectors = new[] {
      new[] { 1.0, 2.0, 3.0 },
      4.0,
      (RealVector)new[] { 5.0, 6.0, 7.0 }
    };

    var result = RealVector.AreCompatible(3, vectors);

    Assert.True(result);
  }

  [Fact]
  public void AreCompatible_LengthAndEnumerable_ReturnsFalse_WhenAtLeastOneIsIncompatible()
  {
    var vectors = new[] {
      new[] { 1.0, 2.0, 3.0 },
      (RealVector)new[] { 4.0, 5.0 }
    };

    var result = RealVector.AreCompatible(3, vectors);

    Assert.False(result);
  }

  [Fact]
  public void BroadcastLength_VectorAndEnumerable_ReturnsVectorLength_WhenOthersAreScalar()
  {
    RealVector vector = new[] { 1.0, 2.0, 3.0 };
    var others = new[] {
      4.0,
      (RealVector)5.0
    };

    var result = RealVector.BroadcastLength(vector, others);

    Assert.Equal(3, result);
  }

  [Fact]
  public void BroadcastLength_VectorAndEnumerable_ReturnsMaximumCompatibleLength()
  {
    RealVector vector = 1.0;
    var others = new[] {
      new[] { 1.0, 2.0, 3.0, 4.0 },
      2.0,
      (RealVector)new[] { 5.0, 6.0, 7.0, 8.0 }
    };

    var result = RealVector.BroadcastLength(vector, others);

    Assert.Equal(4, result);
  }

  [Fact]
  public void BroadcastLength_VectorAndEnumerable_WithEmptyEnumerable_ReturnsVectorLength()
  {
    RealVector vector = new[] { 1.0, 2.0, 3.0 };
    var others = Array.Empty<RealVector>();

    var result = RealVector.BroadcastLength(vector, others);

    Assert.Equal(3, result);
  }

  [Fact]
  public void BroadcastLength_VectorAndEnumerable_Throws_WhenIncompatible()
  {
    RealVector vector = new[] { 1.0, 2.0, 3.0 };
    var others = new[] {
      (RealVector)new[] { 4.0, 5.0 }
    };

    Assert.Throws<ArgumentException>(() => RealVector.BroadcastLength(vector, others));
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
      if (doubles.Count == 0) {
        throw new InvalidOperationException("No more test doubles available.");
      }

      return doubles.Dequeue();
    }

    public int NextInt() => throw new NotImplementedException();

    public IRandomNumberGenerator Fork(ulong forkKey) => throw new NotImplementedException();

    // Add the remaining interface members as needed for your codebase,
    // typically throwing NotSupportedException if the tests do not use them.
  }
}
