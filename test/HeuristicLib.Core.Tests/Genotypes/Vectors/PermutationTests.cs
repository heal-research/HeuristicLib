using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Genotypes.Vectors;

public sealed class PermutationTests
{
  [Fact]
  public void Constructor_ValidPermutation_CreatesInstance()
  {
    Permutation permutation = new[] { 2, 0, 1, 3 };

    Assert.Equal(4, permutation.Count);
    Assert.Equal(new[] { 2, 0, 1, 3 }, permutation.ToArray());
  }

  [Fact]
  public void Constructor_EmptyPermutation_IsValid()
  {
    Permutation permutation = Array.Empty<int>();

    Assert.Equal(0, permutation.Count);
    Assert.Empty(permutation);
  }

  [Fact]
  public void Constructor_WithDuplicate_ThrowsArgumentException()
  {
    Assert.Throws<ArgumentException>(() => new Permutation(0, 1, 1));
  }

  [Fact]
  public void Constructor_WithNegativeValue_ThrowsArgumentException()
  {
    Assert.Throws<ArgumentException>(() => new Permutation(0, -1, 1));
  }

  [Fact]
  public void Constructor_WithValueTooLarge_ThrowsArgumentException()
  {
    Assert.Throws<ArgumentException>(() => new Permutation(0, 1, 3));
  }

  [Fact]
  public void Constructor_WithMissingValue_ThrowsArgumentException()
  {
    Assert.Throws<ArgumentException>(() => new Permutation(0, 2));
  }

  [Fact]
  public void ImplicitConversion_FromArray_CreatesPermutation()
  {
    Permutation permutation = new[] { 1, 0, 2 };

    Assert.Equal(3, permutation.Count);
    Assert.Equal(new[] { 1, 0, 2 }, permutation.ToArray());
  }

  [Fact]
  public void Range_CreatesIdentityPermutation()
  {
    var permutation = Permutation.Range(5);

    Assert.Equal(new[] { 0, 1, 2, 3, 4 }, permutation.ToArray());
  }

  [Fact]
  public void Count_Indexer_AndEnumeration_WorkCorrectly()
  {
    Permutation permutation = new[] { 2, 0, 1 };

    Assert.Equal(3, permutation.Count);
    Assert.Equal(2, permutation[0]);
    Assert.Equal(0, permutation[1]);
    Assert.Equal(1, permutation[2]);
    Assert.Equal(new[] { 2, 0, 1 }, permutation.ToArray());
  }

  [Fact]
  public void Indexer_WithIndexFromEnd_WorksCorrectly()
  {
    Permutation permutation = new[] { 2, 0, 1 };

    Assert.Equal(1, permutation[^1]);
    Assert.Equal(0, permutation[^2]);
    Assert.Equal(2, permutation[^3]);
  }

  [Fact]
  public void Contains_ReturnsTrue_ForContainedValue()
  {
    Permutation permutation = new[] { 2, 0, 1 };

    Assert.True(permutation.Contains(0));
    Assert.True(permutation.Contains(1));
    Assert.True(permutation.Contains(2));
  }

  [Fact]
  public void Contains_ReturnsFalse_ForMissingValue()
  {
    Permutation permutation = new[] { 2, 0, 1 };

    Assert.False(permutation.Contains(3));
    Assert.False(permutation.Contains(-1));
  }

  [Fact]
  public void Enumerator_EnumeratesAllElementsInOrder()
  {
    Permutation permutation = new[] { 3, 1, 0, 2 };

    var values = new List<int>();
    foreach (var value in permutation) {
      values.Add(value);
    }

    Assert.Equal(new[] { 3, 1, 0, 2 }, values);
  }

  [Fact]
  public void Enumerator_Reset_RewindsEnumeration()
  {
    Permutation permutation = new[] { 2, 1, 0 };
    var enumerator = permutation.GetEnumerator();

    Assert.True(enumerator.MoveNext());
    Assert.Equal(2, enumerator.Current);

    Assert.True(enumerator.MoveNext());
    Assert.Equal(1, enumerator.Current);

    enumerator.Reset();

    Assert.True(enumerator.MoveNext());
    Assert.Equal(2, enumerator.Current);
  }

  [Fact]
  public void Span_ExposesElementsInOrder()
  {
    Permutation permutation = new[] { 2, 0, 1 };

    Assert.True(permutation.Span.SequenceEqual(new[] { 2, 0, 1 }));
  }

  [Fact]
  public void FromMemory_ValidPermutation_CreatesInstance()
  {
    var memory = new[] { 1, 2, 0 }.AsMemory();

    var permutation = Permutation.FromMemory(memory);

    Assert.Equal(new[] { 1, 2, 0 }, permutation.ToArray());
  }

  [Fact]
  public void FromMemory_InvalidPermutation_ThrowsArgumentException()
  {
    var memory = new[] { 1, 1, 0 }.AsMemory();

    Assert.Throws<ArgumentException>(() => Permutation.FromMemory(memory));
  }

  [Fact]
  public void RecordEquality_SameReference_ReturnsTrue()
  {
    Permutation permutation = new[] { 1, 0, 2 };

    Assert.True(permutation.Equals(permutation));
    Assert.True(permutation == permutation);
    Assert.False(permutation != permutation);
  }

  [Fact]
  public void RecordEquality_Null_ReturnsFalse()
  {
    Permutation permutation = new[] { 1, 0, 2 };

    Assert.False(permutation.Equals(null));
    Assert.False(permutation.Equals((object?)null));
  }

  [Fact]
  public void RecordEquality_DifferentType_ReturnsFalse()
  {
    Permutation permutation = new[] { 1, 0, 2 };

    Assert.False(permutation.Equals("not a permutation"));
  }

