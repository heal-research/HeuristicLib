using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Tests.Genotypes.Vectors;

public sealed class BoolVectorTests
{
  [Fact]
  public void Create_FromArray_CopiesElements()
  {
    var elements = new[] { true, false, true };

    var vector = BoolVector.Create(elements);
    elements[0] = false;

    Assert.Equal(new[] { true, false, true }, vector.ToArray());
  }

  [Fact]
  public void Create_FromEnumerable_CopiesElements()
  {
    var elements = new List<bool> { true, false, true };

    var vector = BoolVector.Create(elements);
    elements[0] = false;

    Assert.Equal(new[] { true, false, true }, vector.ToArray());
  }

  [Fact]
  public void FromOwnedArray_UsesProvidedArray()
  {
    var elements = new[] { true, false, true };

    var vector = BoolVector.FromOwnedArray(elements);
    elements[0] = false;

    Assert.Equal(new[] { false, false, true }, vector.ToArray());
  }

  [Fact]
  public void Constructor_FromEnumerable_CreatesVector()
  {
    var vector = new BoolVector(true, false, true);

    Assert.Equal(3, vector.Count);
    Assert.Equal(new[] { true, false, true }, vector.ToArray());
  }

  [Fact]
  public void Constructor_FromScalar_CreatesSingleElementVector()
  {
    var vector = new BoolVector(true);

    Assert.Single(vector);
    Assert.True(vector[0]);
  }

  [Fact]
  public void ImplicitConversion_FromScalar_CreatesSingleElementVector()
  {
    BoolVector vector = true;

    Assert.Single(vector);
    Assert.True(vector[0]);
  }

  [Fact]
  public void Count_Indexer_AndEnumeration_WorkCorrectly()
  {
    var vector = new BoolVector(true, false, true);

    Assert.Equal(3, vector.Count);
    Assert.True(vector[0]);
    Assert.False(vector[1]);
    Assert.True(vector[2]);
    Assert.Equal(new[] { true, false, true }, vector.ToArray());
  }

  [Fact]
  public void Indexer_WithIndexFromEnd_WorksCorrectly()
  {
    var vector = new BoolVector(true, false, true);

    Assert.True(vector[^1]);
    Assert.False(vector[^2]);
    Assert.True(vector[^3]);
  }

  [Fact]
  public void Contains_ReturnsTrue_WhenValueExists()
  {
    var vector = new BoolVector(true, false, true);

    Assert.True(vector.Contains(true));
    Assert.True(vector.Contains(false));
  }

  [Fact]
  public void Contains_ReturnsFalse_WhenValueDoesNotExist()
  {
    var allTrue = new BoolVector(true, true, true);
    var allFalse = new BoolVector(false, false, false);

    Assert.False(allTrue.Contains(false));
    Assert.False(allFalse.Contains(true));
  }

  [Fact]
  public void AreCompatible_ReturnsTrue_ForSameLength()
  {
    BoolVector a = new BoolVector(true, false);
    BoolVector b = new BoolVector(false, true);

    Assert.True(BoolVector.AreCompatible(a, b));
  }

  [Fact]
  public void AreCompatible_ReturnsTrue_WhenLeftIsScalar()
  {
    BoolVector a = true;
    BoolVector b = new BoolVector(false, true, false);

    Assert.True(BoolVector.AreCompatible(a, b));
  }

  [Fact]
  public void AreCompatible_ReturnsTrue_WhenRightIsScalar()
  {
    BoolVector a = new BoolVector(false, true, false);
    BoolVector b = false;

    Assert.True(BoolVector.AreCompatible(a, b));
  }

  [Fact]
  public void AreCompatible_ReturnsFalse_ForDifferentNonScalarLengths()
  {
    BoolVector a = new BoolVector(true, false);
    BoolVector b = new BoolVector(true, false, true);

    Assert.False(BoolVector.AreCompatible(a, b));
  }

  [Fact]
  public void BroadcastLength_ReturnsMaximumLength()
  {
    BoolVector scalar = true;
    BoolVector vector = new BoolVector(true, false, true);

    Assert.Equal(3, BoolVector.BroadcastLength(scalar, vector));
    Assert.Equal(3, BoolVector.BroadcastLength(vector, scalar));
  }

  [Fact]
  public void And_SameLength_ComputesElementwiseAnd()
  {
    BoolVector a = new BoolVector(true, true, false, false);
    BoolVector b = new BoolVector(true, false, true, false);

    var result = BoolVector.And(a, b);

    Assert.Equal(new[] { true, false, false, false }, result.ToArray());
  }

  [Fact]
  public void Or_SameLength_ComputesElementwiseOr()
  {
    BoolVector a = new BoolVector(true, true, false, false);
    BoolVector b = new BoolVector(true, false, true, false);

    var result = BoolVector.Or(a, b);

    Assert.Equal(new[] { true, true, true, false }, result.ToArray());
  }

