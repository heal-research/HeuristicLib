using HEAL.HeuristicLib.Core;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

// ToDo: class for individual "FitnessFunction"

public interface IEvaluator<TGenotype, TPhenotype> : IOperator {
  Solution<TGenotype, TPhenotype>[] Evaluate(TGenotype[] population);
}

public abstract class EvaluatorBase<TGenotype, TPhenotype> : IEvaluator<TGenotype, TPhenotype> {
 public abstract Solution<TGenotype, TPhenotype>[] Evaluate(TGenotype[] population);
}

public static class Evaluator {
  public static FitnessFunctionEvaluator<TGenotype, TPhenotype> UsingFitnessFunction<TGenotype, TPhenotype>(Func<TPhenotype, Fitness> evaluator, IGenotypeMapper<TGenotype, TPhenotype> decoder) {
    return new FitnessFunctionEvaluator<TGenotype, TPhenotype>(evaluator, decoder);
  }
  public static FitnessFunctionEvaluator<TGenotype, TGenotype> UsingFitnessFunction<TGenotype>(Func<TGenotype, Fitness> evaluator) {
    return new FitnessFunctionEvaluator<TGenotype, TGenotype>(evaluator, GenotypeMapper.Identity<TGenotype>());
  }
}

public abstract class FitnessFunctionEvaluatorBase<TGenotype, TPhenotype> : EvaluatorBase<TGenotype, TPhenotype> {
  // Define the "runner" (sequential, parallel, ...)
  public IGenotypeMapper<TGenotype, TPhenotype> Decoder { get; }
  protected FitnessFunctionEvaluatorBase(IGenotypeMapper<TGenotype, TPhenotype> decoder) {
    Decoder = decoder;
  }
  public abstract Fitness Evaluate(TPhenotype phenotype);
  public override Solution<TGenotype, TPhenotype>[] Evaluate(TGenotype[] population) {
    return population
      .Select(genotype => {
        var phenotype = Decoder.Decode(genotype);
        var fitness = Evaluate(phenotype);
        return new Solution<TGenotype, TPhenotype>(genotype, phenotype, fitness);
      })
      .ToArray();
  }
}

public class FitnessFunctionEvaluator<TGenotype, TPhenotype> : FitnessFunctionEvaluatorBase<TGenotype, TPhenotype> {
  private readonly Func<TPhenotype, Fitness> fitnessFunction;
  public FitnessFunctionEvaluator(Func<TPhenotype, Fitness> fitnessFunction, IGenotypeMapper<TGenotype, TPhenotype> decoder) : base(decoder) {
    this.fitnessFunction = fitnessFunction;
  }
  public override Fitness Evaluate(TPhenotype phenotype) => fitnessFunction(phenotype);
}
