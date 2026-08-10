using System.Collections.Immutable;
using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Genotypes;

public class VectorAuthoringSpecs
{
    [Fact]
    public void CustomVector_CanUseProtectedAuthoringSurface()
    {
        var elements = ImmutableArray.Create(1, 2, 3);
        var vector = CustomVector.Create(elements);

        vector.Count.ShouldBe(3);
        vector.Sum().ShouldBe(6);
        vector.Storage.ShouldBe(elements);
    }

    [Fact]
    public void CustomVector_ReusesImmutableArrayPassedAsEnumerable()
    {
        var elements = ImmutableArray.Create(1, 2, 3);
        IEnumerable<int> enumerable = elements;

        var vector = CustomVector.Create(enumerable);

        vector.Storage.ShouldBe(elements);
    }

    private sealed class CustomVector : Vector<int>
    {
        private CustomVector(ImmutableArray<int> elements)
            : base(elements)
        { }

        private CustomVector(IEnumerable<int> elements)
            : base(elements)
        { }

        public static CustomVector Create(ImmutableArray<int> elements) => new(elements);

        public static CustomVector Create(IEnumerable<int> elements) => new(elements);

        public ImmutableArray<int> Storage => Elements;

        public int Sum()
        {
            var sum = 0;
            foreach (var element in Elements)
            {
                sum += element;
            }

            return sum;
        }
    }
}
