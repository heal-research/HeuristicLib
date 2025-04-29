using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record class Algorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithmInstance> 
  where TSearchSpace : ISearchSpace<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IAlgorithmResult
  where TAlgorithmInstance : IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
{
  public abstract TAlgorithmInstance CreateInstance();

  public virtual TAlgorithmResult Execute(IOptimizable<TGenotype, TSearchSpace> optimizable, TState? initialState = null) {
    var algorithmInstance = CreateInstance();
    return algorithmInstance.Execute(optimizable, initialState);
  }
}

public interface IAlgorithmInstance<out TGenotype, in TSearchSpace, in TState, out TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IAlgorithmResult
{
  TAlgorithmResult Execute(IOptimizable<TGenotype, TSearchSpace> optimizable, TState? initialState = null);
}

public abstract class AlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithm> : IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IAlgorithmResult
{
  public TAlgorithm Parameters { get; }
  
  protected AlgorithmInstance(TAlgorithm parameters) {
    Parameters = parameters;
  }
  
  public abstract TAlgorithmResult Execute(IOptimizable<TGenotype, TSearchSpace> optimizable, TState? initialState = null);
}


public static class AlgorithmSolveExtensions {
  public static EvaluatedIndividual<TGenotype, TPhenotype>? Solve<TGenotype, TPhenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithmInstance>
    (this Algorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithmInstance> algorithm, IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TState : class
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>
    where TAlgorithmInstance : IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
  {
    var instance = algorithm.CreateInstance();
    return instance.Solve(problem, initialState);
  }
  
  public static EvaluatedIndividual<TGenotype, TPhenotype>? Solve<TGenotype, TPhenotype, TSearchSpace, TState, TAlgorithmResult>
    (this IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TState : class
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>
  {
    var result = algorithm.Execute(problem, initialState);
    var bestSolution = result.CurrentBestSolution;
    
    if (bestSolution is null) return null;
    var phenotype = problem.Decode(bestSolution.Genotype);

    return new EvaluatedIndividual<TGenotype, TPhenotype>(bestSolution.Genotype, phenotype, bestSolution.Fitness);
  }

  public static IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>> SolvePareto<TGenotype, TPhenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithmInstance>
    (this Algorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithmInstance> algorithm, IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TState : class
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype>
    where TAlgorithmInstance : IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
  {
    var instance = algorithm.CreateInstance();
    return instance.SolvePareto(problem, initialState);
  }
  
  public static IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>> SolvePareto<TGenotype, TPhenotype, TSearchSpace, TState, TAlgorithmResult>
    (this IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TState : class
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype> 
  {
    var result = algorithm.Execute(problem, initialState);
    var paretoFront = result.CurrentParetoFront;

    return paretoFront
      .Select(individual => new EvaluatedIndividual<TGenotype, TPhenotype>(individual.Genotype, problem.Decode(individual.Genotype), individual.Fitness))
      .ToList();
  }
}


public abstract record class StreamableAlgorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult>
  : Algorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult, IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
  // where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
{
  public virtual IEnumerable<TAlgorithmResult> ExecuteStreaming(IOptimizable<TGenotype, TSearchSpace> optimizable, TState? initialState = null) {
    var algorithmInstance = CreateInstance();
    return algorithmInstance.ExecuteStreaming(optimizable, initialState);
  }
}

public interface IStreamableAlgorithmInstance<out TGenotype, in TSearchSpace, in TState, out TAlgorithmResult>
  : IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
{
  IEnumerable<TAlgorithmResult> ExecuteStreaming(IOptimizable<TGenotype, TSearchSpace> optimizable, TState? initialState = null);
}

public abstract class StreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithm>
  : AlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithm>, IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
{
  protected StreamableAlgorithmInstance(TAlgorithm parameters) : base(parameters) {}
  public abstract IEnumerable<TAlgorithmResult> ExecuteStreaming(IOptimizable<TGenotype, TSearchSpace> optimizable, TState? initialState = null);
}

public static class AlgorithmSolveStreamingExtensions {
  public static IEnumerable<EvaluatedIndividual<TGenotype, TPhenotype>> SolveStreaming<TGenotype, TPhenotype, TSearchSpace, TState, TAlgorithmResult>
    (this StreamableAlgorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TState : class
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<TState>
  {
    var instance = algorithm.CreateInstance();
    return instance.SolveStreaming(problem, initialState);
  }
  
  public static IEnumerable<EvaluatedIndividual<TGenotype, TPhenotype>> SolveStreaming<TGenotype, TPhenotype, TSearchSpace, TState, TAlgorithmResult>
    (this IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TState : class
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<TState>
  {
    foreach (var iterationResult in algorithm.ExecuteStreaming(problem, initialState)) {
      var bestSolution = iterationResult.CurrentBestSolution;
      yield return new EvaluatedIndividual<TGenotype, TPhenotype>(bestSolution.Genotype, problem.Decode(bestSolution.Genotype), bestSolution.Fitness);
    }
  }
  
  public static IEnumerable<IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>>> SolveParetoStreaming<TGenotype, TPhenotype, TSearchSpace, TState, TAlgorithmResult>
    (this StreamableAlgorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TState : class
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<TState>
    /*where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>*/
  {
    var instance = algorithm.CreateInstance();
    return instance.SolveParetoStreaming(problem, initialState);
  }
  
  public static IEnumerable<IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>>> SolveParetoStreaming<TGenotype, TPhenotype, TSearchSpace, TState, TAlgorithmResult>
    (this IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null)
    where TSearchSpace : ISearchSpace<TGenotype>
    where TState : class
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype>, IContinuableAlgorithmResult<TState>
  {
    foreach (var iterationResult in algorithm.ExecuteStreaming(problem, initialState)) {
      yield return iterationResult
        .CurrentParetoFront
        .Select(individual => new EvaluatedIndividual<TGenotype, TPhenotype>(individual.Genotype, problem.Decode(individual.Genotype), individual.Fitness))
        .ToList();
    }
  }
}

//
// public abstract record class Algorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithmInstance> 
//   : IAlgorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithmInstance>
//   where TSearchSpace : IEncoding<TGenotype>
//   where TState : class
//   where TAlgorithmResult : class, IAlgorithmResult
//   where TAlgorithmInstance : IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
// {
//   //public abstract TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TSearchSpace> problem, TState? initialState = null);
// }
//
// public abstract class AlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
//   : IAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult>
//   where TSearchSpace : IEncoding<TGenotype>
//   where TState : class
//   where TAlgorithmResult : class, IAlgorithmResult 
// {
//   
// }
//
// public abstract record class StreamableAlgorithm<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult, TAlgorithmInstance>
//   : IStreamableAlgorithm<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult, TAlgorithmInstance>
//   where TSearchSpace : IEncoding<TGenotype>
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
//   where TSearchSpace : IEncoding<TGenotype>
//   where TState : class
//   where TIterationResult : class, IContinuableIterationResult<TState>
//   where TAlgorithmResult : class, IAlgorithmResult
// {
//   
// }

public abstract record class IterativeAlgorithm<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
  : StreamableAlgorithm<TGenotype, TSearchSpace, TState, TAlgorithmResult>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
{
  public Terminator<TAlgorithmResult> Terminator { get; init; }

  protected IterativeAlgorithm(Terminator<TAlgorithmResult> terminator) {
    Terminator = terminator;
  }
}

// public interface IIterativeAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
//   : IStreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
//   where TSearchSpace : IEncoding<TGenotype>
//   where TState : class
//   where TIterationResult : class, IContinuableIterationResult<TState>
//   where TAlgorithmResult : class, IAlgorithmResult 
// {
//   ITerminatorInstance<TIterationResult> Terminator { get; set; }
// }

public abstract class IterativeAlgorithmInstance<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult, TAlgorithm>
  : StreamableAlgorithmInstance<TGenotype, TSearchSpace, TState, TAlgorithmResult, TAlgorithm>
  where TSearchSpace : ISearchSpace<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IContinuableAlgorithmResult<TState>
  where TAlgorithm : IterativeAlgorithm<TGenotype, TSearchSpace, TState, TIterationResult, TAlgorithmResult>
{
  public ITerminatorInstance<TAlgorithmResult> Terminator { get; set; }

  protected IterativeAlgorithmInstance(TAlgorithm parameters) : base(parameters) {
    Terminator = parameters.Terminator.CreateInstance();
  }

  public override TAlgorithmResult Execute(IOptimizable<TGenotype, TSearchSpace> optimizable, TState? initialState = null) {
    return ExecuteStreaming(optimizable, initialState).Last();
  }

  public override IEnumerable<TAlgorithmResult> ExecuteStreaming(IOptimizable<TGenotype, TSearchSpace> optimizable, TState? initialState = null) {
    var currentIterationResult = initialState is null
      ? ExecuteInitialization(optimizable) 
      : ExecuteIteration(optimizable, initialState);
    TAlgorithmResult? currentAlgorithmResult = null;
    currentAlgorithmResult = AggregateResult(currentIterationResult, currentAlgorithmResult);
    yield return currentAlgorithmResult;
    
    while (Terminator.ShouldContinue(currentAlgorithmResult)) {
      var currentState = currentAlgorithmResult.GetContinuationState();
      currentIterationResult = ExecuteIteration(optimizable, currentState);
      currentAlgorithmResult = AggregateResult(currentIterationResult, currentAlgorithmResult);
      yield return currentAlgorithmResult;
    }
  }
  
  protected abstract TIterationResult ExecuteInitialization(IOptimizable<TGenotype, TSearchSpace> optimizable);
  
  protected abstract TIterationResult ExecuteIteration(IOptimizable<TGenotype, TSearchSpace> optimizable, TState state);
  
  protected abstract TAlgorithmResult AggregateResult(TIterationResult iterationResult, TAlgorithmResult? algorithmResult);
}
