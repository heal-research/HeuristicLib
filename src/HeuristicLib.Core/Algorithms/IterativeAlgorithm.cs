using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>, IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  public int? MaximumIterations { get; init; }
  
  public abstract TAlgorithmState ExecuteStep(TProblem problem, TAlgorithmState? previousState, IRandomNumberGenerator random);

  public virtual ValueTask<TAlgorithmState> ExecuteStepAsync(TProblem problem, TAlgorithmState? previousState, IRandomNumberGenerator random)
  {
    var result = ExecuteStep(problem, previousState, random);
    return new ValueTask<TAlgorithmState>(result);
  }

  public override Execution CreateExecution() => new(this);
  
  public new class Execution : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>.Execution
  {
    private readonly IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm;
    
    public int CurrentGeneration { get; set; } = 0;


    public Execution(IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm)
    {
      this.algorithm = algorithm;
    }
    
    public override async IAsyncEnumerable<TAlgorithmState> ExecuteStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
      var previousState = initialState;
      var shouldContinue = previousState is null || ShouldContinue(previousState, null, problem.SearchSpace, problem);

      while (shouldContinue) {
        ct.ThrowIfCancellationRequested();
        var newIterationState = await algorithm.ExecuteStepAsync(problem, previousState, random);
        if (algorithm.Interceptor is not null) {
          newIterationState = algorithm.Interceptor.Transform(newIterationState, previousState, problem.SearchSpace, problem);
        }

        algorithm.Observer?.OnIterationCompleted(newIterationState, previousState, problem.SearchSpace, problem);

        yield return newIterationState;
        
        CurrentGeneration++;
        
        await Task.Yield();

        shouldContinue = ShouldContinue(newIterationState, previousState, problem.SearchSpace, problem);
        previousState = newIterationState;
      }
    }

    protected virtual bool ShouldContinue(TAlgorithmState currentIterationState, TAlgorithmState? previousIterationState, TSearchSpace searchSpace, TProblem problem)
    {
      if (algorithm.MaximumIterations.HasValue && CurrentGeneration >= algorithm.MaximumIterations.Value) {
        return false;
      }

      return algorithm.Terminator.ShouldContinue(currentIterationState, previousIterationState, searchSpace, problem);
    }
  }
}
