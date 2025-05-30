using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record class Algorithm<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TState : class, IAlgorithmState
  where TAlgorithmResult : class, IAlgorithmResult
{
  public abstract IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> CreateExecution(TProblem optimizable);

  // // Helper. Maybe remove, maybe to extension method?
  // public virtual TAlgorithmResult Execute(TProblem problem, TState? initialState = null) {
  //   var algorithmInstance = CreateExecution(problem);
  //   return algorithmInstance.Start(initialState);
  // }
}

public interface IAlgorithmExecution<out TGenotype, in TSearchSpace, in TProblem, in TState, out TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TState : class, IAlgorithmState
  where TAlgorithmResult : class, IAlgorithmResult
{
  TAlgorithmResult Execute(TState? initialState = null);
  // ToDo: stop pause ???
}

public abstract class AlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult, TAlgorithm> 
  : IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TState : class, IAlgorithmState
  where TAlgorithmResult : class, IAlgorithmResult
  // where TOptimizable : IOptimizable<TGenotype, TSearchSpace>
{
  public TAlgorithm Parameters { get; }
  public TProblem Problem { get; }
  
  protected AlgorithmExecution(TAlgorithm parameters, TProblem problem) {
    Parameters = parameters;
    Problem = problem;
  }
  
  public abstract TAlgorithmResult Execute(TState? initialState = null);
}


