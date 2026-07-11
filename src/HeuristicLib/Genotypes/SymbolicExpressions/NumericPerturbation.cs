using Generator.Equals;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

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

[Equatable]
public sealed partial record ChooseNumericPerturbation : NumericPerturbation
{
    [OrderedEquality] public ImmutableArray<NumericPerturbation> Options { get; }
    [OrderedEquality] public ImmutableArray<double> Weights { get; }

    public ChooseNumericPerturbation(ImmutableArray<NumericPerturbation> options)
    {
        if (options.IsDefaultOrEmpty)
            throw new ArgumentException("A choice needs at least one perturbation.", nameof(options));

        Options = options;
        Weights = ImmutableArray<double>.Empty;
    }

    public ChooseNumericPerturbation(ImmutableArray<NumericPerturbation> options, ImmutableArray<double> weights)
    {
        if (options.IsDefaultOrEmpty)
            throw new ArgumentException("A choice needs at least one perturbation.", nameof(options));

        Options = options;
        Weights = WeightSelection.Normalize(weights, options.Length);
    }

    public ChooseNumericPerturbation(IEnumerable<(NumericPerturbation Perturbation, double Weight)> options)
    {
        var entries = options.ToArray();
        if (entries.Length == 0)
            throw new ArgumentException("A choice needs at least one perturbation.", nameof(options));

        Options = entries.Select(entry => entry.Perturbation).ToImmutableArray();
        Weights = WeightSelection.Normalize(entries.Select(entry => entry.Weight).ToArray(), entries.Length);
    }

    public override bool TryApply(double value, EvolvableConstantSymbol symbol, IRandomNumberGenerator random, out double perturbed)
    {
        var perturbationIndex = WeightSelection.SelectIndex(random, Options.Length, Weights);
        var perturbation = Options[perturbationIndex];
        return perturbation.TryApply(value, symbol, random, out perturbed);
    }
}

[Equatable]
public sealed partial record ChainNumericPerturbation : NumericPerturbation
{
    [OrderedEquality] public ImmutableArray<NumericPerturbation> Stages { get; }

    public ChainNumericPerturbation(ImmutableArray<NumericPerturbation> stages)
    {
        if (stages.IsDefaultOrEmpty)
            throw new ArgumentException("A chain needs at least one perturbation.", nameof(stages));

        Stages = stages;
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
