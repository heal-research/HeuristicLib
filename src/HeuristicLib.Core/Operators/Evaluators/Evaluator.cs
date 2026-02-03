using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public abstract class Evaluator<TGenotype, TSearchSpace, TProblem> : IEvaluator<TGenotype, TSearchSpace, TProblem>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem);
  IReadOnlyList<ObjectiveVector> IOperator<IReadOnlyList<TGenotype>, IOptimizationContext<TGenotype, TSearchSpace, TProblem>, IReadOnlyList<ObjectiveVector>>.Execute(IReadOnlyList<TGenotype> input, IOptimizationContext<TGenotype, TSearchSpace, TProblem> context) =>
    Evaluate(input, context.Random, context.SearchSpace, context.Problem);
}

public abstract class Evaluator<TGenotype, TSearchSpace> : IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TSearchSpace searchSpace);

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => Evaluate(solutions, random, searchSpace);
  IReadOnlyList<ObjectiveVector> IOperator<IReadOnlyList<TGenotype>, IOptimizationContext<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>>, IReadOnlyList<ObjectiveVector>>.Execute(IReadOnlyList<TGenotype> input, IOptimizationContext<TGenotype, TSearchSpace, IProblem<TGenotype, TSearchSpace>> context) => Evaluate(input, context.Random, context.SearchSpace);
}

public abstract class Evaluator<TGenotype> : IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random);

  IReadOnlyList<ObjectiveVector> IEvaluator<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.Evaluate(IReadOnlyList<TGenotype> solutions, IRandomNumberGenerator random, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => Evaluate(solutions, random);
  IReadOnlyList<ObjectiveVector> IOperator<IReadOnlyList<TGenotype>, IOptimizationContext<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>, IReadOnlyList<ObjectiveVector>>.Execute(IReadOnlyList<TGenotype> input, IOptimizationContext<TGenotype, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> context) => Evaluate(input, context.Random);
}

public class EvaluatorAdapter<TG, TS, TP> : Evaluator<TG, TS, TP>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly IOperator<IReadOnlyList<TG>, IOptimizationContext<TG, TS, TP>, IReadOnlyList<ObjectiveVector>> @operator;

  public EvaluatorAdapter(IOperator<IReadOnlyList<TG>, IOptimizationContext<TG, TS, TP>, IReadOnlyList<ObjectiveVector>> @operator)
  {
    this.@operator = @operator;
  }

  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TG> solutions, IRandomNumberGenerator random, TS searchSpace, TP problem)
  {
    var context = new OptimizationContext<TG, TS, TP>(searchSpace, problem, random);
    return @operator.Execute(solutions, context);
  }
}

public static class EvaluatorAdapter
{
  extension<TG, TS, TP>(IOperator<IReadOnlyList<TG>, IOptimizationContext<TG, TS, TP>, IReadOnlyList<ObjectiveVector>> @operator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IEvaluator<TG, TS, TP> AsEvaluator()
    {
      return new EvaluatorAdapter<TG, TS, TP>(@operator);
    }
  }
  
  extension<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator)
    where TS : class, ISearchSpace<TG>
    where TP : class, IProblem<TG, TS>
  {
    public IOperator<IReadOnlyList<TG>, IOptimizationContext<TG, TS, TP>, IReadOnlyList<ObjectiveVector>> AsOperator()
    {
      return evaluator;
    }
  }
}
