using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.RealVectors;

public record PolynomialMutator : SingleCandidateMutator<RealVector, BoundedRealVectorSearchSpace>, IInvariantContract<RealVector>
{
    /// <summary>
    /// The result is clamped to the search space bounds, so length and bounds both survive at any distribution index.
    /// </summary>
    public bool? Ensures(ISearchInvariant<RealVector> invariant) => invariant switch
    {
        RealVectorLength or RealVectorBounds => true,
        _ => null
    };

    public double Eta { get; init; } = 20;

    public bool AtLeastOnce { get; init; }

    public override RealVector MutateCandidate(RealVector parent, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace) =>
        Mutate(parent, random, searchSpace, Eta, AtLeastOnce);

    public static RealVector Mutate(RealVector candidate, IRandomNumberGenerator random, BoundedRealVectorSearchSpace searchSpace, double eta, bool atLeastOnce)
    {
        return Mutate(candidate, random, searchSpace.Minimum, searchSpace.Maximum, eta, atLeastOnce);
    }

    public static RealVector Mutate(RealVector candidate, IRandomNumberGenerator random, RealVector minimum, RealVector maximum, double eta, bool atLeastOnce)
    {
        if (!Vector.AreBroadcastableTo(candidate.Count, minimum, maximum))
            throw new ArgumentException("Minimum and maximum must each have length 1 or match the candidate length.");

        if (candidate.Count == 0)
            return candidate;

        var length = candidate.Count;
        var mutationProbability = Math.Min(0.5, 1.0 / length);
        var mutationMask = random.NextBools(length, mutationProbability);
        if (atLeastOnce && !mutationMask.AsSpan().Contains(true))
            mutationMask[random.NextInt(length)] = true;

        for (var i = 0; i < length; i++)
        {
#pragma warning disable S1244
            if (minimum[i % minimum.Count] == maximum[i % maximum.Count])
#pragma warning restore S1244
                mutationMask[i] = false;
        }

        var result = candidate.ToArray();
        if (!mutationMask.AsSpan().Contains(true))
            return RealVector.Clamp(RealVector.FromOwnedArray(result), minimum, maximum);

        var mutationExponent = 1.0 / (eta + 1.0);

        for (var i = 0; i < length; i++)
        {
            if (!mutationMask[i])
                continue;

            var value = candidate[i];
            var lowerBound = minimum[i % minimum.Count];
            var upperBound = maximum[i % maximum.Count];
            var range = upperBound - lowerBound;

            var distanceFromLowerBound = (value - lowerBound) / range;
            var distanceFromUpperBound = (upperBound - value) / range;

            var sample = random.NextDouble();
            double mutationOffset;

            if (sample <= 0.5)
            {
                var distanceToBoundary = 1.0 - distanceFromLowerBound;
                var distribution = (2.0 * sample) + ((1.0 - (2.0 * sample)) * Math.Pow(distanceToBoundary, eta + 1.0));
                mutationOffset = Math.Pow(distribution, mutationExponent) - 1.0;
            }
            else
            {
                var distanceToBoundary = 1.0 - distanceFromUpperBound;
                var distribution = (2.0 * (1.0 - sample)) + (2.0 * (sample - 0.5) * Math.Pow(distanceToBoundary, eta + 1.0));
                mutationOffset = 1.0 - Math.Pow(distribution, mutationExponent);
            }

            var mutatedValue = value + (mutationOffset * range);
            if (mutatedValue < lowerBound)
                mutatedValue = lowerBound;
            else if (mutatedValue > upperBound)
                mutatedValue = upperBound;

            result[i] = mutatedValue;
        }

        return RealVector.Clamp(RealVector.FromOwnedArray(result), minimum, maximum);
    }
}
