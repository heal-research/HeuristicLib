using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record class Algorithm<TGenotype, TEncoding, TState, TAlgorithmResult, TAlgorithmInstance> 
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IAlgorithmResult
  where TAlgorithmInstance : IAlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult>
{
  public abstract TAlgorithmInstance CreateInstance();

  public virtual TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
    var algorithmInstance = CreateInstance();
    return algorithmInstance.Execute(problem, initialState);
  }
}

public interface IAlgorithmInstance<out TGenotype, in TEncoding, in TState, out TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IAlgorithmResult
{
  TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
}

public abstract class AlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult, TAlgorithm> : IAlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IAlgorithmResult
{
  public TAlgorithm Parameters { get; }
  
  protected AlgorithmInstance(TAlgorithm parameters) {
    Parameters = parameters;
  }
  
  public abstract TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
}


public static class AlgorithmSolveExtensions {
  public static EvaluatedIndividual<TGenotype, TPhenotype>? Solve<TGenotype, TPhenotype, TEncoding, TState, TAlgorithmResult, TAlgorithmInstance>
    (this Algorithm<TGenotype, TEncoding, TState, TAlgorithmResult, TAlgorithmInstance> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>
    where TAlgorithmInstance : IAlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult>
  {
    var instance = algorithm.CreateInstance();
    return instance.Solve(problem, initialState);
  }
  
  public static EvaluatedIndividual<TGenotype, TPhenotype>? Solve<TGenotype, TPhenotype, TEncoding, TState, TAlgorithmResult>
    (this IAlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TAlgorithmResult : class, ISingleObjectiveAlgorithmResult<TGenotype>
  {
    var result = algorithm.Execute(problem, initialState);
    var bestSolution = result.BestSolution;
    
    if (bestSolution is null) return null;
    var phenotype = problem.Decoder.Decode(bestSolution.Genotype);

    return new EvaluatedIndividual<TGenotype, TPhenotype>(bestSolution.Genotype, phenotype, bestSolution.Fitness);
  }

  public static IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>> SolvePareto<TGenotype, TPhenotype, TEncoding, TState, TAlgorithmResult, TAlgorithmInstance>
    (this Algorithm<TGenotype, TEncoding, TState, TAlgorithmResult, TAlgorithmInstance> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype>
    where TAlgorithmInstance : IAlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult>
  {
    var instance = algorithm.CreateInstance();
    return instance.SolvePareto(problem, initialState);
  }
  
  public static IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>> SolvePareto<TGenotype, TPhenotype, TEncoding, TState, TAlgorithmResult>
    (this IAlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TAlgorithmResult : class, IMultiObjectiveAlgorithmResult<TGenotype> 
  {
    var result = algorithm.Execute(problem, initialState);
    var paretoFront = result.ParetoFront;

    return paretoFront
      .Select(individual => new EvaluatedIndividual<TGenotype, TPhenotype>(individual.Genotype, problem.Decoder.Decode(individual.Genotype), individual.Fitness))
      .ToList();
  }
}


public abstract record class StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  : Algorithm<TGenotype, TEncoding, TState, TAlgorithmResult, IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult
  // where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
{
  public virtual IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
    var algorithmInstance = CreateInstance();
    return algorithmInstance.ExecuteStreaming(problem, initialState);
  }
}

public interface IStreamableAlgorithmInstance<out TGenotype, in TEncoding, in TState, out TIterationResult, out TAlgorithmResult>
  : IAlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult
{
  IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
}

public abstract class StreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, TAlgorithm>
  : AlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult, TAlgorithm>, IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult
{
  protected StreamableAlgorithmInstance(TAlgorithm parameters) : base(parameters) {}
  public abstract IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
}

public static class AlgorithmSolveStreamingExtensions {
  public static IEnumerable<EvaluatedIndividual<TGenotype, TPhenotype>> SolveStreaming<TGenotype, TPhenotype, TEncoding, TState, TIterationResult, TAlgorithmResult/*, TAlgorithmInstance*/>
    (this StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult/*, TAlgorithmInstance*/> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TIterationResult : class, ISingleObjectiveIterationResult<TGenotype>, IContinuableIterationResult<TState>
    where TAlgorithmResult : class, IAlgorithmResult
    /*where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> */
  {
    var instance = algorithm.CreateInstance();
    return instance.SolveStreaming(problem, initialState);
  }
  
  public static IEnumerable<EvaluatedIndividual<TGenotype, TPhenotype>> SolveStreaming<TGenotype, TPhenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
    (this IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TIterationResult : class, ISingleObjectiveIterationResult<TGenotype>, IContinuableIterationResult<TState>
    where TAlgorithmResult : class, IAlgorithmResult
  {
    foreach (var iterationResult in algorithm.ExecuteStreaming(problem, initialState)) {
      var bestSolution = iterationResult.BestSolution;
      yield return new EvaluatedIndividual<TGenotype, TPhenotype>(bestSolution.Genotype, problem.Decoder.Decode(bestSolution.Genotype), bestSolution.Fitness);
    }
  }
  
  public static IEnumerable<IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>>> SolveParetoStreaming<TGenotype, TPhenotype, TEncoding, TState, TIterationResult, TAlgorithmResult/*, TAlgorithmInstance*/>
    (this StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult/*, TAlgorithmInstance*/> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TIterationResult : class, IMultiObjectiveIterationResult<TGenotype>, IContinuableIterationResult<TState>
    where TAlgorithmResult : class, IAlgorithmResult
    /*where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>*/
  {
    var instance = algorithm.CreateInstance();
    return instance.SolveParetoStreaming(problem, initialState);
  }
  
  public static IEnumerable<IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>>> SolveParetoStreaming<TGenotype, TPhenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
    (this IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TIterationResult : class, IMultiObjectiveIterationResult<TGenotype>, IContinuableIterationResult<TState>
    where TAlgorithmResult : class, IAlgorithmResult
  {
    foreach (var iterationResult in algorithm.ExecuteStreaming(problem, initialState)) {
      yield return iterationResult
        .ParetoFront
        .Select(individual => new EvaluatedIndividual<TGenotype, TPhenotype>(individual.Genotype, problem.Decoder.Decode(individual.Genotype), individual.Fitness))
        .ToList();
    }
  }
}

//
// public abstract record class Algorithm<TGenotype, TEncoding, TState, TAlgorithmResult, TAlgorithmInstance> 
//   : IAlgorithm<TGenotype, TEncoding, TState, TAlgorithmResult, TAlgorithmInstance>
//   where TEncoding : IEncoding<TGenotype>
//   where TState : class
//   where TAlgorithmResult : class, IAlgorithmResult
//   where TAlgorithmInstance : IAlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult>
// {
//   //public abstract TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
// }
//
// public abstract class AlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult>
//   : IAlgorithmInstance<TGenotype, TEncoding, TState, TAlgorithmResult>
//   where TEncoding : IEncoding<TGenotype>
//   where TState : class
//   where TAlgorithmResult : class, IAlgorithmResult 
// {
//   
// }
//
// public abstract record class StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, TAlgorithmInstance>
//   : IStreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, TAlgorithmInstance>
//   where TEncoding : IEncoding<TGenotype>
//   where TState : class
//   where TIterationResult : class, IContinuableIterationResult<TState>
//   where TAlgorithmResult : class, IAlgorithmResult
//   where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> 
// {
//   
// }
//
// public abstract class StreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
//   : IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
//   where TEncoding : IEncoding<TGenotype>
//   where TState : class
//   where TIterationResult : class, IContinuableIterationResult<TState>
//   where TAlgorithmResult : class, IAlgorithmResult
// {
//   
// }

public abstract record class IterativeAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult/*, TAlgorithmInstance*/>
  : StreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult/*, TAlgorithmInstance*/>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult
  // where TAlgorithmInstance : IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult> 
{
  public Terminator<TIterationResult> Terminator { get; }

  protected IterativeAlgorithm(Terminator<TIterationResult> terminator) {
    Terminator = terminator;
  }
}

// public interface IIterativeAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
//   : IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
//   where TEncoding : IEncoding<TGenotype>
//   where TState : class
//   where TIterationResult : class, IContinuableIterationResult<TState>
//   where TAlgorithmResult : class, IAlgorithmResult 
// {
//   ITerminatorInstance<TIterationResult> Terminator { get; set; }
// }

public abstract class IterativeAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, TAlgorithm>
  : StreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, TAlgorithm>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult
  where TAlgorithm : IterativeAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  /*where TAlgorithm : IterativeAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult, IStreamableAlgorithmInstance<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>>*/
{
  public ITerminatorInstance<TIterationResult> Terminator { get; set; }

  protected IterativeAlgorithmInstance(TAlgorithm parameters) : base(parameters) {
    Terminator = parameters.Terminator.CreateInstance();
  }
  
  public override IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
    var currentIterationResult = initialState is null
      ? ExecuteInitialization(problem) 
      : ExecuteIteration(problem, initialState);
    yield return currentIterationResult;

    while (Terminator.ShouldContinue(currentIterationResult)) {
      var currentState = currentIterationResult.GetContinuationState();
      currentIterationResult = ExecuteIteration(problem, currentState);
      yield return currentIterationResult;
    }
  }
  
  protected abstract TIterationResult ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem);
  
  protected abstract TIterationResult ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState state);
}
