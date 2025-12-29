using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract class Terminator<TGenotype, TIterationState, TSearchSpace, TProblem> : ITerminator<TGenotype, TIterationState, TSearchSpace, TProblem>
  where TIterationState : IIterationState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace> {
  public abstract bool ShouldTerminate(TIterationState currentIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace, TProblem problem);
}

public abstract class Terminator<TGenotype, TIterationState, TSearchSpace> : ITerminator<TGenotype, TIterationState, TSearchSpace>
  where TIterationState : IIterationState
  where TSearchSpace : class, ISearchSpace<TGenotype> {
  public abstract bool ShouldTerminate(TIterationState currentIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace);

  bool ITerminator<TGenotype, TIterationState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.ShouldTerminate(TIterationState currentIterationState, TIterationState? previousIterationState, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) {
    return ShouldTerminate(currentIterationState, previousIterationState, searchSpace);
  }
}

public abstract class Terminator<TGenotype, TIterationState> : ITerminator<TGenotype, TIterationState>
  where TIterationState : IIterationState {
  public abstract bool ShouldTerminate(TIterationState currentIterationState, TIterationState? previousIterationState);

  bool ITerminator<TGenotype, TIterationState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(TIterationState currentIterationState, TIterationState? previousIterationState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
    return ShouldTerminate(currentIterationState, previousIterationState);
  }
}

public abstract class Terminator<TGenotype> : ITerminator<TGenotype> {
  public abstract bool ShouldTerminate();

  bool ITerminator<TGenotype, IIterationState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(IIterationState currentIterationState, IIterationState? previousIterationState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) {
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

// public class StagnatedFitnessTerminator : IterationStateBasedTerminator<IIterationState> {
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
//   public override bool ShouldTerminate(IIterationState iterationResult) {
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
