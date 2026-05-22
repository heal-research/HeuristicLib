using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Genotypes.Vectors;

public sealed class IntegerVectorTests
{
    [Fact]
    public void Create_FromArray_CopiesElements()
    {
        var elements = new[] { 1, 2, 3 };

        var vector = IntegerVector.Create(elements);
        elements[0] = 99;

        vector.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Create_FromEnumerable_CopiesElements()
    {
        var elements = new List<int> { 1, 2, 3 };

        var vector = IntegerVector.Create(elements);
        elements[0] = 99;

        vector.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void FromOwnedArray_UsesProvidedArray()
    {
        var elements = new[] { 1, 2, 3 };

        var vector = IntegerVector.FromOwnedArray(elements);
        elements[0] = 99;

        vector.ToArray().ShouldBe(new[] { 99, 2, 3 });
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        IntegerVector v = IntegerVector.Create(1, 2, 3);

        v.Equals(v).ShouldBeTrue();
        (v == v).ShouldBeTrue();
        (v != v).ShouldBeFalse();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        IntegerVector v = IntegerVector.Create(1, 2, 3);

        v.Equals(null).ShouldBeFalse();
        v.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_ObjectOfDifferentType_ReturnsFalse()
    {
        IntegerVector v = IntegerVector.Create(1, 2, 3);

        v.Equals("not a vector").ShouldBeFalse();
    }

    [Fact]
    public void Equals_SameElements_ReturnsTrue()
    {
        IntegerVector a = IntegerVector.Create(1, 2, 3);
        IntegerVector b = IntegerVector.Create(1, 2, 3);

        a.Equals(b).ShouldBeTrue();
        b.Equals(a).ShouldBeTrue();
        a.Equals((object)b).ShouldBeTrue();
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentLengths_ReturnsFalse()
    {
        IntegerVector a = IntegerVector.Create(1, 2, 3);
        IntegerVector b = IntegerVector.Create(1, 2);

        a.Equals(b).ShouldBeFalse();
        b.Equals(a).ShouldBeFalse();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_DifferentElements_ReturnsFalse()
    {
        IntegerVector a = IntegerVector.Create(1, 2, 3);
        IntegerVector b = IntegerVector.Create(1, 2, 4);

        a.Equals(b).ShouldBeFalse();
        b.Equals(a).ShouldBeFalse();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equals_IsTransitive()
    {
        IntegerVector a = IntegerVector.Create(1, 2, 3);
        IntegerVector b = IntegerVector.Create(1, 2, 3);
        IntegerVector c = IntegerVector.Create(1, 2, 3);

        a.Equals(b).ShouldBeTrue();
        b.Equals(c).ShouldBeTrue();
        a.Equals(c).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        IntegerVector? a = null;
        IntegerVector? b = null;

        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void EqualityOperator_LeftNull_ReturnsFalse()
    {
        IntegerVector? a = null;
        IntegerVector b = IntegerVector.Create(1, 2, 3);

        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void EqualityOperator_RightNull_ReturnsFalse()
    {
        IntegerVector a = IntegerVector.Create(1, 2, 3);
        IntegerVector? b = null;

        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_EqualVectors_HaveSameHashCode()
    {
        IntegerVector a = IntegerVector.Create(1, 2, 3);
        IntegerVector b = IntegerVector.Create(1, 2, 3);

        b.GetHashCode().ShouldBe(a.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameInstance_IsStable()
    {
        IntegerVector v = IntegerVector.Create(1, 2, 3);

        var h1 = v.GetHashCode();
        var h2 = v.GetHashCode();

        h2.ShouldBe(h1);
    }

    [Fact]
    public void HashSet_ContainsEquivalentVector()
    {
        var set = new HashSet<IntegerVector>();
        IntegerVector a = IntegerVector.Create(1, 2, 3);
        IntegerVector b = IntegerVector.Create(1, 2, 3);

        set.Add(a);

        set.Contains(b).ShouldBeTrue();
    }

    [Fact]
    public void HashSet_AddEquivalentVector_DoesNotIncreaseCount()
    {
        var set = new HashSet<IntegerVector>();
        IntegerVector a = IntegerVector.Create(1, 2, 3);
        IntegerVector b = IntegerVector.Create(1, 2, 3);

        set.Add(a);
        set.Add(b);

        set.ShouldHaveSingleItem();
    }

    [Fact]
    public void Dictionary_CanUseEquivalentVectorAsKey()
    {
        var dict = new Dictionary<IntegerVector, string>();
        IntegerVector key1 = IntegerVector.Create(1, 2, 3);
        IntegerVector key2 = IntegerVector.Create(1, 2, 3);

        dict[key1] = "value";

        dict.ContainsKey(key2).ShouldBeTrue();
        dict[key2].ShouldBe("value");
    }

    [Fact]
    public void Count_Indexer_AndEnumeration_WorkCorrectly()
    {
        IntegerVector v = IntegerVector.Create(10, 20, 30);

        v.Count.ShouldBe(3);
        v[0].ShouldBe(10);
        v[1].ShouldBe(20);
        v[2].ShouldBe(30);
        v.ToArray().ShouldBe(new[] { 10, 20, 30 });
    }

    [Fact]
    public void Indexer_WithIndexFromEnd_WorksCorrectly()
    {
        IntegerVector v = IntegerVector.Create(10, 20, 30);

        v[^1].ShouldBe(30);
        v[^2].ShouldBe(20);
        v[^3].ShouldBe(10);
    }

    [Fact]
    public void ImplicitConversion_FromScalar_CreatesSingleElementVector()
    {
        IntegerVector v = 42;

        v.Count.ShouldBe(1);
        v[0].ShouldBe(42);
    }

    [Fact]
    public void ImplicitConversion_FromArray_CreatesVectorWithArrayValues()
    {
        IntegerVector v = IntegerVector.Create(1, 2, 3);

        v.Count.ShouldBe(3);
        v.ToArray().ShouldBe(new[] { 1, 2, 3 });
    }

    [Fact]
    public void ImplicitConversion_ToRealVector_ConvertsAllElementsToDouble()
    {
        IntegerVector input = IntegerVector.Create(1, -2, 3);

        RealVector result = input;

        result.ToArray().ShouldBe(new[] { 1.0, -2.0, 3.0 });
    }

    [Fact]
    public void ToRealVector_ConvertsAllElementsToDouble()
    {
        IntegerVector input = IntegerVector.Create(1, -2, 3);

        var result = input.ToRealVector();

        result.ToArray().ShouldBe(new[] { 1.0, -2.0, 3.0 });
    }

    [Fact]
    public void ToRealAt_ReturnsElementAsDouble()
    {
        IntegerVector input = IntegerVector.Create(1, -2, 3);

        input.ToRealAt(1).ShouldBe(-2.0);
    }

    [Fact]
    public void AreCompatible_ReturnsTrue_ForSameLength()
    {
        IntegerVector a = IntegerVector.Create(1, 2);
        IntegerVector b = IntegerVector.Create(3, 4);

        IntegerVector.AreCompatible(a, b).ShouldBeTrue();
    }

    [Fact]
    public void AreCompatible_ReturnsTrue_WhenLeftIsScalar()
    {
        IntegerVector a = 1;
        IntegerVector b = IntegerVector.Create(3, 4, 5);

        IntegerVector.AreCompatible(a, b).ShouldBeTrue();
    }

    [Fact]
    public void AreCompatible_ReturnsTrue_WhenRightIsScalar()
    {
        IntegerVector a = IntegerVector.Create(3, 4, 5);
        IntegerVector b = 1;

        IntegerVector.AreCompatible(a, b).ShouldBeTrue();
    }

    [Fact]
    public void AreCompatible_ReturnsFalse_ForDifferentNonScalarLengths()
    {
        IntegerVector a = IntegerVector.Create(1, 2);
        IntegerVector b = IntegerVector.Create(3, 4, 5);

        IntegerVector.AreCompatible(a, b).ShouldBeFalse();
    }

    [Fact]
    public void BroadcastLength_ReturnsMaxLength()
    {
        IntegerVector scalar = 1;
        IntegerVector vector = IntegerVector.Create(3, 4, 5);

        IntegerVector.BroadcastLength(scalar, vector).ShouldBe(3);
        IntegerVector.BroadcastLength(vector, scalar).ShouldBe(3);
    }

    [Fact]
    public void Add_SameLength_AddsElementwise()
    {
        IntegerVector a = IntegerVector.Create(1, 2, 3);
        IntegerVector b = IntegerVector.Create(10, 20, 30);

        var result = IntegerVector.Add(a, b);

        result.ToArray().ShouldBe(new[] { 11, 22, 33 });
    }

    [Fact]
    public void Add_BroadcastsScalarLeft()
    {
        IntegerVector scalar = 2;
        IntegerVector vector = IntegerVector.Create(10, 20, 30);

        var result = IntegerVector.Add(scalar, vector);

        result.ToArray().ShouldBe(new[] { 12, 22, 32 });
    }

    [Fact]
    public void Add_BroadcastsScalarRight()
    {
        IntegerVector vector = IntegerVector.Create(10, 20, 30);
        IntegerVector scalar = 2;

        var result = IntegerVector.Add(vector, scalar);

        result.ToArray().ShouldBe(new[] { 12, 22, 32 });
    }

    [Fact]
    public void Add_IncompatibleLengths_Throws()
    {
        IntegerVector a = IntegerVector.Create(1, 2);
        IntegerVector b = IntegerVector.Create(10, 20, 30);

        Should.Throw<ArgumentException>(() => IntegerVector.Add(a, b));
    }

    [Fact]
    public void Subtract_SameLength_SubtractsElementwise()
    {
        IntegerVector a = IntegerVector.Create(10, 20, 30);
        IntegerVector b = IntegerVector.Create(1, 2, 3);

        var result = IntegerVector.Subtract(a, b);

        result.ToArray().ShouldBe(new[] { 9, 18, 27 });
    }

    [Fact]
    public void Multiply_SameLength_MultipliesElementwise()
    {
        IntegerVector a = IntegerVector.Create(2, 3, 4);
        IntegerVector b = IntegerVector.Create(10, 20, 30);

        var result = IntegerVector.Multiply(a, b);

        result.ToArray().ShouldBe(new[] { 20, 60, 120 });
    }

    [Fact]
    public void Divide_SameLength_DividesElementwise()
    {
        IntegerVector a = IntegerVector.Create(10, 20, 30);
        IntegerVector b = IntegerVector.Create(2, 4, 5);

        var result = IntegerVector.Divide(a, b);

        result.ToArray().ShouldBe(new[] { 5, 5, 6 });
    }

    [Fact]
    public void Operators_DelegateToArithmeticMethods()
    {
        IntegerVector a = IntegerVector.Create(10, 20, 30);
        IntegerVector b = IntegerVector.Create(2, 4, 5);

        (a + b).ToArray().ShouldBe(new[] { 12, 24, 35 });
        (a - b).ToArray().ShouldBe(new[] { 8, 16, 25 });
        (a * b).ToArray().ShouldBe(new[] { 20, 80, 150 });
        (a / b).ToArray().ShouldBe(new[] { 5, 5, 6 });
    }

    [Fact]
    public void Clamp_ScalarBounds_ClampsValues()
    {
        IntegerVector input = IntegerVector.Create(-1, 2, 10);
        IntegerVector min = 0;
        IntegerVector max = 5;

        var result = IntegerVector.Clamp(input, min, max);

        result.ToArray().ShouldBe(new[] { 0, 2, 5 });
        result.ShouldNotBeSameAs(input);
    }

    [Fact]
    public void ClampAt_UsesDimensionBounds()
    {
        IntegerVector input = IntegerVector.Create(-1, 2, 10);
        IntegerVector min = IntegerVector.Create(0, 1, 2);
        IntegerVector max = IntegerVector.Create(5, 3, 8);

        input.ClampAt(min, max, 0).ShouldBe(0);
        input.ClampAt(min, max, 1).ShouldBe(2);
        input.ClampAt(min, max, 2).ShouldBe(8);
    }

    [Fact]
    public void GreaterThan_SameLength_WorksElementwise()
    {
        IntegerVector a = IntegerVector.Create(1, 5, 3);
        IntegerVector b = IntegerVector.Create(2, 5, 1);

        (a > b).ToArray().ShouldBe(new[] { false, false, true });
    }

    [Fact]
    public void LessThan_SameLength_WorksElementwise()
    {
        IntegerVector a = IntegerVector.Create(1, 5, 3);
        IntegerVector b = IntegerVector.Create(2, 5, 4);

        (a < b).ToArray().ShouldBe(new[] { true, false, true });
    }

    [Fact]
    public void GreaterThanOrEqual_SameLength_WorksElementwise()
    {
        IntegerVector a = IntegerVector.Create(1, 5, 3);
        IntegerVector b = IntegerVector.Create(2, 5, 1);

        (a >= b).ToArray().ShouldBe(new[] { false, true, true });
    }

    [Fact]
    public void LessThanOrEqual_SameLength_WorksElementwise()
    {
        IntegerVector a = IntegerVector.Create(1, 5, 3);
        IntegerVector b = IntegerVector.Create(2, 5, 3);

        (a <= b).ToArray().ShouldBe(new[] { true, true, true });
    }

    [Fact]
    public void ComparisonOperators_BroadcastScalarLeft()
    {
        IntegerVector scalar = 3;
        IntegerVector vector = IntegerVector.Create(1, 3, 5);

        (scalar > vector).ToArray().ShouldBe(new[] { true, false, false });
        (scalar < vector).ToArray().ShouldBe(new[] { false, false, true });
        (scalar >= vector).ToArray().ShouldBe(new[] { true, true, false });
        (scalar <= vector).ToArray().ShouldBe(new[] { false, true, true });
    }

    [Fact]
    public void ComparisonOperators_BroadcastScalarRight()
    {
        IntegerVector vector = IntegerVector.Create(1, 3, 5);
        IntegerVector scalar = 3;

        (vector > scalar).ToArray().ShouldBe(new[] { false, false, true });
        (vector < scalar).ToArray().ShouldBe(new[] { true, false, false });
        (vector >= scalar).ToArray().ShouldBe(new[] { false, true, true });
        (vector <= scalar).ToArray().ShouldBe(new[] { true, true, false });
    }

    [Fact]
    public void ComparisonOperators_IncompatibleLengths_ThrowArgumentException()
    {
        IntegerVector a = IntegerVector.Create(1, 2);
        IntegerVector b = IntegerVector.Create(1, 2, 3);

        Should.Throw<ArgumentException>(() => a > b);
        Should.Throw<ArgumentException>(() => a < b);
        Should.Throw<ArgumentException>(() => a >= b);
        Should.Throw<ArgumentException>(() => a <= b);
    }

    [Fact]
    public void CreateUniform_ReturnsVectorOfRequestedLength()
    {
        var rng = new StubRandomNumberGenerator(0.1, 0.2, 0.3);

        IntegerVector low = 0;
        IntegerVector high = 10;

        var result = IntegerVector.CreateUniform(3, low, high, rng);

        result.Count.ShouldBe(3);
    }

    [Fact]
    public void CreateUniform_MapsUniformDrawsIntoScalarBounds()
    {
        var rng = new StubRandomNumberGenerator(0.9, 0.5, 0.1);

        IntegerVector low = 10;
        IntegerVector high = 12;

        var result = IntegerVector.CreateUniform(3, low, high, rng);

        result.ToArray().ShouldBe(new[] { 12, 11, 10 });
    }

    [Fact]
    public void CreateUniform_UsesElementwiseBounds()
    {
        var rng = new StubRandomNumberGenerator(0.9, 0.9, 0.9);

        IntegerVector low = IntegerVector.Create(10, 20, 30);
        IntegerVector high = IntegerVector.Create(12, 22, 32);

        var result = IntegerVector.CreateUniform(3, low, high, rng);

        result.ToArray().ShouldBe(new[] { 12, 22, 32 });
    }

    [Fact]
    public void CreateUniform_LowLengthMismatch_ThrowsArgumentException()
    {
        var rng = new StubRandomNumberGenerator(0.1, 0.2, 0.3);

        IntegerVector low = IntegerVector.Create(0, 1);
        IntegerVector high = 10;

        Should.Throw<ArgumentException>(() => IntegerVector.CreateUniform(3, low, high, rng));
    }

    [Fact]
    public void CreateUniform_HighLengthMismatch_ThrowsArgumentException()
    {
        var rng = new StubRandomNumberGenerator(0.1, 0.2, 0.3);

        IntegerVector low = 0;
        IntegerVector high = IntegerVector.Create(10, 11);

        Should.Throw<ArgumentException>(() => IntegerVector.CreateUniform(3, low, high, rng));
    }

    [Fact]
    public void Equality_EmptyVectors_AreEqual_AndHaveSameHashCode()
    {
        IntegerVector a = IntegerVector.Create(Array.Empty<int>());
        IntegerVector b = IntegerVector.Create(Array.Empty<int>());

        a.Equals(b).ShouldBeTrue();
        b.GetHashCode().ShouldBe(a.GetHashCode());
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

            if (doubles.Count == 0)
            {
                throw new InvalidOperationException("No more test doubles available.");
            }

            return doubles.Dequeue();
        }
    }
}