  [Fact]
  public void Xor_SameLength_ComputesElementwiseXor()
  {
    BoolVector a = new BoolVector(true, true, false, false);
    BoolVector b = new BoolVector(true, false, true, false);

    var result = BoolVector.Xor(a, b);

    Assert.Equal(new[] { false, true, true, false }, result.ToArray());
  }

  [Fact]
  public void Not_ComputesElementwiseNegation()
  {
    BoolVector a = new BoolVector(true, false, true);

    var result = BoolVector.Not(a);

    Assert.Equal(new[] { false, true, false }, result.ToArray());
  }

  [Fact]
  public void And_BroadcastsScalarLeft()
  {
    BoolVector scalar = true;
    BoolVector vector = new BoolVector(true, false, true);

    var result = BoolVector.And(scalar, vector);

    Assert.Equal(new[] { true, false, true }, result.ToArray());
  }

  [Fact]
  public void And_BroadcastsScalarRight()
  {
    BoolVector vector = new BoolVector(true, false, true);
    BoolVector scalar = false;

    var result = BoolVector.And(vector, scalar);

    Assert.Equal(new[] { false, false, false }, result.ToArray());
  }

  [Fact]
  public void Or_BroadcastsScalarLeft()
  {
    BoolVector scalar = false;
    BoolVector vector = new BoolVector(true, false, true);

    var result = BoolVector.Or(scalar, vector);

    Assert.Equal(new[] { true, false, true }, result.ToArray());
  }

  [Fact]
  public void Or_BroadcastsScalarRight()
  {
    BoolVector vector = new BoolVector(true, false, true);
    BoolVector scalar = true;

    var result = BoolVector.Or(vector, scalar);

    Assert.Equal(new[] { true, true, true }, result.ToArray());
  }

  [Fact]
  public void Xor_BroadcastsScalarLeft()
  {
    BoolVector scalar = true;
    BoolVector vector = new BoolVector(true, false, true);

    var result = BoolVector.Xor(scalar, vector);

    Assert.Equal(new[] { false, true, false }, result.ToArray());
  }

  [Fact]
  public void Xor_BroadcastsScalarRight()
  {
    BoolVector vector = new BoolVector(true, false, true);
    BoolVector scalar = false;

    var result = BoolVector.Xor(vector, scalar);

    Assert.Equal(new[] { true, false, true }, result.ToArray());
  }

  [Fact]
  public void LogicalOperators_DelegateToMethods()
  {
    BoolVector a = new BoolVector(true, true, false, false);
    BoolVector b = new BoolVector(true, false, true, false);

    Assert.Equal(new[] { true, false, false, false }, (a & b).ToArray());
    Assert.Equal(new[] { true, true, true, false }, (a | b).ToArray());
    Assert.Equal(new[] { false, true, true, false }, (a ^ b).ToArray());
    Assert.Equal(new[] { false, false, true, true }, (!a).ToArray());
  }

  [Fact]
  public void And_IncompatibleLengths_ThrowsArgumentException()
  {
    BoolVector a = new BoolVector(true, false);
    BoolVector b = new BoolVector(true, false, true);

    Assert.Throws<ArgumentException>(() => BoolVector.And(a, b));
  }

  [Fact]
  public void Or_IncompatibleLengths_ThrowsArgumentException()
  {
    BoolVector a = new BoolVector(true, false);
    BoolVector b = new BoolVector(true, false, true);

    Assert.Throws<ArgumentException>(() => BoolVector.Or(a, b));
  }

  [Fact]
  public void Xor_IncompatibleLengths_ThrowsArgumentException()
  {
    BoolVector a = new BoolVector(true, false);
    BoolVector b = new BoolVector(true, false, true);

    Assert.Throws<ArgumentException>(() => BoolVector.Xor(a, b));
  }

  [Fact]
  public void All_ReturnsTrue_WhenAllElementsAreTrue()
  {
    var vector = new BoolVector(true, true, true);

    Assert.True(vector.All());
  }

  [Fact]
  public void All_ReturnsFalse_WhenAtLeastOneElementIsFalse()
  {
    var vector = new BoolVector(true, false, true);

    Assert.False(vector.All());
  }

  [Fact]
  public void Any_ReturnsTrue_WhenAtLeastOneElementIsTrue()
  {
    var vector = new BoolVector(false, true, false);

    Assert.True(vector.Any());
  }

  [Fact]
  public void Any_ReturnsFalse_WhenAllElementsAreFalse()
  {
    var vector = new BoolVector(false, false, false);

    Assert.False(vector.Any());
  }

  [Fact]
  public void TrueCount_ReturnsNumberOfTrueElements()
  {
    var vector = new BoolVector(true, false, true, true, false);

    Assert.Equal(3, vector.TrueCount());
  }

  [Fact]
  public void Equals_SameReference_ReturnsTrue()
  {
    BoolVector vector = new BoolVector(true, false, true);

    Assert.True(vector.Equals(vector));
    Assert.True(vector == vector);
    Assert.False(vector != vector);
  }

