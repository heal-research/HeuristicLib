using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract class SingleSolutionEvaluator<TGenotype, TSearchSpace, TProblem>
  : Evaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
}

public abstract class SingleSolutionEvaluatorInstance<TGenotype, TSearchSpace, TProblem>(int maxDegreeOfParallelism) 
  : EvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  protected abstract ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
    BatchExecution.Parallel(genotypes, (g, r) => Evaluate(g, r, searchSpace, problem), random, maxDegreeOfParallelism);
}


public abstract class SingleSolutionEvaluator<TGenotype, TSearchSpace>
  : Evaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
}

public abstract class SingleSolutionEvaluatorInstance<TGenotype, TSearchSpace>(int maxDegreeOfParallelism) 
  : EvaluatorInstance<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{

  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace) => 
    BatchExecution.Parallel(genotypes, (g, r) => Evaluate(g, r, searchSpace), random, maxDegreeOfParallelism);
}


public abstract class SingleSolutionEvaluator<TGenotype>
  : Evaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
}

public abstract class SingleSolutionEvaluatorInstance<TGenotype>(int maxDegreeOfParallelism) 
  : EvaluatorInstance<TGenotype>
{
  public abstract ObjectiveVector Evaluate(TGenotype solution, IRandomNumberGenerator random);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random) =>
    BatchExecution.Parallel(genotypes, (g, r) => Evaluate(g, r), random, maxDegreeOfParallelism);
}


public abstract class SingleSolutionStatelessEvaluator<TGenotype, TSearchSpace, TProblem>()
  : StatelessEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
  
  public abstract ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => 
    BatchExecution.Parallel(genotypes, (g, r) => Evaluate(g, r, searchSpace, problem), random, MaxDegreeOfParallelism);
}

public abstract class SingleSolutionStatelessEvaluator<TGenotype, TSearchSpace>(int maxDegreeOfParallelism)
  : StatelessEvaluator<TGenotype, TSearchSpace>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
  
  public abstract ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random, TSearchSpace searchSpace);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace) => 
    BatchExecution.Parallel(genotypes, (g, r) => Evaluate(g, r, searchSpace), random, MaxDegreeOfParallelism);
}

public abstract class SingleSolutionStatelessEvaluator<TGenotype>(int maxDegreeOfParallelism)
  : StatelessEvaluator<TGenotype>
{
  public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
  
  public abstract ObjectiveVector Evaluate(TGenotype genotype, IRandomNumberGenerator random);

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random) => 
    BatchExecution.Parallel(genotypes, (g, r) => Evaluate(g, r), random, MaxDegreeOfParallelism);
}
