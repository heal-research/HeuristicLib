using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Genotypes.Vectors;

public sealed class PermutationTests
{
    [Fact]
    public void Create_FromArray_CopiesElements()
    {
        var elements = new[] { 2, 0, 1 };

        var permutation = Permutation.Create(elements);
        elements[0] = 1;

        permutation.ToArray().ShouldBe(new[] { 2, 0, 1 });
    }

    [Fact]
    public void Create_FromEnumerable_CopiesElements()
    {
        var elements = new List<int> { 2, 0, 1 };

        var permutation = Permutation.Create(elements);
        elements[0] = 1;

        permutation.ToArray().ShouldBe(new[] { 2, 0, 1 });
    }

    [Fact]
    public void FromOwnedArray_UsesProvidedArray()
    {
        var elements = new[] { 2, 0, 1 };

        var permutation = Permutation.FromOwnedArray(elements);
        elements[0] = 1;

        permutation.ToArray().ShouldBe(new[] { 1, 0, 1 });
    }

    [Fact]
    public void Constructor_ValidPermutation_CreatesInstance()
    {
        Permutation permutation = Permutation.Create(2, 0, 1, 3);

        permutation.Count.ShouldBe(4);
        permutation.ToArray().ShouldBe(new[] { 2, 0, 1, 3 });
    }

