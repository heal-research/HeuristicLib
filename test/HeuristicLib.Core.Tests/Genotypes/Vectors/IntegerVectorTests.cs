using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Genotypes.Vectors;

public sealed class IntegerVectorTests
{
  [Fact]
  public void Equals_SameReference_ReturnsTrue()
  {
    IntegerVector v = new[] { 1, 2, 3 };

    Assert.True(v.Equals(v));
    Assert.True(v == v);
    Assert.False(v != v);
  }

  [Fact]
  public void Equals_Null_ReturnsFalse()
  {
    IntegerVector v = new[] { 1, 2, 3 };

    Assert.False(v.Equals(null));
    Assert.False(v.Equals((object?)null));
  }

  [Fact]
  public void Equals_ObjectOfDifferentType_ReturnsFalse()
  {
    IntegerVector v = new[] { 1, 2, 3 };

    Assert.False(v.Equals("not a vector"));
  }

  [Fact]
  public void Equals_SameElements_ReturnsTrue()
  {
    IntegerVector a = new[] { 1, 2, 3 };
    IntegerVector b = new[] { 1, 2, 3 };

    Assert.True(a.Equals(b));
    Assert.True(b.Equals(a));
    Assert.True(a.Equals((object)b));
    Assert.True(a == b);
    Assert.False(a != b);
  }

