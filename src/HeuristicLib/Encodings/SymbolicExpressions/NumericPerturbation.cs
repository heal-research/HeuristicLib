using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressions;

public abstract record NumericPerturbation
{
    public static NumericPerturbation Default { get; } = new ChooseNumericPerturbation(
        [(new MultiplicativeNumericPerturbation(new UniformDoubleDistribution(-0.1, 0.1)), 0.8),
         (new ResampleInitialNumericPerturbation(), 0.2)]);

    public abstract bool TryApply(double value, EvolvableConstantSymbol symbol, IRandomNumberGenerator random, out double perturbed);
}

public sealed record AdditiveNumericPerturbation(IDistribution<double> DeltaDistribution) : NumericPerturbation
{
    public override bool TryApply(double value, EvolvableConstantSymbol symbol, IRandomNumberGenerator random, out double perturbed)
    {
        perturbed = value + DeltaDistribution.Sample(random);
        return true;
    }
}

public sealed record MultiplicativeNumericPerturbation(IDistribution<double> RelativeDeltaDistribution) : NumericPerturbation
{
    public override bool TryApply(double value, EvolvableConstantSymbol symbol, IRandomNumberGenerator random, out double perturbed)
    {
        perturbed = value * (1.0 + RelativeDeltaDistribution.Sample(random));
        return true;
    }
}

public sealed record ResampleNumericPerturbation(IDistribution<double> Distribution) : NumericPerturbation
{
    public override bool TryApply(double value, EvolvableConstantSymbol symbol, IRandomNumberGenerator random, out double perturbed)
    {
        perturbed = Distribution.Sample(random);
        return true;
    }
}

public sealed record ResampleInitialNumericPerturbation : NumericPerturbation
{
    public override bool TryApply(double value, EvolvableConstantSymbol symbol, IRandomNumberGenerator random, out double perturbed)
    {
        perturbed = symbol.InitialDistribution.Sample(random);
        return true;
    }
}

/// <remarks>
/// <see cref="Weights"/> is the only member a <c>with</c> expression may set, so <c>choice with { Weights = … }</c>
/// reweights the unchanged options. Choosing from a different set of options means constructing a new choice.
/// </remarks>
public sealed record ChooseNumericPerturbation : NumericPerturbation
{
    private readonly WeightedItemSampler<NumericPerturbation> sampler;

    public ValueArray<NumericPerturbation> Options => sampler.Items;

    /// <summary>
    /// Gets the configured weights, exactly as supplied, or an empty collection for uniform selection. Setting them
    /// reweights <see cref="Options"/>; an empty collection restores uniform selection.
    /// </summary>
    public ValueArray<double> Weights
    {
        get => sampler.Weights;
        init => sampler = sampler with { Weights = value };
    }

    public ChooseNumericPerturbation(IReadOnlyList<NumericPerturbation> options, IReadOnlyList<double>? weights = null)
    {
        if (options.Count == 0)
            throw new ArgumentException("A choice needs at least one perturbation.", nameof(options));

        sampler = new WeightedItemSampler<NumericPerturbation>(options, weights);
    }

    public ChooseNumericPerturbation(IReadOnlyList<(NumericPerturbation Perturbation, double Weight)> options)
    {
        if (options.Count == 0)
            throw new ArgumentException("A choice needs at least one perturbation.", nameof(options));

        var perturbations = new NumericPerturbation[options.Count];
        var weights = new double[options.Count];
        for (var i = 0; i < options.Count; i++)
        {
            perturbations[i] = options[i].Perturbation;
            weights[i] = options[i].Weight;
        }

        sampler = new WeightedItemSampler<NumericPerturbation>(
            ValueArray.FromOwnedArray(perturbations),
            ValueArray.FromOwnedArray(weights));
    }

    public override bool TryApply(double value, EvolvableConstantSymbol symbol, IRandomNumberGenerator random, out double perturbed)
    {
        var perturbation = sampler.Sample(random);
        return perturbation.TryApply(value, symbol, random, out perturbed);
    }
}

public sealed record ChainNumericPerturbation : NumericPerturbation
{
    public ValueArray<NumericPerturbation> Stages { get; }

    public ChainNumericPerturbation(IReadOnlyList<NumericPerturbation> stages)
    {
        if (stages.Count == 0)
            throw new ArgumentException("A chain needs at least one perturbation.", nameof(stages));

        Stages = stages.ToValueArray();
    }

    public override bool TryApply(double value, EvolvableConstantSymbol symbol, IRandomNumberGenerator random, out double perturbed)
    {
        perturbed = value;
        foreach (var stage in Stages)
        {
            if (!stage.TryApply(perturbed, symbol, random, out perturbed))
            {
                perturbed = value;
                return false;
            }
        }

        return true;
    }
}
