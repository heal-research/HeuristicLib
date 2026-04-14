using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.SearchSpaces.Vectors;

public record IntegerVectorSearchSpace : SearchSpace<IntegerVector>
{
  private static readonly IntegerVector DefaultStep = new(1);

  public IntegerVectorSearchSpace(int Length, IntegerVector Minimum, IntegerVector Maximum, IntegerVector? Step = null)
  {
    if (Minimum.Count != Length && Minimum.Count != 1)
      throw new ArgumentException("Minium is compatible with Length");
    if (Maximum.Count != Length && Maximum.Count != 1)
      throw new ArgumentException("Maximum is compatible with Length");
    if (Step is not null && Step.Count != Length && Step.Count != 1)
      throw new ArgumentException("Step is compatible with Length");
    if ((Minimum > Maximum).Any())
      throw new ArgumentException("Minium and Maximum are not compatible");
    if (Step is not null && Step.Any(x => x < 1))
      throw new ArgumentException("Steps may not be zero or negative");
    this.Length = Length;
    this.Minimum = Minimum;
    this.Maximum = Maximum;
    this.Step = Step ?? DefaultStep;
  }

  public IntegerVector Step { get; }
  public int Length { get; }
  public IntegerVector Minimum { get; }
  public IntegerVector Maximum { get; }

  public override bool Contains(IntegerVector genotype)
  {
    if (genotype.Count != Length)
      return false;

    for (int i = 0; i < Length; i++) {
      var v = genotype[i];
      var (min, max, step) = GetDimInfo(i);

      if (v < min)
        return false;
      if (v > max)
        return false;
      if ((v - min) % step != 0)
        return false;
    }

    return true;
  }

  public static implicit operator RealVectorSearchSpace(IntegerVectorSearchSpace integerVectorSpace) =>
    new(integerVectorSpace.Length, integerVectorSpace.Minimum, integerVectorSpace.Maximum);

  public int FloorFeasible(double x, int dim)
  {
    var (minBound, maxBound, step) = GetDimInfo(dim);
    if (x <= minBound)
      return minBound;
    if (x >= maxBound)
      return maxBound;

    // compute k = floor((x - minBound)/step)
    double t = (x - minBound) / step;
    int k = (int)Math.Floor(t + Tolerance);
    long v = minBound + (long)k * step;
    if (v < minBound)
      v = minBound;
    if (v > maxBound)
      v = maxBound;
    return (int)v;
  }

  public int CeilingFeasible(double x, int dim)
  {
    var (minBound, maxBound, step) = GetDimInfo(dim);
    if (x <= minBound)
      return minBound;
    if (x >= maxBound)
      return maxBound;

    // compute k = ceil((x - minBound)/step)
    double t = (x - minBound) / step;
    int k = (int)Math.Ceiling(t - Tolerance);
    long v = minBound + (long)k * step;
    if (v < minBound)
      v = minBound;
    if (v > maxBound)
      v = maxBound;
    return (int)v;
  }

  public int RoundFeasible(double x, int dim)
  {
    var (minBound, maxBound, step) = GetDimInfo(dim);
    if (x <= minBound)
      return minBound;
    if (x >= maxBound)
      return maxBound;

    double t = (x - minBound) / step;

    // round to nearest integer index
    int k = (int)Math.Round(t, MidpointRounding.AwayFromZero);

    long v = minBound + (long)k * step;
    if (v < minBound)
      v = minBound;
    if (v > maxBound)
      v = maxBound;
    return (int)v;
  }

  public IntegerVector RoundFeasible(RealVector x)
  {
    var res = new int[x.Count];
    for (var i = 0; i < res.Length; i++) {
      res[i] = RoundFeasible(x[i], i);
    }

    return res;
  }

  public int UniformRandom(IRandomNumberGenerator random, int dim)
  {
    var (minBound, maxBound, step) = GetDimInfo(dim);
    int nSteps = (maxBound - minBound) / step; // inclusive count-1
    int k = random.NextInt(0, nSteps, true); // inclusive
    var v = minBound + k * step;
    return v;
  }

  private (int minBound, int maxBound, int step) GetDimInfo(int dim)
  {
    int minBound = Minimum.Count == 1 ? Minimum[0] : Minimum[dim];
    int maxBound = Maximum.Count == 1 ? Maximum[0] : Maximum[dim];
    int step = Step.Count == 1 ? Step[0] : Step[dim];
    return (minBound, maxBound, step);
  }

  private const double Tolerance = 1e-12;
}
