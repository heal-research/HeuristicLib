using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public abstract class Terminator<TGenotype, TIterationResult, TSearchSpace, TProblem> : ITerminator<TGenotype, TIterationResult, TSearchSpace, TProblem>
  where TIterationResult : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace encoding, TProblem problem);
}

public abstract class Terminator<TGenotype, TIterationResult, TSearchSpace> : ITerminator<TGenotype, TIterationResult, TSearchSpace>
  where TIterationResult : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{

  bool ITerminator<TGenotype, TIterationResult, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace encoding, IProblem<TGenotype, TSearchSpace> problem) => ShouldTerminate(currentIterationState, previousIterationState, encoding);
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TSearchSpace encoding);
}

public abstract class Terminator<TGenotype, TIterationResult> : ITerminator<TGenotype, TIterationResult>
  where TIterationResult : IAlgorithmState
{

  bool ITerminator<TGenotype, TIterationResult, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => ShouldTerminate(currentIterationState, previousIterationState);
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState);
}

public abstract class Terminator<TGenotype> : ITerminator<TGenotype>
{

  bool ITerminator<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(IAlgorithmState currentIterationState, IAlgorithmState? previousIterationState, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => ShouldTerminate();
  public abstract bool ShouldTerminate();
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