public static class AlgorithmSolveExtensions {
  public static Solution<TGenotype>? Solve<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
    (this Algorithm<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> algorithm, TProblem problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
    where TState : class, IAlgorithmState
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>
  {
    var instance = algorithm.CreateExecution(problem);
    return instance.Solve(initialState);
  }
  
  public static Solution<TGenotype>? Solve<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
    (this IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> algorithmExecution, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
    where TState : class, IAlgorithmState
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>
  {
    var result = algorithmExecution.Execute(initialState);
    var bestSolution = result.BestSolution;

    return bestSolution;
    //
    // if (bestSolution is null) return null;
    // // var phenotype = problem.Decode(bestSolution.Genotype);
    //
    // return new Solution<TGenotype>(bestSolution.Genotype, bestSolution.Fitness);
  }

  public static IReadOnlyList<Solution<TGenotype>> SolvePareto<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
    (this Algorithm<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> algorithm, TProblem problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
    where TState : class, IAlgorithmState
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype>
  {
    var instance = algorithm.CreateExecution(problem);
    return instance.SolvePareto(initialState);
  }
  
  public static IReadOnlyList<Solution<TGenotype>> SolvePareto<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
    (this IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> algorithmExecution, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
    where TState : class, IAlgorithmState
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype> 
  {
    var result = algorithmExecution.Execute(initialState);
    var paretoFront = result.ParetoFront;

    return paretoFront;
    //
    // return paretoFront
    //   .Select(individual => new Solution<TGenotype, TPhenotype>(individual.Genotype, problem.Decode(individual.Genotype), individual.Fitness))
    //   .ToList();
  }
}


public abstract record class StreamableAlgorithm<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
  : Algorithm<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult/*, IStreamableAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>*/>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TState : class, IAlgorithmState
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
  // where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
{
  public override IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> CreateExecution(TProblem optimizable) {
    return CreateStreamingExecution(optimizable);
  }
  public abstract IStreamableAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> CreateStreamingExecution(TProblem optimizable);
  
  // ToDo: helper, maybe remove, maybe to extension method?
  public virtual IEnumerable<TAlgorithmResult> ExecuteStreaming(TProblem optimizable, TState? initialState = null) {
    var algorithmInstance = CreateStreamingExecution(optimizable);
    return algorithmInstance.ExecuteStreaming(initialState);
  }
}

public interface IStreamableAlgorithmExecution<out TGenotype, in TSearchSpace, in TProblem, in TState, out TAlgorithmResult>
  : IAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TState : class, IAlgorithmState
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
{
  IEnumerable<TAlgorithmResult> ExecuteStreaming(TState? initialState = null);
}

public abstract class StreamableAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult, TAlgorithm>
  : AlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult, TAlgorithm>, IStreamableAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TState : class, IAlgorithmState
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
{
  protected StreamableAlgorithmExecution(TAlgorithm parameters, TProblem problem) : base(parameters, problem) {}
  public abstract IEnumerable<TAlgorithmResult> ExecuteStreaming(TState? initialState = null);
}

public static class AlgorithmSolveStreamingExtensions {
  public static IEnumerable<Solution<TGenotype>> SolveStreaming<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
    (this StreamableAlgorithm<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> algorithm, TProblem problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
    where TState : class, IAlgorithmState
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<TState>
  {
    var instance = algorithm.CreateStreamingExecution(problem);
    return instance.SolveStreaming(initialState);
  }
  
  public static IEnumerable<Solution<TGenotype>> SolveStreaming<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
    (this IStreamableAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> algorithmExecution, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
    where TState : class, IAlgorithmState
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<TState>
  {
    foreach (var iterationResult in algorithmExecution.ExecuteStreaming(initialState)) {
      var bestSolution = iterationResult.BestSolution;

      yield return bestSolution;
      // yield return new Solution<TGenotype, TPhenotype>(bestSolution.Genotype, problem.Decode(bestSolution.Genotype), bestSolution.Fitness);
    }
  }
  
  public static IEnumerable<IReadOnlyList<Solution<TGenotype>>> SolveParetoStreaming<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
    (this StreamableAlgorithm<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> algorithm, TProblem problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
    where TState : class, IAlgorithmState
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<TState>
    /*where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>*/
  {
    var instance = algorithm.CreateStreamingExecution(problem);
    return instance.SolveParetoStreaming(initialState);
  }
  
  public static IEnumerable<IReadOnlyList<Solution<TGenotype>>> SolveParetoStreaming<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
    (this IStreamableAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult> algorithmExecution, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TProblem : IOptimizable<TGenotype, TSearchSpace>
    where TState : class, IAlgorithmState
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<TState>
  {
    foreach (var iterationResult in algorithmExecution.ExecuteStreaming(initialState)) {
      var paretoFront = iterationResult.ParetoFront;

      yield return paretoFront;
      //
      // yield return iterationResult
      //   .ParetoFront
      //   .Select(individual => new Solution<TGenotype, TPhenotype>(individual.Genotype, problem.Decode(individual.Genotype), individual.Fitness))
      //   .ToList();
    }
  }
}

//
// public abstract record class Algorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithmInstance> 
//   : IAlgorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithmInstance>
//   where TSearchSpace : ISearchSpace<TGenotype>
//   where TState : class
//   where TAlgorithmResult : class, IAlgorithmResult
//   where TAlgorithmInstance : IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
// {
//   //public abstract TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null);
// }
//
// public abstract class AlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
//   : IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
//   where TSearchSpace : ISearchSpace<TGenotype>
//   where TState : class
//   where TAlgorithmResult : class, IAlgorithmResult 
// {
//   
// }
//
// public abstract record class StreamableAlgorithm<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult, TAlgorithmInstance>
//   : IStreamableAlgorithm<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult, TAlgorithmInstance>
//   where TSearchSpace : ISearchSpace<TGenotype>
//   where TState : class
//   where TIterationResult : class, IContinuableIterationResult<TState>
//   where TAlgorithmResult : class, IAlgorithmResult
//   where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult> 
// {
//   
// }
//
// public abstract class StreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
//   : IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
//   where TSearchSpace : ISearchSpace<TGenotype>
//   where TState : class
//   where TIterationResult : class, IContinuableIterationResult<TState>
//   where TAlgorithmResult : class, IAlgorithmResult
// {
//   
// }

public abstract record class IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TState, TIterationResult, TAlgorithmResult>
  : StreamableAlgorithm<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TState : class, IAlgorithmState
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
{
  public Terminator<TGenotype, TSearchSpace, TProblem, TAlgorithmResult> Terminator { get; init; }

  protected IterativeAlgorithm(Terminator<TGenotype, TSearchSpace, TProblem, TAlgorithmResult> terminator) {
    Terminator = terminator;
  }
}

// public interface IIterativeAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
//   : IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
//   where TSearchSpace : ISearchSpace<TGenotype>
//   where TState : class
//   where TIterationResult : class, IContinuableIterationResult<TState>
//   where TAlgorithmResult : class, IAlgorithmResult 
// {
//   ITerminatorInstance<TIterationResult> Terminator { get; set; }
// }

public abstract class IterativeAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TIterationResult, TAlgorithmResult, TAlgorithm>
  : StreamableAlgorithmExecution<TGenotype, TSearchSpace, TProblem, TState, TAlgorithmResult, TAlgorithm>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TState : class, IAlgorithmState
  where TProblem : IOptimizable<TGenotype, TSearchSpace>
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
  where TAlgorithm : IterativeAlgorithm<TGenotype, TSearchSpace, TProblem, TState, TIterationResult, TAlgorithmResult>
  
{
  public ITerminatorInstance<TAlgorithmResult> Terminator { get; set; }

  protected IterativeAlgorithmExecution(TAlgorithm parameters, TProblem problem) : base(parameters, problem) {
    Terminator = parameters.Terminator.CreateExecution(problem.SearchSpace, problem);
  }

  public override TAlgorithmResult Execute(TState? initialState = null) {
    return ExecuteStreaming(initialState).Last();
  }

  public override IEnumerable<TAlgorithmResult> ExecuteStreaming(TState? initialState = null) {
    TIterationResult currentIterationResult;
    if (initialState is null) {
      currentIterationResult = ExecuteInitialization();
    } else {
      if (!IsValidState(initialState)) {
        throw new ArgumentException("Invalid initial state", nameof(initialState));
      }
      currentIterationResult = ExecuteIteration(initialState);
    }
    TAlgorithmResult? currentAlgorithmResult = null;
    currentAlgorithmResult = AggregateResult(currentIterationResult, currentAlgorithmResult);
    yield return currentAlgorithmResult;
    
    while (Terminator.ShouldContinue(currentAlgorithmResult)) {
      var currentState = currentAlgorithmResult.GetContinuationState();
      currentIterationResult = ExecuteIteration(currentState);
      currentAlgorithmResult = AggregateResult(currentIterationResult, currentAlgorithmResult);
      yield return currentAlgorithmResult;
    }
  }


  
  protected abstract TIterationResult ExecuteInitialization();
  
  protected abstract TIterationResult ExecuteIteration(TState state);
  
  protected abstract TAlgorithmResult AggregateResult(TIterationResult iterationResult, TAlgorithmResult? algorithmResult);
  
  protected virtual bool IsValidState(TState state) {
    return true;
  }
}
