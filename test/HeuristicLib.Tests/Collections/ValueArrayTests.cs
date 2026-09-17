using System.Runtime.InteropServices;

namespace HEAL.HeuristicLib.Tests.Collections;

public sealed class ValueArrayTests
{
    private sealed record Element(int Value);

    private sealed record Holder
    {
        public ValueArray<int> Items { get; init; }
    }

    [Fact]
    public void Equals_WithEqualContents_IsEqualAndSharesHashCode()
    {
        var left = ValueArray.Create(1, 2, 3);
        var right = ValueArray.Create(1, 2, 3);

        left.ShouldBe(right);
        (left == right).ShouldBeTrue();
        (left != right).ShouldBeFalse();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Equals_WithReorderedContents_IsNotEqual()
    {
        ValueArray.Create(1, 2, 3).ShouldNotBe(ValueArray.Create(3, 2, 1));
    }

    [Fact]
    public void Equals_WithDifferentLength_IsNotEqual()
    {
        ValueArray.Create(1, 2, 3).ShouldNotBe(ValueArray.Create(1, 2));
    }

    [Fact]
    public void Equals_WithPrefixOfLongerArray_IsNotEqual()
    {
        ValueArray.Create(1, 2).ShouldNotBe(ValueArray.Create(1, 2, 0));
    }

    [Fact]
    public void Equals_UsesElementEqualityRatherThanReferences()
    {
        var left = ValueArray.Create(new Element(1), new Element(2));
        var right = ValueArray.Create(new Element(1), new Element(2));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Equals_WithNullElements_ComparesThem()
    {
        var left = ValueArray.Create(null, new Element(1));
        var right = ValueArray.Create(null, new Element(1));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(ValueArray.Create(new Element(1), null));
    }

    [Fact]
    public void Equals_WithDifferentType_IsNotEqual()
    {
        ValueArray.Create(1, 2, 3).Equals("not a value array").ShouldBeFalse();
    }

    [Fact]
    public void Default_BehavesAsEmptyArray()
    {
        ValueArray<int> value = default;

        value.Count.ShouldBe(0);
        value.IsEmpty.ShouldBeTrue();
        value.AsSpan().Length.ShouldBe(0);
        value.AsImmutableArray().IsDefault.ShouldBeFalse();
        value.ToString().ShouldBe("[]");
        value.ShouldBeEmpty();
    }

    [Fact]
    public void Default_EqualsExplicitlyConstructedEmptyArray()
    {
        ValueArray<int> value = default;

        value.ShouldBe(ValueArray<int>.Empty);
        value.ShouldBe(ValueArray.Create<int>());
        value.GetHashCode().ShouldBe(ValueArray<int>.Empty.GetHashCode());
        value.GetHashCode().ShouldBe(ValueArray.Create<int>().GetHashCode());
    }

    [Fact]
    public void Default_FromDefaultImmutableArray_BehavesAsEmptyArray()
    {
        ImmutableArray<int> source = default;
        ValueArray<int> value = source;

        value.Count.ShouldBe(0);
        value.ShouldBe(ValueArray<int>.Empty);
        value.GetHashCode().ShouldBe(ValueArray<int>.Empty.GetHashCode());
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        var value = ValueArray.Create(1, 2, 3);

        Should.Throw<IndexOutOfRangeException>(() => value[3]);
        Should.Throw<IndexOutOfRangeException>(() => default(ValueArray<int>)[0]);
    }

    [Fact]
    public void Indexer_SupportsFromEndIndex()
    {
        var value = ValueArray.Create(1, 2, 3);

        value[0].ShouldBe(1);
        value[^1].ShouldBe(3);
    }

    [Fact]
    public void ToValueArray_FromMutableList_SnapshotsTheInput()
    {
        var source = new List<int> { 1, 2, 3 };
        var value = source.ToValueArray();

        source[0] = 99;
        source.Add(4);

        value.ShouldBe(ValueArray.Create(1, 2, 3));
    }

    [Fact]
    public void ToValueArray_FromReadOnlyListInterface_SnapshotsElementsRatherThanWrappingTheList()
    {
        // The shape every operator configuration uses: a params IReadOnlyList<T> parameter snapshotted into state.
        IReadOnlyList<int> source = new List<int> { 1, 2, 3 };

        source.ToValueArray().ShouldBe(ValueArray.Create(1, 2, 3));
    }

    [Fact]
    public void Create_BindsArraysAndSpansAsElements()
    {
        // An array or span binds to the ReadOnlySpan<T> parameter directly and supplies the elements.
        var fromArray = ValueArray.Create(1, 2, 3);
        var fromSpan = ValueArray.Create(new[] { 1, 2, 3 }.AsSpan());

        fromArray.ShouldBe(ValueArray.Create(1, 2, 3));
        fromSpan.ShouldBe(ValueArray.Create(1, 2, 3));
    }

    [Fact]
    public void Create_WrapsAnyOtherArgumentAsASingleElement()
    {
        // Nothing else infers an element type through the span parameter, so the argument itself is the element.
        var fromList = ValueArray.Create(new List<int> { 1, 2, 3 });
        var fromImmutableArray = ValueArray.Create(ImmutableArray.Create(1, 2, 3));
        var fromString = ValueArray.Create("abc");

        fromList.GetType().ShouldBe(typeof(ValueArray<List<int>>));
        fromList.Count.ShouldBe(1);
        fromImmutableArray.GetType().ShouldBe(typeof(ValueArray<ImmutableArray<int>>));
        fromImmutableArray.Count.ShouldBe(1);
        fromString.GetType().ShouldBe(typeof(ValueArray<string>));
        fromString.Count.ShouldBe(1);
    }

    [Fact]
    public void CollectionExpression_ConstructsAndSpreads()
    {
        ValueArray<int> value = [1, 2, 3];
        ValueArray<int> extended = [.. value, 4];

        value.ShouldBe(ValueArray.Create(1, 2, 3));
        extended.ShouldBe(ValueArray.Create(1, 2, 3, 4));
    }

    [Fact]
    public void ImplicitConversionFromImmutableArray_DoesNotCopy()
    {
        var source = ImmutableArray.Create(1, 2, 3);
        ValueArray<int> value = source;

        ImmutableCollectionsMarshal.AsArray(value.AsImmutableArray())
            .ShouldBeSameAs(ImmutableCollectionsMarshal.AsArray(source));
    }

    [Fact]
    public void ToValueArray_FromImmutableArrayAsEnumerable_DoesNotCopy()
    {
        var source = ImmutableArray.Create(1, 2, 3);
        IEnumerable<int> enumerable = source;

        var value = enumerable.ToValueArray();

        ImmutableCollectionsMarshal.AsArray(value.AsImmutableArray())
            .ShouldBeSameAs(ImmutableCollectionsMarshal.AsArray(source));
    }

    [Fact]
    public void ToValueArray_FromValueArrayPassedAsAnInterface_DoesNotCopy()
    {
        // The shape every configuration constructor sees: a ValueArray arriving through an IReadOnlyList parameter.
        var source = ValueArray.Create(1, 2, 3);
        IReadOnlyList<int> asReadOnlyList = source;
        IEnumerable<int> asEnumerable = source;

        var backing = ImmutableCollectionsMarshal.AsArray(source.AsImmutableArray());
        ImmutableCollectionsMarshal.AsArray(asReadOnlyList.ToValueArray().AsImmutableArray()).ShouldBeSameAs(backing);
        ImmutableCollectionsMarshal.AsArray(asEnumerable.ToValueArray().AsImmutableArray()).ShouldBeSameAs(backing);
    }

    [Fact]
    public void FromOwnedArray_UsesProvidedArray()
    {
        var elements = new[] { 1, 2, 3 };
        var value = ValueArray.FromOwnedArray(elements);

        elements[0] = 99;

        value[0].ShouldBe(99);
        ImmutableCollectionsMarshal.AsArray(value.AsImmutableArray()).ShouldBeSameAs(elements);
    }

    [Fact]
    public void Enumeration_YieldsElementsInOrder()
    {
        var value = ValueArray.Create(1, 2, 3);

        var collected = new List<int>();
        foreach (var item in value)
        {
            collected.Add(item);
        }

        collected.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void GetEnumerator_ReturnsTheImmutableArrayStructEnumerator()
    {
        // Typed deliberately: a compile error here means foreach started allocating a boxed enumerator.
        ImmutableArray<int>.Enumerator enumerator = ValueArray.Create(1, 2, 3).GetEnumerator();

        enumerator.MoveNext().ShouldBeTrue();
        enumerator.Current.ShouldBe(1);
    }

    [Fact]
    public void NestedValueArrays_CompareStructurally()
    {
        var left = ValueArray.Create(ValueArray.Create(1, 2), ValueArray.Create(3));
        var right = ValueArray.Create(ValueArray.Create(1, 2), ValueArray.Create(3));

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(ValueArray.Create(ValueArray.Create(1), ValueArray.Create(2, 3)));
    }

    [Fact]
    public void RecordHoldingValueArray_ComparesStructurallyWithoutAnyAttribute()
    {
        var left = new Holder { Items = ValueArray.Create(1, 2, 3) };
        var right = new Holder { Items = ValueArray.Create(1, 2, 3) };

        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
        left.ShouldNotBe(new Holder { Items = ValueArray.Create(1, 2) });
    }

    [Fact]
    public void RecordHoldingValueArray_TreatsDefaultAndEmptyAlike()
    {
        new Holder().ShouldBe(new Holder { Items = ValueArray<int>.Empty });
    }

    [Fact]
    public void UsableAsDictionaryKey()
    {
        var lookup = new Dictionary<ValueArray<int>, string>
        {
            [ValueArray.Create(1, 2)] = "first",
            [ValueArray.Create(3, 4)] = "second"
        };

        lookup[ValueArray.Create(1, 2)].ShouldBe("first");
        lookup.ContainsKey(ValueArray.Create(2, 1)).ShouldBeFalse();
    }

    [Fact]
    public void ToString_FormatsElements()
    {
        ValueArray.Create(1, 2, 3).ToString().ShouldBe("[1, 2, 3]");
    }
}