    [Fact]
    public void Constructor_EmptyPermutation_IsValid()
    {
        Permutation permutation = Permutation.Create();

        permutation.Count.ShouldBe(0);
        permutation.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WithDuplicate_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Permutation(0, 1, 1));
    }

    [Fact]
    public void Constructor_WithNegativeValue_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Permutation(0, -1, 1));
    }

    [Fact]
    public void Constructor_WithValueTooLarge_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Permutation(0, 1, 3));
    }

    [Fact]
    public void Constructor_WithMissingValue_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Permutation(0, 2));
    }

    [Fact]
    public void ImplicitConversion_FromArray_CreatesPermutation()
    {
        Permutation permutation = Permutation.Create(1, 0, 2);

        permutation.Count.ShouldBe(3);
        permutation.ToArray().ShouldBe(new[] { 1, 0, 2 });
    }

    [Fact]
    public void Range_CreatesIdentityPermutation()
    {
        var permutation = Permutation.Range(5);

        permutation.ToArray().ShouldBe(new[] { 0, 1, 2, 3, 4 });
    }

    [Fact]
    public void Count_Indexer_AndEnumeration_WorkCorrectly()
    {
        Permutation permutation = Permutation.Create(2, 0, 1);

        permutation.Count.ShouldBe(3);
        permutation[0].ShouldBe(2);
        permutation[1].ShouldBe(0);
        permutation[2].ShouldBe(1);
        permutation.ToArray().ShouldBe(new[] { 2, 0, 1 });
    }

    [Fact]
    public void Indexer_WithIndexFromEnd_WorksCorrectly()
    {
        Permutation permutation = Permutation.Create(2, 0, 1);

        permutation[^1].ShouldBe(1);
        permutation[^2].ShouldBe(0);
        permutation[^3].ShouldBe(2);
    }

    [Fact]
    public void Contains_ReturnsTrue_ForContainedValue()
    {
        Permutation permutation = Permutation.Create(2, 0, 1);

        permutation.Contains(0).ShouldBeTrue();
        permutation.Contains(1).ShouldBeTrue();
        permutation.Contains(2).ShouldBeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_ForMissingValue()
    {
        Permutation permutation = Permutation.Create(2, 0, 1);

        permutation.Contains(3).ShouldBeFalse();
        permutation.Contains(-1).ShouldBeFalse();
    }

    [Fact]
    public void Enumerator_EnumeratesAllElementsInOrder()
    {
        Permutation permutation = Permutation.Create(3, 1, 0, 2);

        var values = new List<int>();
        foreach (var value in permutation)
        {
            values.Add(value);
        }

        values.ShouldBe(new[] { 3, 1, 0, 2 });
    }

    [Fact]
    public void Enumerator_Reset_RewindsEnumeration()
    {
        Permutation permutation = Permutation.Create(2, 1, 0);
        using var enumerator = permutation.GetEnumerator();

        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBe(2);

        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBe(1);

        enumerator.Reset();

        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBe(2);
    }

    [Fact]
    public void FromOwnedArray_ValidPermutation_CreatesInstance()
    {
        var elements = new[] { 1, 2, 0 };

        var permutation = Permutation.FromOwnedArray(elements);

        permutation.ToArray().ShouldBe(new[] { 1, 2, 0 });
    }

    [Fact]
    public void FromOwnedArray_InvalidPermutation_ThrowsArgumentException()
    {
        var elements = new[] { 1, 1, 0 };

        Should.Throw<ArgumentException>(() => Permutation.FromOwnedArray(elements));
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(0, -1, 1)]
    [InlineData(0, 1, 3)]
    [InlineData(0, 2)]
    public void Create_InvalidPermutation_ThrowsArgumentException(params int[] elements)
    {
        Should.Throw<ArgumentException>(() => Permutation.Create(elements));
    }

    [Fact]
    public void RecordEquality_SameReference_ReturnsTrue()
    {
        Permutation permutation = Permutation.Create(1, 0, 2);

        permutation.Equals(permutation).ShouldBeTrue();
#pragma warning disable CS1718
        (permutation == permutation).ShouldBeTrue();
        (permutation != permutation).ShouldBeFalse();
#pragma warning restore CS1718
    }

    [Fact]
    public void RecordEquality_Null_ReturnsFalse()
    {
        Permutation permutation = Permutation.Create(1, 0, 2);

        permutation.Equals(null).ShouldBeFalse();
        permutation.Equals((object?)null).ShouldBeFalse();
    }

    [Fact]
    public void Equality_SameElementsConstructedSeparately_ShouldBeEqual()
    {
        Permutation a = Permutation.Create(2, 0, 1, 3);
        Permutation b = Permutation.Create(2, 0, 1, 3);

        a.Equals(b).ShouldBeTrue();
        b.Equals(a).ShouldBeTrue();
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void Equality_DifferentElements_ReturnsFalse()
    {
        Permutation a = Permutation.Create(2, 0, 1, 3);
        Permutation b = Permutation.Create(2, 1, 0, 3);

        a.Equals(b).ShouldBeFalse();
        b.Equals(a).ShouldBeFalse();
        (a == b).ShouldBeFalse();
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_IsTransitive()
    {
        Permutation a = Permutation.Create(2, 0, 1, 3);
        Permutation b = Permutation.Create(2, 0, 1, 3);
        Permutation c = Permutation.Create(2, 0, 1, 3);

        a.Equals(b).ShouldBeTrue();
        b.Equals(c).ShouldBeTrue();
        a.Equals(c).ShouldBeTrue();
    }

    [Fact]
    public void GetHashCode_EqualPermutations_ShouldHaveSameHashCode()
    {
        Permutation a = Permutation.Create(2, 0, 1, 3);
        Permutation b = Permutation.Create(2, 0, 1, 3);

        b.GetHashCode().ShouldBe(a.GetHashCode());
    }

    [Fact]
    public void GetHashCode_SameInstance_IsStable()
    {
        Permutation permutation = Permutation.Create(2, 0, 1, 3);

        var h1 = permutation.GetHashCode();
        var h2 = permutation.GetHashCode();

        h2.ShouldBe(h1);
    }

    [Fact]
    public void HashSet_ContainsEquivalentPermutation()
    {
        var set = new HashSet<Permutation>();
        Permutation a = Permutation.Create(2, 0, 1, 3);
        Permutation b = Permutation.Create(2, 0, 1, 3);

        set.Add(a);
        set.ShouldContain(b);
    }

    [Fact]
    public void HashSet_AddEquivalentPermutation_DoesNotIncreaseCount()
    {
        var set = new HashSet<Permutation>();
        Permutation a = Permutation.Create(2, 0, 1, 3);
        Permutation b = Permutation.Create(2, 0, 1, 3);

        set.Add(a);
        set.Add(b);

        set.ShouldHaveSingleItem();
    }

    [Fact]
    public void Dictionary_CanUseEquivalentPermutationAsKey()
    {
        var dict = new Dictionary<Permutation, string>();
        Permutation key1 = Permutation.Create(2, 0, 1, 3);
        Permutation key2 = Permutation.Create(2, 0, 1, 3);

        dict[key1] = "value";

        dict.ContainsKey(key2).ShouldBeTrue();
        dict[key2].ShouldBe("value");
    }

    [Fact]
    public void CreateRandom_LengthZero_ReturnsEmptyPermutation()
    {
        var rng = new StubRandomNumberGenerator();

        var permutation = Permutation.CreateRandom(0, rng);

        permutation.ShouldBeEmpty();
    }

    [Fact]
    public void CreateRandom_ReturnsValidPermutationOfRequestedLength()
    {
        var rng = new StubRandomNumberGenerator(0.0, 0.0, 0.0, 0.0);

        var permutation = Permutation.CreateRandom(5, rng);

        permutation.Count.ShouldBe(5);
        permutation.OrderBy(x => x).ShouldBe(Enumerable.Range(0, 5).OrderBy(x => x));
    }

    [Fact]
    public void CreateRandom_WithAlwaysZeroIndices_ProducesDeterministicPermutation()
    {
        var rng = new StubRandomNumberGenerator(0.0, 0.0, 0.0);

        var permutation = Permutation.CreateRandom(4, rng);

        permutation.ToArray().ShouldBe(new[] { 1, 2, 3, 0 });
    }

    [Fact]
    public void SwapRandomElements_WhenIndicesDiffer_SwapsThem()
    {
        Permutation permutation = Permutation.Create(0, 1, 2, 3);
        var rng = new StubRandomNumberGenerator(0.3, 0.9);

        var result = Permutation.SwapRandomElements(permutation, rng);

        result.ToArray().ShouldBe(new[] { 0, 3, 2, 1 });
        permutation.ToArray().ShouldBe(new[] { 0, 1, 2, 3 });
    }

    [Fact]
    public void SwapRandomElements_WhenIndicesEqual_ReturnsEqualPermutation()
    {
        Permutation permutation = Permutation.Create(0, 1, 2, 3);
        var rng = new StubRandomNumberGenerator(0.6, 0.6);

        var result = Permutation.SwapRandomElements(permutation, rng);

        result.ToArray().ShouldBe(new[] { 0, 1, 2, 3 });
        result.ShouldBe(permutation);
    }

    [Fact]
    public void SwapRandomElements_ResultIsStillValidPermutation()
    {
        Permutation permutation = Permutation.Create(3, 1, 0, 2);
        var rng = new StubRandomNumberGenerator(0.0, 0.6);

        var result = Permutation.SwapRandomElements(permutation, rng);

        result.Count.ShouldBe(4);
        result.OrderBy(x => x).ShouldBe(new[] { 0, 1, 2, 3 });
    }

    private sealed class StubRandomNumberGenerator : IRandomNumberGenerator
    {
        private readonly Queue<double> values;

        public StubRandomNumberGenerator(params double[] values)
        {
            this.values = new Queue<double>(values);
        }

        public int NextInt() => throw new NotSupportedException();

        public IRandomNumberGenerator Fork(ulong forkKey) => throw new NotSupportedException();

        public double NextDouble() => values.Count == 0 ? 0.0 : values.Dequeue();
    }
}
