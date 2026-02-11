using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract record Evaluator<TGenotype, TSearchSpace, TProblem>
  : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

  public abstract class Instance
    : IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  }
}

public abstract record Evaluator<TGenotype, TSearchSpace>
  : IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

  public abstract class Instance
    : IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  {
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace);

    IReadOnlyList<ObjectiveVector>
      IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
      Evaluate(genotypes, random, searchSpace);
  }
}

public abstract record Evaluator<TGenotype>
  : IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);

  public abstract class Instance
    : IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  {
    public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random);

    IReadOnlyList<ObjectiveVector> IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) =>
      Evaluate(genotypes, random);
  }
}