  [Fact]
  public void Equals_Null_ReturnsFalse()
  {
    BoolVector vector = new BoolVector(true, false, true);

    Assert.False(vector.Equals(null));
    Assert.False(vector.Equals((object?)null));
  }

  [Fact]
  public void Equals_ObjectOfDifferentType_ReturnsFalse()
  {
    BoolVector vector = new BoolVector(true, false, true);

    Assert.False(vector.Equals("not a BoolVector"));
  }

  [Fact]
  public void Equals_SameElements_ReturnsTrue()
  {
    BoolVector a = new BoolVector(true, false, true);
    BoolVector b = new BoolVector(true, false, true);

    Assert.True(a.Equals(b));
    Assert.True(b.Equals(a));
    Assert.True(a.Equals((object)b));
    Assert.True(a == b);
    Assert.False(a != b);
  }

  [Fact]
  public void Equals_DifferentLengths_ReturnsFalse()
  {
    BoolVector a = new BoolVector(true, false, true);
    BoolVector b = new BoolVector(true, false);

    Assert.False(a.Equals(b));
    Assert.False(b.Equals(a));
    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void Equals_DifferentElements_ReturnsFalse()
  {
    BoolVector a = new BoolVector(true, false, true);
    BoolVector b = new BoolVector(true, true, true);

    Assert.False(a.Equals(b));
    Assert.False(b.Equals(a));
    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void Equals_IsTransitive()
  {
    BoolVector a = new BoolVector(true, false, true);
    BoolVector b = new BoolVector(true, false, true);
    BoolVector c = new BoolVector(true, false, true);

    Assert.True(a.Equals(b));
    Assert.True(b.Equals(c));
    Assert.True(a.Equals(c));
  }

  [Fact]
  public void EqualityOperator_BothNull_ReturnsTrue()
  {
    BoolVector? a = null;
    BoolVector? b = null;

    Assert.True(a == b);
    Assert.False(a != b);
  }

  [Fact]
  public void EqualityOperator_LeftNull_ReturnsFalse()
  {
    BoolVector? a = null;
    BoolVector b = new BoolVector(true);

    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void EqualityOperator_RightNull_ReturnsFalse()
  {
    BoolVector a = new BoolVector(true);
    BoolVector? b = null;

    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void GetHashCode_EqualVectors_HaveSameHashCode()
  {
    BoolVector a = new BoolVector(true, false, true);
    BoolVector b = new BoolVector(true, false, true);

    Assert.Equal(a.GetHashCode(), b.GetHashCode());
  }

  [Fact]
  public void GetHashCode_SameInstance_IsStable()
  {
    BoolVector vector = new BoolVector(true, false, true);

    var h1 = vector.GetHashCode();
    var h2 = vector.GetHashCode();

    Assert.Equal(h1, h2);
  }

  [Fact]
  public void HashSet_ContainsEquivalentVector()
  {
    var set = new HashSet<BoolVector>();
    BoolVector a = new BoolVector(true, false, true);
    BoolVector b = new BoolVector(true, false, true);

    set.Add(a);
    Assert.Contains(b, set);
  }

  [Fact]
  public void HashSet_AddEquivalentVector_DoesNotIncreaseCount()
  {
    var set = new HashSet<BoolVector>();
    BoolVector a = new BoolVector(true, false, true);
    BoolVector b = new BoolVector(true, false, true);

    set.Add(a);
    set.Add(b);

    Assert.Single(set);
  }

  [Fact]
  public void Dictionary_CanUseEquivalentVectorAsKey()
  {
    var dict = new Dictionary<BoolVector, string>();
    BoolVector key1 = new BoolVector(true, false, true);
    BoolVector key2 = new BoolVector(true, false, true);

    dict[key1] = "value";

    Assert.True(dict.ContainsKey(key2));
    Assert.Equal("value", dict[key2]);
  }

  [Fact]
  public void Equality_EmptyVectors_AreEqual_AndHaveSameHashCode()
  {
    var a = new BoolVector();
    var b = new BoolVector();

    Assert.True(a.Equals(b));
    Assert.Equal(a.GetHashCode(), b.GetHashCode());
  }

  [Fact]
  public void ToString_FormatsValuesAsTrueAndFalse()
  {
    var vector = new BoolVector(true, false, true);

    Assert.Equal("[True, False, True]", vector.ToString());
  }

  [Fact]
  public void All_OnEmptyVector_ReturnsTrue()
  {
    var vector = new BoolVector();

    Assert.True(vector.All());
  }

  [Fact]
  public void Any_OnEmptyVector_ReturnsFalse()
  {
    var vector = new BoolVector();

    Assert.False(vector.Any());
  }

  [Fact]
  public void TrueCount_OnEmptyVector_ReturnsZero()
  {
    var vector = new BoolVector();

    Assert.Equal(0, vector.TrueCount());
  }

  [Fact]
  public void ImplicitConversion_FromArray_CreatesVectorWithValues()
  {
    BoolVector vector = new[] { true, false, true };

    Assert.Equal(3, vector.Count);
    Assert.Equal(new[] { true, false, true }, vector.ToArray());
  }
}
