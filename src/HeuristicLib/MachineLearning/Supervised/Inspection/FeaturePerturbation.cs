using System.Numerics.Tensors;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.MachineLearning;

public abstract record FeaturePerturbation
{
    public abstract void Apply(ReadOnlySpan<double> originalValues, Span<double> perturbedValues, IRandomNumberGenerator random);

    protected static void ValidateBuffers(ReadOnlySpan<double> originalValues, Span<double> perturbedValues)
    {
        if (originalValues.Length != perturbedValues.Length)
            throw new ArgumentException("Perturbed values must have the same length as the original values.", nameof(perturbedValues));

        if (originalValues.IsEmpty)
            throw new ArgumentException("At least one feature value is required.", nameof(originalValues));
    }
}

public sealed record PermutationFeaturePerturbation : FeaturePerturbation
{
    public override void Apply(ReadOnlySpan<double> originalValues, Span<double> perturbedValues, IRandomNumberGenerator random)
    {
        ValidateBuffers(originalValues, perturbedValues);
        originalValues.CopyTo(perturbedValues);

        for (var i = perturbedValues.Length - 1; i > 0; i--)
        {
            var swapIndex = random.NextInt(i + 1);
            (perturbedValues[i], perturbedValues[swapIndex]) = (perturbedValues[swapIndex], perturbedValues[i]);
        }
    }
}

public sealed record MeanFeaturePerturbation : FeaturePerturbation
{
    public override void Apply(ReadOnlySpan<double> originalValues, Span<double> perturbedValues, IRandomNumberGenerator random)
    {
        ValidateBuffers(originalValues, perturbedValues);
        var mean = TensorPrimitives.Sum(originalValues) / originalValues.Length;
        perturbedValues.Fill(mean);
    }
}

public sealed record MedianFeaturePerturbation : FeaturePerturbation
{
    public override void Apply(ReadOnlySpan<double> originalValues, Span<double> perturbedValues, IRandomNumberGenerator random)
    {
        ValidateBuffers(originalValues, perturbedValues);
        originalValues.CopyTo(perturbedValues);
        perturbedValues.Sort();

        var middle = perturbedValues.Length / 2;
        var median = perturbedValues.Length % 2 == 0
            ? (perturbedValues[middle - 1] + perturbedValues[middle]) / 2.0
            : perturbedValues[middle];

        perturbedValues.Fill(median);
    }
}

public sealed record ResamplingFeaturePerturbation(
    IDistribution<double>? Distribution = null) : FeaturePerturbation
{
    public override void Apply(ReadOnlySpan<double> originalValues, Span<double> perturbedValues, IRandomNumberGenerator random)
    {
        ValidateBuffers(originalValues, perturbedValues);

        if (Distribution is not null)
        {
            for (var i = 0; i < perturbedValues.Length; i++)
                perturbedValues[i] = Distribution.Sample(random);

            return;
        }

        var sum = TensorPrimitives.Sum(originalValues);
        var mean = sum / originalValues.Length;
        var variance = (TensorPrimitives.SumOfSquares(originalValues) / originalValues.Length) - (mean * mean);
        var standardDeviation = Math.Sqrt(Math.Max(0.0, variance));

        if (standardDeviation <= 0.0)
        {
            perturbedValues.Fill(mean);
            return;
        }

        for (var i = 0; i < perturbedValues.Length; i++)
            perturbedValues[i] = NormalDoubleDistribution.Sample(random, mean, standardDeviation);
    }
}

public static class FeaturePerturbations
{
    public static FeaturePerturbation Permutation { get; } = new PermutationFeaturePerturbation();
    public static FeaturePerturbation Mean { get; } = new MeanFeaturePerturbation();
    public static FeaturePerturbation Median { get; } = new MedianFeaturePerturbation();

    public static FeaturePerturbation Resampling(IDistribution<double>? distribution = null)
    {
        return new ResamplingFeaturePerturbation(distribution);
    }
}
