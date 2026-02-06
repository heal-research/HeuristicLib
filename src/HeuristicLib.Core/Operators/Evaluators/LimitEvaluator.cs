using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public class LimitEvaluator<TG, TS, TP>
  : Evaluator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  private readonly IEvaluator<TG, TS, TP> evaluator;
  private readonly int maxEvaluations;
  private readonly ObjectiveVector? alternativeValue;
  private readonly bool strict;

  // ToDo: document that strict means in-batch checking
  public LimitEvaluator(IEvaluator<TG, TS, TP> evaluator, int maxEvaluations, ObjectiveVector? alternativeValue, bool strict = false)
  {
    this.evaluator = evaluator;
    this.maxEvaluations = maxEvaluations;
    this.alternativeValue = alternativeValue;
    this.strict = strict;
  }
  
  public override IEvaluatorInstance<TG, TS, TP> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var countedEvaluator = evaluator.CountInvocations(out var counter);
    
    var evaluatorInstance = instanceRegistry.GetOrCreate(countedEvaluator);
    return new Instance(evaluatorInstance, counter, maxEvaluations, alternativeValue, strict);
  }

  public class Instance : EvaluatorInstance<TG, TS, TP>
  {
    private readonly IEvaluatorInstance<TG, TS, TP> evaluator;
    private readonly InvocationCounter counter;
    private readonly int maxEvaluations;
    private readonly ObjectiveVector? alternativeValue;
    private readonly bool strict;

    public Instance(IEvaluatorInstance<TG, TS, TP> evaluator, InvocationCounter counter, int maxEvaluations, ObjectiveVector? alternativeValue, bool strict)
    {
      this.evaluator = evaluator;
      this.counter = counter;
      this.maxEvaluations = maxEvaluations;
      this.alternativeValue = alternativeValue;
      this.strict = strict;
    }

    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TG> genotypes, IRandomNumberGenerator random, TS searchSpace, TP problem)
    {
      var remainingEvaluations = maxEvaluations - counter.CurrentCount;
      
      var alternative = alternativeValue ?? problem.Objective.Worst;
      
      if (remainingEvaluations <= 0) {
        return Enumerable.Repeat(alternative, genotypes.Count).ToArray();
      }

      if (strict && remainingEvaluations < genotypes.Count) {
        var solutionsToEvaluate = genotypes.Take(remainingEvaluations).ToList();
        var solutionsToSkip = genotypes.Skip(remainingEvaluations).ToList();

        var evaluated = evaluator.Evaluate(solutionsToEvaluate, random, searchSpace, problem);
        var skipped = Enumerable.Repeat(alternative, solutionsToSkip.Count);

        return evaluated.Concat(skipped).ToArray();
      }
      
      return evaluator.Evaluate(genotypes, random, searchSpace, problem);
    }
  }
}

public static class LimitEvaluatorExtensions
{
  extension<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator) where TG : class where TS : class, ISearchSpace<TG> where TP : class, IProblem<TG, TS>
  {
    public LimitEvaluator<TG, TS, TP> LimitEvaluations(int maxEvaluations, ObjectiveVector? alternativeValue = null, bool strict = false)
    {
      return new LimitEvaluator<TG, TS, TP>(evaluator, maxEvaluations, alternativeValue, strict);
    }
  }
}

// public class LimitingEvaluatorRewriter<TBuildSpec, TG, TS, TP>(int maxEvaluations, bool preventOverBudget)
//   : IAlgorithmBuilderRewriter<TBuildSpec>
//   where TBuildSpec : class, ISpecWithEvaluator<TG, TS, TP>
//   where TG : class
//   where TS : class, ISearchSpace<TG>
//   where TP : class, IProblem<TG, TS>
// {
//   public void Rewrite(TBuildSpec buildSpec)
//   {
//     var countedEvaluator = buildSpec.Evaluator.AsCountedEvaluator();
//     // ToDo: think about only using the limit evaluator if prevenOverBudget is true
//     var limitEvaluator = new LimitEvaluator<TG, TS, TP>(countedEvaluator, maxEvaluations, preventOverBudget);
//     buildSpec.Evaluator = limitEvaluator;
//   }
// }
