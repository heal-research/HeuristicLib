using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract class Evaluator<TGenotype, TSearchSpace, TProblem> 
  : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class EvaluatorInstance<TGenotype, TSearchSpace, TProblem> 
  : IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}


public abstract class Evaluator<TGenotype, TSearchSpace>
  : IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class EvaluatorInstance<TGenotype, TSearchSpace>
  : IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace);
 
  IReadOnlyList<ObjectiveVector>
    IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
    Evaluate(genotypes, random, searchSpace);
}


public abstract class Evaluator<TGenotype> 
  : IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class EvaluatorInstance<TGenotype>
  : IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random);

  IReadOnlyList<ObjectiveVector> IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => 
    Evaluate(genotypes, random);
}


public abstract class StatelessEvaluator<TGenotype, TSearchSpace, TProblem>
  : IEvaluator<TGenotype, TSearchSpace, TProblem>,
    IEvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
}

public abstract class StatelessEvaluator<TGenotype, TSearchSpace>
  : IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>,
    IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace);

  IReadOnlyList<ObjectiveVector> IEvaluatorInstance<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) =>
    Evaluate(genotypes, random, searchSpace);
}

public abstract class StatelessEvaluator<TGenotype>
  : IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>,
    IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random);

  IReadOnlyList<ObjectiveVector> IEvaluatorInstance<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => 
    Evaluate(genotypes, random);
}
