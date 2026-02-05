using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Observation;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract class IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>,
    IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{
  //public int? MaximumIterations { get; init; }
  //public ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> Terminator { get; init; } = new NeverTerminator<TGenotype>();
  public IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor { get; init; }

  

  // private ValueTask<TAlgorithmState> ExecuteStepAsync(TProblem problem, TAlgorithmState? previousState, IRandomNumberGenerator random)
  // {
  //   return new ValueTask<TAlgorithmState>(ExecuteStep(problem, previousState, random));
  // }
}

public abstract class IterativeAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  : AlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>,
    IIterativeAlgorithmInstance<TGenotype, TSearchSpace, TProblem, TAlgorithmState>
  where TGenotype : class
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TAlgorithmState : class, IAlgorithmState
{

  protected readonly IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? Interceptor;

  protected IterativeAlgorithmInstance(IInterceptor<TGenotype, TAlgorithmState, TSearchSpace, TProblem>? interceptor,
    IEvaluator<TGenotype, TSearchSpace, TProblem> evaluator, IIterationObserver<TGenotype, TSearchSpace, TProblem, TAlgorithmState>? observer)
    : base(evaluator, observer)
  {
    Interceptor = interceptor;
  }

  // public override TAlgorithmState Transform(TAlgorithmState? state, IRandomNumberGenerator randomNumberGenerator, TSearchSpace searchSpace, TProblem problem)
  // {
  //   return ExecuteStep(state, problem, randomNumberGenerator);
  // }

  public abstract TAlgorithmState ExecuteStep(TAlgorithmState? previousState, TProblem problem, IRandomNumberGenerator random);

  public override async IAsyncEnumerable<TAlgorithmState> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random, TAlgorithmState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
  {
    var previousState = initialState;

    foreach (var currentIteration in Enumerable.InfiniteSequence(0, 1)) {
      ct.ThrowIfCancellationRequested();
      var iterationRandom = random.Fork(currentIteration);
      var newState = ExecuteStep(previousState, problem, iterationRandom);
      if (Interceptor is not null) {
        newState = Interceptor.Transform(newState, previousState, problem.SearchSpace, problem);
      }

      Observer?.OnIterationCompleted(newState, previousState, problem.SearchSpace, problem);

      yield return newState;
      
      await Task.Yield();
      
      previousState = newState;
    }
  }
}

public static class IterativeAlgorithmExtensions
{
  extension<TGenotype, TSearchSpace, TProblem, TAlgorithmState>(IIterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TAlgorithmState> algorithm)
    where TGenotype : class
    where TSearchSpace : class, ISearchSpace<TGenotype>
    where TProblem : class, IProblem<TGenotype, TSearchSpace>
    where TAlgorithmState : class, IAlgorithmState
  {
   
  }
}
