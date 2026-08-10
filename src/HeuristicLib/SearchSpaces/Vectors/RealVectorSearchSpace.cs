using HEAL.HeuristicLib.Genotypes.Vectors;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record RealVectorSearchSpace : SearchSpace<RealVector>
{
    public int Length { get; }
    public RealVector Minimum { get; }
    public RealVector Maximum { get; }

    public RealVectorSearchSpace(int length, double minimum, double maximum) : this(length, [minimum], [maximum]) { }

    public RealVectorSearchSpace(int length, RealVector minimum, RealVector maximum)
    {
        if (!RealVector.AreBroadcastableTo(length, minimum, maximum))
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

    public double GetMinimum(int dim) => Minimum.Count == 1 ? Minimum[0] : Minimum[dim];
    public double GetMaximum(int dim) => Maximum.Count == 1 ? Maximum[0] : Maximum[dim];
}
