using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TEncoding, in TState, out TAlgorithmResult> 
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IAlgorithmResult
{
  TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
}

public static class AlgorithmSolveExtensions {
  public static EvaluatedIndividual<TGenotype, TPhenotype>? Solve<TGenotype, TPhenotype, TEncoding, TState, TResult>
    (this IAlgorithm<TGenotype, TEncoding, TState, TResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TResult : class, ISingleObjectiveAlgorithmResult<TGenotype>
  {
    var result = algorithm.Execute(problem, initialState);
    var bestSolution = result.BestSolution;
    
    if (bestSolution is null) return null;
    var phenotype = problem.Decoder.Decode(bestSolution.Genotype);

    return new EvaluatedIndividual<TGenotype, TPhenotype>(bestSolution.Genotype, phenotype, bestSolution.Fitness);
  }

  public static IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>> SolvePareto<TGenotype, TPhenotype, TEncoding, TState, TResult>
    (this IAlgorithm<TGenotype, TEncoding, TState, TResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TResult : class, IMultiObjectiveAlgorithmResult<TGenotype> 
  {
    var result = algorithm.Execute(problem, initialState);
    var paretoFront = result.ParetoFront;

    return paretoFront
      .Select(individual => new EvaluatedIndividual<TGenotype, TPhenotype>(individual.Genotype, problem.Decoder.Decode(individual.Genotype), individual.Fitness))
      .ToList();
  }
}

public interface IStreamableAlgorithm<TGenotype, in TEncoding, in TState, out TIterationResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
{
  IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
}

public static class AlgorithmSolveStreamingExtensions {
  public static IEnumerable<EvaluatedIndividual<TGenotype, TPhenotype>> SolveStreaming<TGenotype, TPhenotype, TEncoding, TState, TResult>
    (this IStreamableAlgorithm<TGenotype, TEncoding, TState, TResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TResult : class, ISingleObjectiveIterationResult<TGenotype>, IContinuableIterationResult<TState>
  {
    foreach (var iterationResult in algorithm.ExecuteStreaming(problem, initialState)) {
      var bestSolution = iterationResult.BestSolution;
      yield return new EvaluatedIndividual<TGenotype, TPhenotype>(bestSolution.Genotype, problem.Decoder.Decode(bestSolution.Genotype), bestSolution.Fitness);
    }
  }
  
  public static IEnumerable<IReadOnlyList<EvaluatedIndividual<TGenotype, TPhenotype>>> SolveParetoStreaming<TGenotype, TPhenotype, TEncoding, TState, TResult>
    (this IStreamableAlgorithm<TGenotype, TEncoding, TState, TResult> algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
    where TEncoding : IEncoding<TGenotype>
    where TState : class
    where TResult : class, IMultiObjectiveIterationResult<TGenotype>, IContinuableIterationResult<TState> 
  {
    foreach (var iterationResult in algorithm.ExecuteStreaming(problem, initialState)) {
      yield return iterationResult
        .ParetoFront
        .Select(individual => new EvaluatedIndividual<TGenotype, TPhenotype>(individual.Genotype, problem.Decoder.Decode(individual.Genotype), individual.Fitness))
        .ToList();
    }
  }
}

public abstract class Algorithm<TGenotype, TEncoding, TState, TAlgorithmResult> 
  : IAlgorithm<TGenotype, TEncoding, TState, TAlgorithmResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TAlgorithmResult : class, IAlgorithmResult
{
  public abstract TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
}

public abstract class IterativeAlgorithm<TGenotype, TEncoding, TState, TIterationResult, TAlgorithmResult>
  : Algorithm<TGenotype, TEncoding, TState, TAlgorithmResult>, 
    IStreamableAlgorithm<TGenotype, TEncoding, TState, TIterationResult>
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
  where TAlgorithmResult : class, IAlgorithmResult
{
  public ITerminator<TIterationResult> Terminator { get; }

  protected IterativeAlgorithm(ITerminator<TIterationResult> terminator) {
    Terminator = terminator;
  }
  
  public virtual IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
    var currentIterationResult = initialState is null
      ? ExecuteInitialization(problem) 
      : ExecuteIteration(problem, initialState);
    yield return currentIterationResult;

    while (Terminator.ShouldContinue(currentIterationResult)) {
      var currentState = currentIterationResult.GetState();
      currentIterationResult = ExecuteIteration(problem, currentState);
      yield return currentIterationResult;
    }
  }
  
  protected abstract TIterationResult ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem);
  
  protected abstract TIterationResult ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState state);
}
