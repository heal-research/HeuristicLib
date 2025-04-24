using HEAL.HeuristicLib.Encodings;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithm<TGenotype, in TEncoding, in TState, out TAlgorithmResult> 
  where TEncoding : IEncoding<TGenotype>
  where TState : class//, IState
  where TAlgorithmResult : class, IAlgorithmResult
{
  TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);

  //EvaluatedIndividual<TGenotype, TPhenotype> Solve<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
  
  // TRichResultState ExecuteRich<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null)
  //   where TRichResultState : class, IContinuableResultState<TState>;
}

// public interface IState {
// }

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

public interface IStreamableAlgorithm<TGenotype, in TEncoding, in TState, out TIterationResult>/* : IAlgorithm<TGenotype, TEncoding, TState>*/
  where TEncoding : IEncoding<TGenotype>
  where TState : class
  where TIterationResult : class, IContinuableIterationResult<TState>
{
  //IEnumerable<EvaluatedIndividual<TGenotype, TPhenotype>> SolveStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);

  IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
  
  //IEnumerable<EvaluatedIndividual<TGenotype, TPhenotype>> SolveStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
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

//
// public interface IAlgorithm { }
//
// public interface IAlgorithm<out TGenotype, in TEncoding> : IAlgorithm
//   where TEncoding : IEncoding<TGenotype>
// {
//   (TPhenotype, Fitness) Solve<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem);
// }
//
// public interface IAlgorithm<out TGenotype, in TEncoding, in TState> : IAlgorithm<TGenotype, TEncoding>
//   where TEncoding : IEncoding<TGenotype>
//   where TState : class
// {
//   (TPhenotype, Fitness) Solve<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
// }
//
// public interface IAlgorithm<out TGenotype, in TEncoding, in TState, out TResultState> : IAlgorithm<TGenotype, TEncoding, TState>
//   where TEncoding : IEncoding<TGenotype>
//   where TState : class
//   where TResultState : class
// {
//   TResultState Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
// }
//
//
// public interface IStreamableAlgorithm<out TGenotype, in TEncoding, in TState, out TResultState> : IAlgorithm<TGenotype, TEncoding, TState>
//   where TEncoding : IEncoding<TGenotype>
//   where TState: class
//   where TResultState : class, IContinuableResultState<TState>
// {
//   (TPhenotype, Fitness) SolveStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
//   IEnumerable<TResultState> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
// }



//
// public interface ISolver<TGenotype, TPhenotype, TEncoding>
//   where TEncoding : IEncoding<TGenotype>
// {
//   (TPhenotype, Fitness) Solve(IEvaluationContext<TGenotype, TPhenotype, TEncoding> evaluationContext);
// }

// public interface IInitiableAlgorithm<TGenotype/*, TPhenotype*/, TEncoding/*, out TResultState*/> 
//   : IAlgorithm<TGenotype, TEncoding>
//   where TEncoding : IEncoding<TGenotype> 
//   /*where TResultState : IResultState*/
// {
//   IResultState<TPhenotype> ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem);
// }
//
// public interface IInitiableAlgorithm<TGenotype/*, TPhenotype*/, TEncoding, in TInitializationState, out TResultState> : IAlgorithm<TGenotype, TEncoding> 
//   where TEncoding : IEncoding<TGenotype> 
//   where TInitializationState : IStartState
//   where TResultState : IResultState
// {
//   TResultState ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TInitializationState initializationState);
// }


// public interface IContinuableAlgorithm<TGenotype/*, TPhenotype*/, TEncoding, in TContinuationState, TResultState> 
//   : IInitiableAlgorithm<TGenotype/*, TPhenotype*/, TEncoding, TResultState>
//   where TEncoding : IEncoding<TGenotype>
//   where TContinuationState : IContinuationState 
//   where TResultState : IContinuableResultState<TContinuationState>
// {
//   ITerminator<TResultState> Terminator { get; }
//   TResultState ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TContinuationState continuationState);
// }




//
// public static class AlgorithmExtensions {
//   public static IEnumerable<TResultState> CreateResultStream<TAlgorithm, TGenotype, TPhenotype, TEncoding, TContinuationState, TResultState>
//     (this TAlgorithm algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem)
//     where TAlgorithm : IInitiableAlgorithm<TGenotype, /*TPhenotype,*/ TEncoding, TResultState>, IContinuableAlgorithm<TGenotype, /*TPhenotype,*/ TEncoding, TContinuationState, TResultState>
//     where TEncoding: IEncoding<TGenotype>
//     where TContinuationState : IContinuationState
//     where TResultState : IContinuableResultState<TContinuationState>
//   {
//     var currentResult = algorithm.ExecuteInitialization(problem);
//     yield return currentResult;
//     var continuationState = currentResult.GetNextContinuationState();
//     var resultStream = algorithm.CreateResultStream<TAlgorithm, TGenotype, TPhenotype, TEncoding, TContinuationState, TResultState>(problem, continuationState);
//     foreach (var result in resultStream) {
//       yield return result;
//     }
//   }
//   
//   public static IEnumerable<TResultState> CreateResultStream<TAlgorithm, TGenotype, TPhenotype, TEncoding, TInitializationState, TContinuationState, TResultState>
//     (this TAlgorithm algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TInitializationState initializationState)
//     where TAlgorithm : IInitiableAlgorithm<TGenotype, /*TPhenotype, */TEncoding, TInitializationState, TResultState>, IContinuableAlgorithm<TGenotype,/* TPhenotype,*/ TEncoding, TContinuationState, TResultState>
//     where TEncoding: IEncoding<TGenotype>
//     where TInitializationState : IStartState
//     where TContinuationState : IContinuationState
//     where TResultState : IContinuableResultState<TContinuationState>
//   {
//     var currentResult = algorithm.ExecuteInitialization(problem, initializationState);
//     yield return currentResult;
//     while (algorithm.Terminator.ShouldContinue(currentResult)) {
//       var continuationState = currentResult.GetNextContinuationState();
//       currentResult = algorithm.ExecuteIteration(problem, continuationState);
//       yield return currentResult;
//     }
//   }
//   
//   public static IEnumerable<TResultState> CreateResultStream<TAlgorithm, TGenotype, TPhenotype, TEncoding, TContinuationState, TResultState>
//     (this TAlgorithm algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TContinuationState continuationState)
//     where TAlgorithm : IContinuableAlgorithm<TGenotype, /*TPhenotype,*/ TEncoding, TContinuationState, TResultState>
//     where TEncoding: IEncoding<TGenotype>
//     where TContinuationState : IContinuationState 
//     where TResultState : IContinuableResultState<TContinuationState>
//   {
//     TResultState currentResult;
//     var currentContinuationState = continuationState;
//     do {
//       currentResult = algorithm.ExecuteIteration(problem, currentContinuationState);
//       yield return currentResult;
//       currentContinuationState = currentResult.GetNextContinuationState();
//     } while (algorithm.Terminator.ShouldContinue(currentResult));
//   }
//   
//   
//   public static TResultState Execute<TAlgorithm, TGenotype, TPhenotype, TEncoding, TContinuationState, TResultState>
//     (this TAlgorithm algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem)
//     where TAlgorithm : IContinuableAlgorithm<TGenotype, /*TPhenotype,*/ TEncoding, TContinuationState, TResultState>, IInitiableAlgorithm<TGenotype, /*TPhenotype,*/ TEncoding, TResultState>
//     where TEncoding : IEncoding<TGenotype>
//     where TContinuationState : IContinuationState 
//     where TResultState : IContinuableResultState<TContinuationState> 
//   {
//     return algorithm.CreateResultStream<TAlgorithm, TGenotype, TPhenotype, TEncoding, TContinuationState, TResultState>(problem).Last();
//   }
//   
//   public static TResultState Execute<TAlgorithm, TGenotype, TPhenotype, TEncoding, TInitializationState, TContinuationState, TResultState>
//     (this TAlgorithm algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TInitializationState initializationState)
//     where TAlgorithm : IContinuableAlgorithm<TGenotype,/* TPhenotype,*/ TEncoding, TContinuationState, TResultState>, IInitiableAlgorithm<TGenotype, /*TPhenotype,*/ TEncoding, TInitializationState, TResultState>
//     where TEncoding : IEncoding<TGenotype>
//     where TInitializationState : IStartState 
//     where TContinuationState : IContinuationState
//     where TResultState : IContinuableResultState<TContinuationState> 
//   {
//     return algorithm.CreateResultStream<TAlgorithm, TGenotype, TPhenotype, TEncoding, TInitializationState, TContinuationState, TResultState>(problem, initializationState).Last();
//   }
//   
//   public static TResultState Execute<TAlgorithm, TGenotype, TPhenotype, TEncoding, TContinuationState, TResultState>
//     (this TAlgorithm algorithm, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TContinuationState continuationState)
//     where TAlgorithm : IContinuableAlgorithm<TGenotype,/* TPhenotype,*/ TEncoding, TContinuationState, TResultState>
//     where TEncoding : IEncoding<TGenotype>
//     where TContinuationState : IContinuationState where TResultState : IContinuableResultState<TContinuationState>
//   {
//     return algorithm.CreateResultStream<TAlgorithm, TGenotype, TPhenotype, TEncoding, TContinuationState, TResultState>(problem, continuationState).Last();
//   }
// }

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

  //public abstract EvaluatedIndividual<TGenotype, TPhenotype> Solve<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);

  
  
  // public virtual EvaluatedIndividual<TGenotype, TPhenotype> Solve<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
  //   var lastState = Execute(problem, initialState);
  //   return GetBestSolution(lastState, problem);
  // }
  //
  // public virtual IEnumerable<EvaluatedIndividual<TGenotype>> SolveStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
  //   foreach (var state in ExecuteStreaming(problem, initialState)) {
  //     yield return GetBestSolution(state, problem);
  //   }
  // }
  
  // public virtual TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
  //   return ExecuteStreaming(problem, initialState).Aggregate(AggregateIterationResults);
  // }

  //public abstract TAlgorithmResult Execute<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null);
  
  public virtual IEnumerable<TIterationResult> ExecuteStreaming<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState? initialState = null) {
    var currentIterationResult = initialState is null
      ? ExecuteInitialization(problem) 
      : ExecuteIteration(problem, initialState);
    yield return currentIterationResult;

    while (Terminator.ShouldContinue(currentIterationResult)) {
      var currentState = currentIterationResult.GetNextState();
      currentIterationResult = ExecuteIteration(problem, currentState);
      yield return currentIterationResult;
    }
  }
  
  protected abstract TIterationResult ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem);
  
  protected abstract TIterationResult ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TState state);

  // protected abstract TAlgorithmResult EmptyAlgorithmResult();
  // protected abstract TAlgorithmResult AggregateIterationResults(TAlgorithmResult algorithmResult, TIterationResult currentIterationResult);

  //public abstract EvaluatedIndividual<TGenotype, TPhenotype> GetBestSolution<TPhenotype>(TResult result, IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem);
}

