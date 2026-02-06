using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators.Evaluators;

public class RepeatingEvaluator<TGenotype, TSearchSpace, TProblem>
  : Evaluator<TGenotype, TSearchSpace, TProblem>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  private readonly IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator;
  private readonly int repeats;
  private readonly Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator;

  public RepeatingEvaluator(IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
  {
    this.evaluator = evaluator;
    this.repeats = repeats;
    this.aggregator = aggregator;
  }

  public override IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    // ToDo: think about if we want a fresh evaluation or the the same evaluation instance as the base for the repeated evaluations. 
    // Currently we use the same instance to maintain state such as caches.
    var evaluatorInstance = instanceRegistry.GetOrAdd(evaluator, () => this.evaluator.CreateExecutionInstance(instanceRegistry));
    return new Instance(evaluatorInstance, repeats, aggregator);
  }

  public class Instance : EvaluatorInstance<TGenotype, TSearchSpace, TProblem>
  {
    private readonly IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator;
    private readonly int repeats;
    private readonly Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator;

    public Instance(IEvaluatorInstance<TGenotype, TSearchSpace, TProblem> evaluator, int repeats, Func<ObjectiveVector, ObjectiveVector, ObjectiveVector> aggregator)
    {
      this.evaluator = evaluator;
      this.repeats = repeats;
      this.aggregator = aggregator;
    }

    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TGenotype> genotypes, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
    {
      var results = evaluator.Evaluate(genotypes, random, searchSpace, problem).ToArray();

      for (var i = 0; i < repeats; i++) {
        var reevaluationResult = evaluator.Evaluate(genotypes, random, searchSpace, problem);
        for (var j = 0; j < results.Length; j++) {
          results[j] = aggregator(results[j], reevaluationResult[j]);
        }
      }

      return results;
    }
  }
}
