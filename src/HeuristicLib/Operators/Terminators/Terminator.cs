using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract class Terminator<TGenotype, TIterationResult, TSearchSpace, TProblem> : ITerminator<TGenotype, TIterationResult, TSearchSpace, TProblem>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace searchSpace, TProblem problem);
}

public abstract class Terminator<TGenotype, TIterationResult, TSearchSpace> : ITerminator<TGenotype, TIterationResult, TSearchSpace>
  where TIterationResult : IIterationResult
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace searchSpace);

  bool ITerminator<TGenotype, TIterationResult, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return ShouldTerminate(currentIterationState, previousIterationState, searchSpace);
  }
}

public abstract class Terminator<TGenotype, TIterationResult> : ITerminator<TGenotype, TIterationResult>
  where TIterationResult : IIterationResult {
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState);

  bool ITerminator<TGenotype, TIterationResult, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return ShouldTerminate(currentIterationState, previousIterationState);
  }
}

public abstract class Terminator<TGenotype> : ITerminator<TGenotype> {
  public abstract bool ShouldTerminate();

  bool ITerminator<TGenotype, IIterationResult, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(IIterationResult currentIterationState, IIterationResult? previousIterationState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return ShouldTerminate();
  }
}

// public class OnMaximumIterationTerminator : AlgorithmStateBasedTerminator<IIterativeAlgorithm>
// {
//   public int MaximumGeneration { get; set; }
//   public OnMaximumIterationTerminator(int maximumGeneration) {
//     MaximumGeneration = maximumGeneration;
//   }
//   public override bool ShouldTerminate(IIterativeAlgorithm iterationResult) {
//     return iterationResult.CurrentIteration >= MaximumGeneration;
//   }
// }

// public  class MaximumExecutionTimeTerminator : AlgorithmStateBasedTerminator<IIterativeAlgorithm>
// {
//   public TimeSpan MaximumExecutionTime { get; set; }
//   public MaximumExecutionTimeTerminator(TimeSpan maximumExecutionTime) {
//     MaximumExecutionTime = maximumExecutionTime;
//   }
//   
//   public override bool ShouldTerminate(IIterativeAlgorithm algorithm) {
//     return algorithm.TotalElapsedTime >= MaximumExecutionTime;
//   }
// }

// public class StagnatedFitnessTerminator : IterationStateBasedTerminator<IIterationResult> {
//   public int NumberOfIterations { get; set; }
//   public double MinimumImprovement { get; set; }
//   
//   public StagnatedFitnessTerminator(int numberOfIterations, double minimumImprovement = double.Epsilon) {
//     NumberOfIterations = numberOfIterations;
//     MinimumImprovement = minimumImprovement;
//     
//     FitnessHistory = new List<double>(numberOfIterations);
//   }
//   
//   public List<double> FitnessHistory { get; }
//   
//   
//   public override bool ShouldTerminate(IIterationResult iterationResult) {
//     // drop old values
//     if (FitnessHistory.Count >= NumberOfIterations) {
//       FitnessHistory.RemoveAt(0);
//     }
//     // add new value
//     FitnessHistory.Add(iterationResult.BestFitness);
//     // check if ramp-up is complete
//     if (FitnessHistory.Count < NumberOfIterations) {
//       return false; // not enough data to determine stagnation
//     }
//     // check if stagnation occurred: if there was progress in "any" of the last N iterations
//     for (int i = 1; i < NumberOfIterations; i++) {
//       if (Math.Abs(FitnessHistory[i] - FitnessHistory[i - 1]) > MinimumImprovement) {
//         return false; // progress was made
//       }
//     }
//     // if we reach here, it means no progress was made in the last N iterations
//     return true; // stagnation detected
//   }
// }