  [Fact]
  public void Equals_DifferentLengths_ReturnsFalse()
  {
    IntegerVector a = new[] { 1, 2, 3 };
    IntegerVector b = new[] { 1, 2 };

    Assert.False(a.Equals(b));
    Assert.False(b.Equals(a));
    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void Equals_DifferentElements_ReturnsFalse()
  {
    IntegerVector a = new[] { 1, 2, 3 };
    IntegerVector b = new[] { 1, 2, 4 };

    Assert.False(a.Equals(b));
    Assert.False(b.Equals(a));
    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void Equals_IsTransitive()
  {
    IntegerVector a = new[] { 1, 2, 3 };
    IntegerVector b = new[] { 1, 2, 3 };
    IntegerVector c = new[] { 1, 2, 3 };

    Assert.True(a.Equals(b));
    Assert.True(b.Equals(c));
    Assert.True(a.Equals(c));
  }

  [Fact]
  public void EqualityOperator_BothNull_ReturnsTrue()
  {
    IntegerVector? a = null;
    IntegerVector? b = null;

    Assert.True(a == b);
    Assert.False(a != b);
  }

  [Fact]
  public void EqualityOperator_LeftNull_ReturnsFalse()
  {
    IntegerVector? a = null;
    IntegerVector b = new[] { 1, 2, 3 };

    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void EqualityOperator_RightNull_ReturnsFalse()
  {
    IntegerVector a = new[] { 1, 2, 3 };
    IntegerVector? b = null;

    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void GetHashCode_EqualVectors_HaveSameHashCode()
  {
    IntegerVector a = new[] { 1, 2, 3 };
    IntegerVector b = new[] { 1, 2, 3 };

    Assert.Equal(a.GetHashCode(), b.GetHashCode());
  }

  [Fact]
  public void GetHashCode_SameInstance_IsStable()
  {
    IntegerVector v = new[] { 1, 2, 3 };

    var h1 = v.GetHashCode();
    var h2 = v.GetHashCode();

    Assert.Equal(h1, h2);
  }

  [Fact]
  public void HashSet_ContainsEquivalentVector()
  {
    var set = new HashSet<IntegerVector>();
    IntegerVector a = new[] { 1, 2, 3 };
    IntegerVector b = new[] { 1, 2, 3 };

    set.Add(a);

    Assert.True(set.Contains(b));
  }

  [Fact]
  public void HashSet_AddEquivalentVector_DoesNotIncreaseCount()
  {
    var set = new HashSet<IntegerVector>();
    IntegerVector a = new[] { 1, 2, 3 };
    IntegerVector b = new[] { 1, 2, 3 };

    set.Add(a);
    set.Add(b);

    Assert.Single(set);
  }

  [Fact]
  public void Dictionary_CanUseEquivalentVectorAsKey()
  {
    var dict = new Dictionary<IntegerVector, string>();
    IntegerVector key1 = new[] { 1, 2, 3 };
    IntegerVector key2 = new[] { 1, 2, 3 };

    dict[key1] = "value";

    Assert.True(dict.ContainsKey(key2));
    Assert.Equal("value", dict[key2]);
  }

  [Fact]
  public void Count_Indexer_AndEnumeration_WorkCorrectly()
  {
    IntegerVector v = new[] { 10, 20, 30 };

    Assert.Equal(3, v.Count);
    Assert.Equal(10, v[0]);
    Assert.Equal(20, v[1]);
    Assert.Equal(30, v[2]);
    Assert.Equal(new[] { 10, 20, 30 }, v.ToArray());
  }

  [Fact]
  public void Indexer_WithIndexFromEnd_WorksCorrectly()
  {
    IntegerVector v = new[] { 10, 20, 30 };

    Assert.Equal(30, v[^1]);
    Assert.Equal(20, v[^2]);
    Assert.Equal(10, v[^3]);
  }

  [Fact]
  public void ImplicitConversion_FromScalar_CreatesSingleElementVector()
  {
    IntegerVector v = 42;

    Assert.Equal(1, v.Count);
    Assert.Equal(42, v[0]);
  }

  [Fact]
  public void ImplicitConversion_FromArray_CreatesVectorWithArrayValues()
  {
    IntegerVector v = new[] { 1, 2, 3 };

    Assert.Equal(3, v.Count);
    Assert.Equal(new[] { 1, 2, 3 }, v.ToArray());
  }

  [Fact]
  public void ImplicitConversion_ToRealVector_ConvertsAllElementsToDouble()
  {
    IntegerVector input = new[] { 1, -2, 3 };

    RealVector result = input;

    Assert.Equal(new[] { 1.0, -2.0, 3.0 }, result.ToArray());
  }

  [Fact]
  public void ToRealVector_ConvertsAllElementsToDouble()
  {
    IntegerVector input = new[] { 1, -2, 3 };

    var result = input.ToRealVector();

    Assert.Equal(new[] { 1.0, -2.0, 3.0 }, result.ToArray());
  }

  [Fact]
  public void ToRealAt_ReturnsElementAsDouble()
  {
    IntegerVector input = new[] { 1, -2, 3 };

    Assert.Equal(-2.0, input.ToRealAt(1));
  }

  [Fact]
  public void AreCompatible_ReturnsTrue_ForSameLength()
  {
    IntegerVector a = new[] { 1, 2 };
    IntegerVector b = new[] { 3, 4 };

    Assert.True(IntegerVector.AreCompatible(a, b));
  }

  [Fact]
  public void AreCompatible_ReturnsTrue_WhenLeftIsScalar()
  {
    IntegerVector a = 1;
    IntegerVector b = new[] { 3, 4, 5 };

    Assert.True(IntegerVector.AreCompatible(a, b));
  }

  [Fact]
  public void AreCompatible_ReturnsTrue_WhenRightIsScalar()
  {
    IntegerVector a = new[] { 3, 4, 5 };
    IntegerVector b = 1;

    Assert.True(IntegerVector.AreCompatible(a, b));
  }

  [Fact]
  public void AreCompatible_ReturnsFalse_ForDifferentNonScalarLengths()
  {
    IntegerVector a = new[] { 1, 2 };
    IntegerVector b = new[] { 3, 4, 5 };

    Assert.False(IntegerVector.AreCompatible(a, b));
  }

  [Fact]
  public void BroadcastLength_ReturnsMaxLength()
  {
    IntegerVector scalar = 1;
    IntegerVector vector = new[] { 3, 4, 5 };

    Assert.Equal(3, IntegerVector.BroadcastLength(scalar, vector));
    Assert.Equal(3, IntegerVector.BroadcastLength(vector, scalar));
  }

  [Fact]
  public void Add_SameLength_AddsElementwise()
  {
    IntegerVector a = new[] { 1, 2, 3 };
    IntegerVector b = new[] { 10, 20, 30 };

    var result = IntegerVector.Add(a, b);

    Assert.Equal(new[] { 11, 22, 33 }, result.ToArray());
  }

  [Fact]
  public void Add_BroadcastsScalarLeft()
  {
    IntegerVector scalar = 2;
    IntegerVector vector = new[] { 10, 20, 30 };

    var result = IntegerVector.Add(scalar, vector);

    Assert.Equal(new[] { 12, 22, 32 }, result.ToArray());
  }

  [Fact]
  public void Add_BroadcastsScalarRight()
  {
    IntegerVector vector = new[] { 10, 20, 30 };
    IntegerVector scalar = 2;

    var result = IntegerVector.Add(vector, scalar);

    Assert.Equal(new[] { 12, 22, 32 }, result.ToArray());
  }

  [Fact]
  public void Add_IncompatibleLengths_Throws()
  {
    IntegerVector a = new[] { 1, 2 };
    IntegerVector b = new[] { 10, 20, 30 };

    Assert.Throws<ArgumentException>(() => IntegerVector.Add(a, b));
  }

  [Fact]
  public void Subtract_SameLength_SubtractsElementwise()
  {
    IntegerVector a = new[] { 10, 20, 30 };
    IntegerVector b = new[] { 1, 2, 3 };

    var result = IntegerVector.Subtract(a, b);

    Assert.Equal(new[] { 9, 18, 27 }, result.ToArray());
  }

  [Fact]
  public void Multiply_SameLength_MultipliesElementwise()
  {
    IntegerVector a = new[] { 2, 3, 4 };
    IntegerVector b = new[] { 10, 20, 30 };

    var result = IntegerVector.Multiply(a, b);

    Assert.Equal(new[] { 20, 60, 120 }, result.ToArray());
  }

  [Fact]
  public void Divide_SameLength_DividesElementwise()
  {
    IntegerVector a = new[] { 10, 20, 30 };
    IntegerVector b = new[] { 2, 4, 5 };

    var result = IntegerVector.Divide(a, b);

    Assert.Equal(new[] { 5, 5, 6 }, result.ToArray());
  }

  [Fact]
  public void Operators_DelegateToArithmeticMethods()
  {
    IntegerVector a = new[] { 10, 20, 30 };
    IntegerVector b = new[] { 2, 4, 5 };

    Assert.Equal(new[] { 12, 24, 35 }, (a + b).ToArray());
    Assert.Equal(new[] { 8, 16, 25 }, (a - b).ToArray());
    Assert.Equal(new[] { 20, 80, 150 }, (a * b).ToArray());
    Assert.Equal(new[] { 5, 5, 6 }, (a / b).ToArray());
  }

  [Fact]
  public void Clamp_ScalarBounds_ClampsValues()
  {
    IntegerVector input = new[] { -1, 2, 10 };
    IntegerVector min = 0;
    IntegerVector max = 5;

    var result = IntegerVector.Clamp(input, min, max);

    Assert.Equal(new[] { 0, 2, 5 }, result.ToArray());
    Assert.NotSame(input, result);
  }

  [Fact]
  public void ClampAt_UsesDimensionBounds()
  {
    IntegerVector input = new[] { -1, 2, 10 };
    IntegerVector min = new[] { 0, 1, 2 };
    IntegerVector max = new[] { 5, 3, 8 };

    Assert.Equal(0, input.ClampAt(min, max, 0));
    Assert.Equal(2, input.ClampAt(min, max, 1));
    Assert.Equal(8, input.ClampAt(min, max, 2));
  }

  [Fact]
  public void GreaterThan_SameLength_WorksElementwise()
  {
    IntegerVector a = new[] { 1, 5, 3 };
    IntegerVector b = new[] { 2, 5, 1 };

    Assert.Equal(new[] { false, false, true }, (a > b).ToArray());
  }

  [Fact]
  public void LessThan_SameLength_WorksElementwise()
  {
    IntegerVector a = new[] { 1, 5, 3 };
    IntegerVector b = new[] { 2, 5, 4 };

    Assert.Equal(new[] { true, false, true }, (a < b).ToArray());
  }

  [Fact]
  public void GreaterThanOrEqual_SameLength_WorksElementwise()
  {
    IntegerVector a = new[] { 1, 5, 3 };
    IntegerVector b = new[] { 2, 5, 1 };

    Assert.Equal(new[] { false, true, true }, (a >= b).ToArray());
  }

  [Fact]
  public void LessThanOrEqual_SameLength_WorksElementwise()
  {
    IntegerVector a = new[] { 1, 5, 3 };
    IntegerVector b = new[] { 2, 5, 3 };

    Assert.Equal(new[] { true, true, true }, (a <= b).ToArray());
  }

  [Fact]
  public void ComparisonOperators_BroadcastScalarLeft()
  {
    IntegerVector scalar = 3;
    IntegerVector vector = new[] { 1, 3, 5 };

    Assert.Equal(new[] { true, false, false }, (scalar > vector).ToArray());
    Assert.Equal(new[] { false, false, true }, (scalar < vector).ToArray());
    Assert.Equal(new[] { true, true, false }, (scalar >= vector).ToArray());
    Assert.Equal(new[] { false, true, true }, (scalar <= vector).ToArray());
  }

  [Fact]
  public void ComparisonOperators_BroadcastScalarRight()
  {
    IntegerVector vector = new[] { 1, 3, 5 };
    IntegerVector scalar = 3;

    Assert.Equal(new[] { false, false, true }, (vector > scalar).ToArray());
    Assert.Equal(new[] { true, false, false }, (vector < scalar).ToArray());
    Assert.Equal(new[] { false, true, true }, (vector >= scalar).ToArray());
    Assert.Equal(new[] { true, true, false }, (vector <= scalar).ToArray());
  }

  [Fact]
  public void ComparisonOperators_IncompatibleLengths_ThrowArgumentException()
  {
    IntegerVector a = new[] { 1, 2 };
    IntegerVector b = new[] { 1, 2, 3 };

    Assert.Throws<ArgumentException>(() => a > b);
    Assert.Throws<ArgumentException>(() => a < b);
    Assert.Throws<ArgumentException>(() => a >= b);
    Assert.Throws<ArgumentException>(() => a <= b);
  }

  [Fact]
  public void CreateUniform_ReturnsVectorOfRequestedLength()
  {
    var rng = new StubRandomNumberGenerator(0.1, 0.2, 0.3);

    IntegerVector low = 0;
    IntegerVector high = 10;

    var result = IntegerVector.CreateUniform(3, low, high, rng);

    Assert.Equal(3, result.Count);
  }

  [Fact]
  public void CreateUniform_MapsUniformDrawsIntoScalarBounds()
  {
    var rng = new StubRandomNumberGenerator(0.9, 0.5, 0.1);

    IntegerVector low = 10;
    IntegerVector high = 12;

    var result = IntegerVector.CreateUniform(3, low, high, rng);

    Assert.Equal(new[] { 12, 11, 10 }, result.ToArray());
  }

  [Fact]
  public void CreateUniform_UsesElementwiseBounds()
  {
    var rng = new StubRandomNumberGenerator(0.9, 0.9, 0.9);

    IntegerVector low = new[] { 10, 20, 30 };
    IntegerVector high = new[] { 12, 22, 32 };

    var result = IntegerVector.CreateUniform(3, low, high, rng);

    Assert.Equal(new[] { 12, 22, 32 }, result.ToArray());
  }

  [Fact]
  public void CreateUniform_LowLengthMismatch_ThrowsArgumentException()
  {
    var rng = new StubRandomNumberGenerator(0.1, 0.2, 0.3);

    IntegerVector low = new[] { 0, 1 };
    IntegerVector high = 10;

    Assert.Throws<ArgumentException>(() => IntegerVector.CreateUniform(3, low, high, rng));
  }

  [Fact]
  public void CreateUniform_HighLengthMismatch_ThrowsArgumentException()
  {
    var rng = new StubRandomNumberGenerator(0.1, 0.2, 0.3);

    IntegerVector low = 0;
    IntegerVector high = new[] { 10, 11 };

    Assert.Throws<ArgumentException>(() => IntegerVector.CreateUniform(3, low, high, rng));
  }

  [Fact]
  public void Equality_EmptyVectors_AreEqual_AndHaveSameHashCode()
  {
    IntegerVector a = Array.Empty<int>();
    IntegerVector b = Array.Empty<int>();

    Assert.True(a.Equals(b));
    Assert.Equal(a.GetHashCode(), b.GetHashCode());
  }

  private sealed class StubRandomNumberGenerator : IRandomNumberGenerator
  {
    private readonly Queue<double> doubles;

    public int NextDoubleCallCount { get; private set; }

    public StubRandomNumberGenerator(params double[] nextDoubles)
    {
      doubles = new Queue<double>(nextDoubles);
    }

    public int NextInt() => throw new NotSupportedException();

    public IRandomNumberGenerator Fork(ulong forkKey) => throw new NotImplementedException();

    public double NextDouble()
    {
      NextDoubleCallCount++;

      if (doubles.Count == 0) {
        throw new InvalidOperationException("No more test doubles available.");
      }

      return doubles.Dequeue();
    }
  }
}
