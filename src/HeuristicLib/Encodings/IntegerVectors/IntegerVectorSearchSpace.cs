using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.IntegerVectors;

public record IntegerVectorSearchSpace : SearchSpace<IntegerVector>
{
    public IntegerVectorSearchSpace(int Length, IntegerVector Minimum, IntegerVector Maximum)
    {
        this.Length = Length;
        if (!Vector.AreBroadcastableTo(Length, Minimum, Maximum))
            throw new ArgumentException("Bounds are not compatible with Length.");

        this.Minimum = Minimum;
        this.Maximum = Maximum;
    }

    public int Length { get; }
    public IntegerVector Minimum { get; }
    public IntegerVector Maximum { get; }

    public override bool Contains(IntegerVector candidate) => candidate.Count == Length
                                                             && (candidate >= Minimum).All()
                                                             && (candidate <= Maximum).All();

    public static implicit operator BoundedRealVectorSearchSpace(IntegerVectorSearchSpace integerVectorSpace) =>
        new(integerVectorSpace.Length, integerVectorSpace.Minimum, integerVectorSpace.Maximum);

    public int GetMinimum(int dim) => Minimum.Count == 1 ? Minimum[0] : Minimum[dim];
    public int GetMaximum(int dim) => Maximum.Count == 1 ? Maximum[0] : Maximum[dim];
}
