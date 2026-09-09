using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public record BoundedRealVectorSearchSpace : SearchSpace<RealVector>
{
    public int Length { get; }
    public RealVector Minimum { get; }
    public RealVector Maximum { get; }

    public BoundedRealVectorSearchSpace(int length, double minimum, double maximum) : this(length, [minimum], [maximum]) { }

    public BoundedRealVectorSearchSpace(int length, RealVector minimum, RealVector maximum)
    {
        if (!Vector.AreBroadcastableTo(length, minimum, maximum))
        {
            throw new ArgumentException("Minimum and Maximum vector must be of length 1 or match the searchSpace length");
        }

        Length = length;
        Minimum = minimum;
        Maximum = maximum;
    }

    public override bool Contains(RealVector candidate)
    {
        return candidate.Count == Length
               && (candidate >= Minimum).All()
               && (candidate <= Maximum).All();
    }

    /// <remarks>
    /// Invariants are read during validation, never during a run.
    /// </remarks>
    public override IReadOnlyList<ICandidateInvariant<RealVector>> Invariants =>
        [new RealVectorLength(Length), new RealVectorBounds(Minimum, Maximum)];

    public double GetMinimum(int dim) => Minimum.Count == 1 ? Minimum[0] : Minimum[dim];
    public double GetMaximum(int dim) => Maximum.Count == 1 ? Maximum[0] : Maximum[dim];
}
