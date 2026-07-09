using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record IntegerVectorSearchSpace : SearchSpace<IntegerVector>
{
    public IntegerVectorSearchSpace(int Length, IntegerVector Minimum, IntegerVector Maximum)
    {
        this.Length = Length;
        if (Minimum.Count != Length && Minimum.Count != 1)
            throw new ArgumentException("Minimum is not compatible with Length", nameof(Minimum));
        if (Maximum.Count != Length && Maximum.Count != 1)
            throw new ArgumentException("Maximum is not compatible with Length", nameof(Maximum));
        if ((Minimum > Maximum).Any())
            throw new ArgumentException("Minimum values must be less than or equal to maximum values.");

        this.Minimum = Minimum;
        this.Maximum = Maximum;
    }

    public int Length { get; }
    public IntegerVector Minimum { get; }
    public IntegerVector Maximum { get; }

    public override bool Contains(IntegerVector candidate) => candidate.Count == Length
                                                             && (candidate >= Minimum).All()
                                                             && (candidate <= Maximum).All();

    public static implicit operator RealVectorSearchSpace(IntegerVectorSearchSpace integerVectorSpace) =>
      new(integerVectorSpace.Length, integerVectorSpace.Minimum, integerVectorSpace.Maximum);
}