//
// public abstract record class Algorithm<TGenotype, /*TPhenotype,*/ TEncoding, TResultState> 
//   : IInitiableAlgorithm<TGenotype, /*TPhenotype,*/ TEncoding, TResultState>
//   where TEncoding : IEncoding<TGenotype>
//   where TResultState : IResultState
// {
//   public abstract TResultState ExecuteInitialization<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem);
// }
//
// public abstract record class ContinuableAlgorithm<TGenotype, /*TPhenotype, */TEncoding, TContinuationState, TResultState> 
//   : Algorithm<TGenotype, /*TPhenotype, */TEncoding, TResultState>, 
//     IContinuableAlgorithm<TGenotype, /*TPhenotype,*/ TEncoding, TContinuationState, TResultState>
//   where TEncoding: IEncoding<TGenotype>
//   where TContinuationState : IContinuationState 
//   where TResultState : IContinuableResultState<TContinuationState>
// {
//   public required ITerminator<TResultState> Terminator { get; init; }
//   
//   public abstract TResultState ExecuteIteration<TPhenotype>(IEncodedProblem<TPhenotype, TGenotype, TEncoding> problem, TContinuationState continuationState);
// }


//
// public static class ExecutionStreamExtensions {
//   public static Solution<TGenotype>? GetBest<TGenotype>(this IEnumerable<PopulationState<TGenotype>> stream, IComparer<Fitness> comparer) {
//     return stream
//       .SelectMany(state => state.Population)
//       .MinBy(individual => individual.Fitness, comparer);
//   }
//   public static Solution<TGenotype>? GetBestSingleObjective<TGenotype>(this IEnumerable<PopulationState<TGenotype>> stream) {
//     var states = stream.ToList();
//     var objective = states.Select(state => state.Objective).First().WithSingleObjective();
//     return states.GetBest(objective.TotalOrderComparer);
//   }
//
//
//   public static IEnumerable<(Fitness best, Fitness median, Fitness worst)> GetObjectiveStatistics<TGenotype>(this IEnumerable<PopulationState<TGenotype>> stream, IComparer<Fitness> comparer) {
//     return stream.Select(state => {
//       if (state.Population.Length == 0) throw new InvalidOperationException("Population must not be empty.");
//       var fitnessValues = state.Population.Select(p => p.Fitness);
//       var orderedFitness = fitnessValues.OrderBy(x => x, comparer).ToArray();
//       Fitness best = orderedFitness[0];
//       Fitness worst = orderedFitness[^1];
//       // ToDo: average only works for single objective
//       Fitness median = orderedFitness[orderedFitness.Length / 2];
//       return (best, median, worst);
//     });
//   }
//   public static IEnumerable<(SingleFitness best, SingleFitness mean, SingleFitness worst)> GetSingleObjectiveStatisticsStream<TGenotype>(this ResultStream<PopulationState<TGenotype>> stream) {
//     return stream.Select(state => {
//       if (state.Population.Length == 0) throw new InvalidOperationException("Population must not be empty.");
//       var objective = state.Objective.WithSingleObjective();
//       var comparer = objective.TotalOrderComparer;
//       var fitnessValues = state.Population.Select(p => p.Fitness);
//       var orderedFitness = fitnessValues.OrderBy(x => x, comparer).ToArray();
//       Fitness best = orderedFitness[0];
//       Fitness worst = orderedFitness[^1];
//       double mean = orderedFitness.Select(x => x.SingleFitness!.Value.Value).Average();
//       return (best.SingleFitness!.Value, new SingleFitness(mean), worst.SingleFitness!.Value);
//     });
//   }
// }
