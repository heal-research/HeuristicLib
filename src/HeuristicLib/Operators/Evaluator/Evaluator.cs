using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Evaluator;

public class Evaluator<TGenotype, TEncoding, TProblem>(IRandomNumberGenerator randomNumberGenerator) : IEvaluator<TGenotype, TEncoding, TProblem>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding> {
  public Evaluator() : this(NoRandomNumberGenerator.Instance) { }

  public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, TEncoding encoding, TProblem problem) {
    return genotypes.ParallelSelect(randomNumberGenerator, (i, genotype, random) => problem.Evaluate(genotype, random));
  }
}

public static class Evaluator {
  public static Evaluator<TGenotype> CreateEvaluator<TGenotype>(this IProblem<TGenotype, IEncoding<TGenotype>> problem) => new();
  public static Evaluator<TGenotype> CreateEvaluator<TGenotype>(this IStochasticProblem<TGenotype, IEncoding<TGenotype>> problem) => new(new SystemRandomNumberGenerator(0));
}

public class Evaluator<TGenotype> : Evaluator<TGenotype, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  public Evaluator() { }
  public Evaluator(IRandomNumberGenerator randomNumberGenerator) : base(randomNumberGenerator) { }
}
