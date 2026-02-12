using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record SingleSolutionStatelessEvaluator<TGenotype, TSearchSpace, TProblem>
  : StatelessEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public int MaxDegreeOfParallelism { get; init; } = -1;

  public abstract ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Parallel(genotypes, (g, r) => Evaluate(g, r, searchSpace, problem), random, MaxDegreeOfParallelism);
}


public abstract record SingleSolutionStatelessEvaluator<TGenotype, TSearchSpace>
  : StatelessEvaluator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public int MaxDegreeOfParallelism { get; init; } = -1;

  public abstract ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace) =>
    BatchExecution.Parallel(genotypes, (g, r) => Evaluate(g, r, searchSpace), random, MaxDegreeOfParallelism);
}

public abstract record SingleSolutionStatelessEvaluator<TGenotype>
  : StatelessEvaluator<TGenotype>
{
  public int MaxDegreeOfParallelism { get; init; } = -1;

  public abstract ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random) =>
    BatchExecution.Parallel(genotypes, (g, r) => Evaluate(g, r), random, MaxDegreeOfParallelism);
}
