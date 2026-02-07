using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Problems.Dynamic.Operators;

public class ReevaluationInterceptor<T, TR, TE, TP> 
  : Interceptor<T, TR, TE, TP>
  where TR : PopulationState<T>
  where TE : class, ISearchSpace<T>
  where TP : DynamicProblem<T, TE>
  where T : class
{
  private readonly IEvaluator<T, TE, TP> evaluator;
  private readonly TP problem;
  
  public ReevaluationInterceptor(IEvaluator<T, TE, TP> evaluator, TP problem)
  {
    this.evaluator = evaluator;
    this.problem = problem;
  }

  public override Instance CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry)
  {
    var evaluatorInstance = instanceRegistry.GetOrCreate(evaluator);
    return new Instance(evaluatorInstance, problem);
  }

  public class Instance : InterceptorInstance<T, TR, TE, TP>
  {
    private readonly IEvaluatorInstance<T, TE, TP> evaluator;
    
    private int requireReevaluation;
    
    public Instance(IEvaluatorInstance<T, TE, TP> evaluator, TP problem) {
      this.evaluator = evaluator;
      
      // ToDo: maybe we have a memory leak here?
      problem.EpochClock.OnEpochChange += (_, _) => Interlocked.Increment(ref requireReevaluation);
    }

    public override TR Transform(TR currentIterationResult, TR? previousIterationResult, TE encoding, TP problem)
    {
      var r = currentIterationResult;

      var wasTrue = Interlocked.Exchange(ref requireReevaluation, 0) != 0;
      if (!wasTrue) {
        return r;
      }

      var genotypes = r.Population.Genotypes.ToArray();
      var fitnesses = evaluator.Evaluate(genotypes, null!, encoding, problem); //TODO: pass random?
      r = r with { Population = Population.From(genotypes, fitnesses) };

      return r;
    }
  }

}


// public class ReevaluationInterceptor<T, TR, TE, TP> : Interceptor<T, TR, TE, TP>
//   where TR : PopulationState<T>
//   where TE : class, ISearchSpace<T>
//   where TP : DynamicProblem<T, TE>
//   where T : class
// {
//   private readonly IEvaluator<T, TE, TP> evaluator;
//   private readonly IInterceptor<T, TR, TE, TP>? inner;
//
//   private int requireReevaluation;
//
//   public ReevaluationInterceptor(IInterceptor<T, TR, TE, TP>? inner, IEvaluator<T, TE, TP> evaluator, TP problem)
//   {
//     this.inner = inner;
//     this.evaluator = evaluator;
//     problem.EpochClock.OnEpochChange += (_, _) => Interlocked.Increment(ref requireReevaluation);
//   }
//
//   public TR Transform(TR currentIterationResult, TR? previousIterationResult, TE encoding, TP problem)
//   {
//     var r = currentIterationResult;
//     if (inner != null) {
//       r = inner.Transform(currentIterationResult, previousIterationResult, encoding, problem);
//     }
//
//     var wasTrue = Interlocked.Exchange(ref requireReevaluation, 0) != 0;
//     if (!wasTrue) {
//       return r;
//     }
//
//     var genotypes = r.Population.Genotypes.ToArray();
//     var fitnesses = evaluator.Evaluate(genotypes, null!, encoding, problem); //TODO: pass random?
//     r = r with { Population = Population.From(genotypes, fitnesses) };
//
//     return r;
//   }
// }
