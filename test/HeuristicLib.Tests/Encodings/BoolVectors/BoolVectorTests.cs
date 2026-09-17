using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Encodings.Vectors;

namespace HEAL.HeuristicLib.Tests.Genotypes.Vectors;

public sealed class BoolVectorTests
{
    [Fact]
    public void Create_FromArray_CopiesElements()
    {
        var elements = new[] { true, false, true };

        var vector = BoolVector.Create(elements);
        elements[0] = false;

        vector.ToArray().ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void Create_FromEnumerable_CopiesElements()
    {
        var elements = new List<bool> { true, false, true };

        var vector = BoolVector.Create(elements);
        elements[0] = false;

        vector.ToArray().ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void FromOwnedArray_UsesProvidedArray()
    {
        var elements = new[] { true, false, true };

        var vector = BoolVector.FromOwnedArray(elements);
        elements[0] = false;

        vector.ToArray().ShouldBe(new[] { false, false, true });
    }

    [Fact]
    public void Constructor_FromEnumerable_CreatesVector()
    {
        var vector = new BoolVector(true, false, true);

        vector.Count.ShouldBe(3);
        vector.ToArray().ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void Constructor_FromScalar_CreatesSingleElementVector()
    {
        var vector = new BoolVector(true);

        vector.ShouldHaveSingleItem();
        vector[0].ShouldBeTrue();
    }

    [Fact]
    public void ImplicitConversion_FromScalar_CreatesSingleElementVector()
    {
        BoolVector vector = true;

        vector.ShouldHaveSingleItem();
        vector[0].ShouldBeTrue();
    }

    [Fact]
    public void Count_Indexer_AndEnumeration_WorkCorrectly()
    {
        var vector = new BoolVector(true, false, true);

        vector.Count.ShouldBe(3);
        vector[0].ShouldBeTrue();
        vector[1].ShouldBeFalse();
        vector[2].ShouldBeTrue();
        vector.ToArray().ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void Indexer_WithIndexFromEnd_WorksCorrectly()
    {
        var vector = new BoolVector(true, false, true);

        vector[^1].ShouldBeTrue();
        vector[^2].ShouldBeFalse();
        vector[^3].ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsTrue_WhenValueExists()
    {
        var vector = new BoolVector(true, false, true);

        vector.Contains(true).ShouldBeTrue();
        vector.Contains(false).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_WhenValueDoesNotExist()
    {
        var allTrue = new BoolVector(true, true, true);
        var allFalse = new BoolVector(false, false, false);

        allTrue.Contains(false).ShouldBeFalse();
        allFalse.Contains(true).ShouldBeFalse();
    }

    [Fact]
    public void AreBroadcastable_ReturnsTrue_ForSameLength()
    {
        BoolVector a = new BoolVector(true, false);
        BoolVector b = new BoolVector(false, true);

        Vector.AreBroadcastable(a, b).ShouldBeTrue();
    }

    [Fact]
    public void AreBroadcastable_ReturnsTrue_WhenLeftIsScalar()
    {
        BoolVector a = true;
        BoolVector b = new BoolVector(false, true, false);

        Vector.AreBroadcastable(a, b).ShouldBeTrue();
    }

    [Fact]
    public void AreBroadcastable_ReturnsTrue_WhenRightIsScalar()
    {
        BoolVector a = new BoolVector(false, true, false);
        BoolVector b = false;

        Vector.AreBroadcastable(a, b).ShouldBeTrue();
    }

    [Fact]
    public void AreBroadcastable_ReturnsFalse_ForDifferentNonScalarLengths()
    {
        BoolVector a = new BoolVector(true, false);
        BoolVector b = new BoolVector(true, false, true);

        Vector.AreBroadcastable(a, b).ShouldBeFalse();
    }

    [Fact]
    public void BroadcastLength_ReturnsNonScalarLength()
    {
        BoolVector scalar = true;
        BoolVector vector = new BoolVector(true, false, true);

        Vector.BroadcastLength(scalar, vector).ShouldBe(3);
        Vector.BroadcastLength(vector, scalar).ShouldBe(3);
    }

    [Fact]
    public void BroadcastLength_ScalarAndEmptyVector_ReturnsZero()
    {
        BoolVector scalar = true;
        var empty = BoolVector.Create();

        Vector.BroadcastLength(scalar, empty).ShouldBe(0);
        Vector.BroadcastLength(empty, scalar).ShouldBe(0);
        (scalar & empty).ShouldBeEmpty();
        (empty & scalar).ShouldBeEmpty();
    }

    [Fact]
    public void AreBroadcastable_VectorAndEnumerable_ReturnsFalse_WhenScalarPrecedesDifferentLengths()
    {
        BoolVector scalar = true;
        var others = new[] { BoolVector.Create(true, false), BoolVector.Create(true, false, true) };

        Vector.AreBroadcastable(scalar, others).ShouldBeFalse();
    }

    [Fact]
    public void BroadcastLength_VectorAndEnumerable_ReturnsCommonLengthOrThrows()
    {
        BoolVector scalar = true;
        var compatible = new[] { BoolVector.Create(true, false, true), false };
        var incompatible = new[] { BoolVector.Create(true, false), BoolVector.Create(true, false, true) };

        Vector.BroadcastLength(scalar, compatible).ShouldBe(3);
        Should.Throw<ArgumentException>(() => Vector.BroadcastLength(scalar, incompatible));
    }

    [Fact]
    public void AreBroadcastableTo_AcceptsScalarsAndMatchingLengths()
    {
        Vector.AreBroadcastableTo(3, BoolVector.Create(true), BoolVector.Create(true, false, true)).ShouldBeTrue();
        Vector.AreBroadcastableTo(3, BoolVector.Create(true, false)).ShouldBeFalse();
    }

    [Fact]
    public void And_SameLength_ComputesElementwiseAnd()
    {
        BoolVector a = new BoolVector(true, true, false, false);
        BoolVector b = new BoolVector(true, false, true, false);

        var result = BoolVector.And(a, b);

        result.ToArray().ShouldBe(new[] { true, false, false, false });
    }

    [Fact]
    public void Or_SameLength_ComputesElementwiseOr()
    {
        BoolVector a = new BoolVector(true, true, false, false);
        BoolVector b = new BoolVector(true, false, true, false);

        var result = BoolVector.Or(a, b);

        result.ToArray().ShouldBe(new[] { true, true, true, false });
    }

    [Fact]
    public void Xor_SameLength_ComputesElementwiseXor()
    {
        BoolVector a = new BoolVector(true, true, false, false);
        BoolVector b = new BoolVector(true, false, true, false);

        var result = BoolVector.Xor(a, b);

        result.ToArray().ShouldBe(new[] { false, true, true, false });
    }

    [Fact]
    public void Not_ComputesElementwiseNegation()
    {
        BoolVector a = new BoolVector(true, false, true);

        var result = BoolVector.Not(a);

        result.ToArray().ShouldBe(new[] { false, true, false });
    }

    [Fact]
    public void And_BroadcastsScalarLeft()
    {
        BoolVector scalar = true;
        BoolVector vector = new BoolVector(true, false, true);

        var result = BoolVector.And(scalar, vector);

        result.ToArray().ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void And_BroadcastsScalarRight()
    {
        BoolVector vector = new BoolVector(true, false, true);
        BoolVector scalar = false;

        var result = BoolVector.And(vector, scalar);

        result.ToArray().ShouldBe(new[] { false, false, false });
    }

    [Fact]
    public void Or_BroadcastsScalarLeft()
    {
        BoolVector scalar = false;
        BoolVector vector = new BoolVector(true, false, true);

        var result = BoolVector.Or(scalar, vector);

        result.ToArray().ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void Or_BroadcastsScalarRight()
    {
        BoolVector vector = new BoolVector(true, false, true);
        BoolVector scalar = true;

        var result = BoolVector.Or(vector, scalar);

        result.ToArray().ShouldBe(new[] { true, true, true });
    }

    [Fact]
    public void Xor_BroadcastsScalarLeft()
    {
        BoolVector scalar = true;
        BoolVector vector = new BoolVector(true, false, true);

        var result = BoolVector.Xor(scalar, vector);

        result.ToArray().ShouldBe(new[] { false, true, false });
    }

    [Fact]
    public void Xor_BroadcastsScalarRight()
    {
        BoolVector vector = new BoolVector(true, false, true);
        BoolVector scalar = false;

        var result = BoolVector.Xor(vector, scalar);

        result.ToArray().ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void LogicalOperators_DelegateToMethods()
    {
        BoolVector a = new BoolVector(true, true, false, false);
        BoolVector b = new BoolVector(true, false, true, false);

        (a & b).ToArray().ShouldBe(new[] { true, false, false, false });
        (a | b).ToArray().ShouldBe(new[] { true, true, true, false });
        (a ^ b).ToArray().ShouldBe(new[] { false, true, true, false });
        (!a).ToArray().ShouldBe(new[] { false, false, true, true });
    }

    [Fact]
    public void And_IncompatibleLengths_ThrowsArgumentException()
    {
        BoolVector a = new BoolVector(true, false);
        BoolVector b = new BoolVector(true, false, true);

        Should.Throw<ArgumentException>(() => BoolVector.And(a, b));
    }

    [Fact]
    public void Or_IncompatibleLengths_ThrowsArgumentException()
    {
        BoolVector a = new BoolVector(true, false);
        BoolVector b = new BoolVector(true, false, true);

        Should.Throw<ArgumentException>(() => BoolVector.Or(a, b));
    }

    [Fact]
    public void Xor_IncompatibleLengths_ThrowsArgumentException()
    {
        BoolVector a = new BoolVector(true, false);
        BoolVector b = new BoolVector(true, false, true);

        Should.Throw<ArgumentException>(() => BoolVector.Xor(a, b));
    }

    [Fact]
    public void All_ReturnsTrue_WhenAllElementsAreTrue()
    {
        var vector = new BoolVector(true, true, true);

        vector.All().ShouldBeTrue();
    }

    [Fact]
    public void All_ReturnsFalse_WhenAtLeastOneElementIsFalse()
    {
        var vector = new BoolVector(true, false, true);

        vector.All().ShouldBeFalse();
    }

    [Fact]
    public void Any_ReturnsTrue_WhenAtLeastOneElementIsTrue()
    {
        var vector = new BoolVector(false, true, false);

        vector.Any().ShouldBeTrue();
    }

    [Fact]
    public void Any_ReturnsFalse_WhenAllElementsAreFalse()
    {
        var vector = new BoolVector(false, false, false);

        vector.Any().ShouldBeFalse();
    }

    [Fact]
    public void TrueCount_ReturnsNumberOfTrueElements()
    {
        var vector = new BoolVector(true, false, true, true, false);

        vector.TrueCount().ShouldBe(3);
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        BoolVector vector = new BoolVector(true, false, true);

        vector.Equals(vector).ShouldBeTrue();
#pragma warning disable CS1718
        (vector == vector).ShouldBeTrue();
        (vector != vector).ShouldBeFalse();
#pragma warning restore CS1718
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        BoolVector vector = new BoolVector(true, false, true);

        vector.Equals(null).ShouldBeFalse();
        vector.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameElements_ReturnsTrue()
    {
        BoolVector a = new BoolVector(true, false, true);
        BoolVector b = new BoolVector(true, false, true);

        a.Equals(b).ShouldBeTrue();
        b.Equals(a).ShouldBeTrue();
        a.Equals((object)b).ShouldBeTrue();
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentLengths_ReturnsFalse()
    {
        BoolVector a = new BoolVector(true, false, true);
        BoolVector b = new BoolVector(true, false);

        a.Equals(b).ShouldBeFalse();
        b.Equals(a).ShouldBeFalse();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentElements_ReturnsFalse()
    {
        BoolVector a = new BoolVector(true, false, true);
        BoolVector b = new BoolVector(true, true, true);

        a.Equals(b).ShouldBeFalse();
        b.Equals(a).ShouldBeFalse();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_IsTransitive()
    {
        BoolVector a = new BoolVector(true, false, true);
        BoolVector b = new BoolVector(true, false, true);
        BoolVector c = new BoolVector(true, false, true);

        a.Equals(b).ShouldBeTrue();
        b.Equals(c).ShouldBeTrue();
        a.Equals(c).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        BoolVector? a = null;
        BoolVector? b = null;

        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_LeftNull_ReturnsFalse()
    {
        BoolVector? a = null;
        BoolVector b = new BoolVector(true);

        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_RightNull_ReturnsFalse()
    {
        BoolVector a = new BoolVector(true);
        BoolVector? b = null;

        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_EqualVectors_HaveSameHashCode()
    {
        BoolVector a = new BoolVector(true, false, true);
        BoolVector b = new BoolVector(true, false, true);

        b.GetHashCode().ShouldBe(a.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameInstance_IsStable()
    {
        BoolVector vector = new BoolVector(true, false, true);

        var h1 = vector.GetHashCode();
        var h2 = vector.GetHashCode();

        h2.ShouldBe(h1);
    }

    [Fact]
    public void HashSet_ContainsEquivalentVector()
    {
        var set = new HashSet<BoolVector>();
        BoolVector a = new BoolVector(true, false, true);
        BoolVector b = new BoolVector(true, false, true);

        set.Add(a);
        set.ShouldContain(b);
    }

    [Fact]
    public void HashSet_AddEquivalentVector_DoesNotIncreaseCount()
    {
        var set = new HashSet<BoolVector>();
        BoolVector a = new BoolVector(true, false, true);
        BoolVector b = new BoolVector(true, false, true);

        set.Add(a);
        set.Add(b);

        set.ShouldHaveSingleItem();
    }

    [Fact]
    public void Dictionary_CanUseEquivalentVectorAsKey()
    {
        var dict = new Dictionary<BoolVector, string>();
        BoolVector key1 = new BoolVector(true, false, true);
        BoolVector key2 = new BoolVector(true, false, true);

        dict[key1] = "value";

        dict.ContainsKey(key2).ShouldBeTrue();
        dict[key2].ShouldBe("value");
    }

    [Fact]
    public void Equality_EmptyVectors_AreEqual_AndHaveSameHashCode()
    {
        var a = new BoolVector();
        var b = new BoolVector();

        a.Equals(b).ShouldBeTrue();
        b.GetHashCode().ShouldBe(a.GetHashCode());
    }

    [Fact]
    public void ToString_FormatsValuesAsTrueAndFalse()
    {
        var vector = new BoolVector(true, false, true);

        vector.ToString().ShouldBe("[True, False, True]");
    }

    [Fact]
    public void All_OnEmptyVector_ReturnsTrue()
    {
        var vector = new BoolVector();

        vector.All().ShouldBeTrue();
    }

    [Fact]
    public void Any_OnEmptyVector_ReturnsFalse()
    {
        var vector = new BoolVector();

        vector.Any().ShouldBeFalse();
    }

    [Fact]
    public void TrueCount_OnEmptyVector_ReturnsZero()
    {
        var vector = new BoolVector();

        vector.TrueCount().ShouldBe(0);
    }

    [Fact]
    public void ImplicitConversion_FromArray_CreatesVectorWithValues()
    {
        BoolVector vector = BoolVector.Create(true, false, true);

        vector.Count.ShouldBe(3);
        vector.ToArray().ShouldBe(new[] { true, false, true });
    }
}