  [Fact]
  public void Equality_SameElementsConstructedSeparately_ShouldBeEqual()
  {
    Permutation a = new[] { 2, 0, 1, 3 };
    Permutation b = new[] { 2, 0, 1, 3 };

    Assert.True(a.Equals(b));
    Assert.True(b.Equals(a));
    Assert.True(a == b);
    Assert.False(a != b);
  }

  [Fact]
  public void Equality_DifferentElements_ReturnsFalse()
  {
    Permutation a = new[] { 2, 0, 1, 3 };
    Permutation b = new[] { 2, 1, 0, 3 };

    Assert.False(a.Equals(b));
    Assert.False(b.Equals(a));
    Assert.False(a == b);
    Assert.True(a != b);
  }

  [Fact]
  public void Equality_IsTransitive()
  {
    Permutation a = new[] { 2, 0, 1, 3 };
    Permutation b = new[] { 2, 0, 1, 3 };
    Permutation c = new[] { 2, 0, 1, 3 };

    Assert.True(a.Equals(b));
    Assert.True(b.Equals(c));
    Assert.True(a.Equals(c));
  }

  [Fact]
  public void GetHashCode_EqualPermutations_ShouldHaveSameHashCode()
  {
    Permutation a = new[] { 2, 0, 1, 3 };
    Permutation b = new[] { 2, 0, 1, 3 };

    Assert.Equal(a.GetHashCode(), b.GetHashCode());
  }

  [Fact]
  public void GetHashCode_SameInstance_IsStable()
  {
    Permutation permutation = new[] { 2, 0, 1, 3 };

    var h1 = permutation.GetHashCode();
    var h2 = permutation.GetHashCode();

    Assert.Equal(h1, h2);
  }

  [Fact]
  public void HashSet_ContainsEquivalentPermutation()
  {
    var set = new HashSet<Permutation>();
    Permutation a = new[] { 2, 0, 1, 3 };
    Permutation b = new[] { 2, 0, 1, 3 };

    set.Add(a);
    Assert.Contains(b, set);
  }

  [Fact]
  public void HashSet_AddEquivalentPermutation_DoesNotIncreaseCount()
  {
    var set = new HashSet<Permutation>();
    Permutation a = new[] { 2, 0, 1, 3 };
    Permutation b = new[] { 2, 0, 1, 3 };

    set.Add(a);
    set.Add(b);

    Assert.Single(set);
  }

  [Fact]
  public void Dictionary_CanUseEquivalentPermutationAsKey()
  {
    var dict = new Dictionary<Permutation, string>();
    Permutation key1 = new[] { 2, 0, 1, 3 };
    Permutation key2 = new[] { 2, 0, 1, 3 };

    dict[key1] = "value";

    Assert.True(dict.ContainsKey(key2));
    Assert.Equal("value", dict[key2]);
  }

  [Fact]
  public void CreateRandom_LengthZero_ReturnsEmptyPermutation()
  {
    var rng = new StubRandomNumberGenerator();

    var permutation = Permutation.CreateRandom(0, rng);

    Assert.Empty(permutation);
  }

  [Fact]
  public void CreateRandom_ReturnsValidPermutationOfRequestedLength()
  {
    var rng = new StubRandomNumberGenerator(0.0, 0.0, 0.0, 0.0);

    var permutation = Permutation.CreateRandom(5, rng);

    Assert.Equal(5, permutation.Count);
    Assert.Equal(Enumerable.Range(0, 5).OrderBy(x => x), permutation.OrderBy(x => x));
  }

  [Fact]
  public void CreateRandom_WithAlwaysZeroIndices_ProducesDeterministicPermutation()
  {
    var rng = new StubRandomNumberGenerator(0.0, 0.0, 0.0);

    var permutation = Permutation.CreateRandom(4, rng);

    Assert.Equal(new[] { 1, 2, 3, 0 }, permutation.ToArray());
  }

  [Fact]
  public void SwapRandomElements_WhenIndicesDiffer_SwapsThem()
  {
    Permutation permutation = new[] { 0, 1, 2, 3 };
    var rng = new StubRandomNumberGenerator(0.3, 0.9);

    var result = Permutation.SwapRandomElements(permutation, rng);

    Assert.Equal(new[] { 0, 3, 2, 1 }, result.ToArray());
    Assert.Equal(new[] { 0, 1, 2, 3 }, permutation.ToArray());
  }

  [Fact]
  public void SwapRandomElements_WhenIndicesEqual_ReturnsEqualPermutation()
  {
    Permutation permutation = new[] { 0, 1, 2, 3 };
    var rng = new StubRandomNumberGenerator(0.6, 0.6);

    var result = Permutation.SwapRandomElements(permutation, rng);

    Assert.Equal(new[] { 0, 1, 2, 3 }, result.ToArray());
    Assert.Equal(permutation, result);
  }

  [Fact]
  public void SwapRandomElements_ResultIsStillValidPermutation()
  {
    Permutation permutation = new[] { 3, 1, 0, 2 };
    var rng = new StubRandomNumberGenerator(0.0, 0.6);

    var result = Permutation.SwapRandomElements(permutation, rng);

    Assert.Equal(4, result.Count);
    Assert.Equal(new[] { 0, 1, 2, 3 }, result.OrderBy(x => x));
  }

  private sealed class StubRandomNumberGenerator : IRandomNumberGenerator
  {
    private readonly Queue<double> values;

    public StubRandomNumberGenerator(params double[] values)
    {
      this.values = new Queue<double>(values);
    }

    public int NextInt() => throw new NotSupportedException();

    public IRandomNumberGenerator Fork(ulong forkKey) => throw new NotImplementedException();

    public double NextDouble() => values.Count == 0 ? 0.0 : values.Dequeue();
  }
}
