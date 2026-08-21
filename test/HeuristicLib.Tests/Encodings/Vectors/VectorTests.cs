using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Encodings.Vectors;

namespace HEAL.HeuristicLib.Tests.Genotypes.Vectors;

public sealed class VectorTests
{
    [Fact]
    public void AreBroadcastableTo_AcceptsMixedVectorTypes()
    {
        IntegerVector minimum = 0;
        IntegerVector maximum = IntegerVector.Create(10, 10, 10);
        RealVector sigma = RealVector.Create(1, 2, 3);

        Vector.AreBroadcastableTo(3, minimum, maximum, sigma).ShouldBeTrue();
        Vector.AreBroadcastableTo(2, minimum, maximum, sigma).ShouldBeFalse();
    }

    [Fact]
    public void AreBroadcastableTo_RejectsNegativeTargetLength()
    {
        Vector.AreBroadcastableTo(-1, (IntegerVector)1).ShouldBeFalse();
    }

    [Fact]
    public void TryGetBroadcastLength_ComputesMixedVectorLength()
    {
        IntegerVector scalar = 1;
        RealVector vector = RealVector.Create(1, 2, 3);
        BoolVector mask = BoolVector.Create(true, false, true);

        Vector.TryGetBroadcastLength(scalar, [vector, mask], out var length).ShouldBeTrue();
        length.ShouldBe(3);
    }

    [Fact]
    public void TryGetBroadcastLength_ReturnsZero_WhenVectorsAreNotBroadcastable()
    {
        IntegerVector vector = IntegerVector.Create(1, 2);
        RealVector incompatible = RealVector.Create(1, 2, 3);

        Vector.TryGetBroadcastLength(vector, [incompatible], out var length).ShouldBeFalse();
        length.ShouldBe(0);
    }

    [Fact]
    public void All_IsAvailableForEveryElementType()
    {
        IntegerVector.Create(1, 2, 3).All(static value => value > 0).ShouldBeTrue();
        BoolVector.Create(true, false).All(static value => value).ShouldBeFalse();
    }

    [Fact]
    public void DirectEnumeration_ReturnsAllElements()
    {
        var vector = IntegerVector.Create(1, 2, 3);
        var elements = new List<int>();

        foreach (var element in vector)
        {
            elements.Add(element);
        }

        elements.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void DefaultImmutableArrays_AreNormalizedToEmptyVectors()
    {
        ImmutableArray<bool> boolElements = default;
        ImmutableArray<int> integerElements = default;
        ImmutableArray<double> realElements = default;

#pragma warning disable S3220 // The ImmutableArray overload is intentionally selected.
        new BoolVector(boolElements).ShouldBeEmpty();
        new IntegerVector(integerElements).ShouldBeEmpty();
        new RealVector(realElements).ShouldBeEmpty();
        new Permutation(integerElements).ShouldBeEmpty();
#pragma warning restore S3220
    }
}
