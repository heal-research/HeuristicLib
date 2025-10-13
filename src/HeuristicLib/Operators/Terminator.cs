using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public abstract class Terminator<TGenotype, TIterationResult, TEncoding, TProblem> : ITerminator<TGenotype, TIterationResult, TEncoding, TProblem>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : class, IEncoding<TGenotype>
  where TProblem : class, IProblem<TGenotype, TEncoding> {
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, TProblem problem);
}

public abstract class Terminator<TGenotype, TIterationResult, TEncoding> : ITerminator<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : class, IEncoding<TGenotype> {
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding);

  bool ITerminator<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>.ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return ShouldTerminate(currentIterationState, previousIterationState, encoding);
  }
}

public abstract class Terminator<TGenotype, TIterationResult> : ITerminator<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
  where TIterationResult : IIterationResult<TGenotype> {
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState);

  bool ITerminator<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return ShouldTerminate(currentIterationState, previousIterationState);
  }
}

public abstract class Terminator<TGenotype> : ITerminator<TGenotype, IIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>> {
  public abstract bool ShouldTerminate();

  bool ITerminator<TGenotype, IIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.ShouldTerminate(IIterationResult<TGenotype> currentIterationState, IIterationResult<TGenotype>? previousIterationState, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
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
