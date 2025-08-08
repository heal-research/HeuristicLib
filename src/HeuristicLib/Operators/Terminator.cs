using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators;

public interface ITerminator<TGenotype, in TIterationResult, in TEncoding, in TProblem>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, TProblem problem);
  bool ShouldContinue(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, TProblem problem) {
    return !ShouldTerminate(currentIterationState, previousIterationState, encoding, problem);
  }
}

public abstract class Terminator<TGenotype, TIterationResult, TEncoding, TProblem> : ITerminator<TGenotype, TIterationResult, TEncoding, TProblem>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, TProblem problem);
}

public abstract class Terminator<TGenotype, TIterationResult, TEncoding> : ITerminator<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : IEncoding<TGenotype>
{
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding);
  
  bool ITerminator<TGenotype, TIterationResult, TEncoding, IProblem<TGenotype, TEncoding>>.ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, IProblem<TGenotype, TEncoding> problem) {
    return ShouldTerminate(currentIterationState, previousIterationState, encoding);
  }
}

public abstract class Terminator<TGenotype, TIterationResult> : ITerminator<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
  where TIterationResult : IIterationResult<TGenotype>
{
  public abstract bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState);

  bool ITerminator<TGenotype, TIterationResult, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>.ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, IEncoding<TGenotype> encoding, IProblem<TGenotype, IEncoding<TGenotype>> problem) {
    return ShouldTerminate(currentIterationState, previousIterationState);
  }
}

public abstract class Terminator<TGenotype> : ITerminator<TGenotype, IIterationResult<TGenotype>, IEncoding<TGenotype>, IProblem<TGenotype, IEncoding<TGenotype>>>
{
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


public class AfterIterationsTerminator<TGenotype> : Terminator<TGenotype> 
{
  public int MaximumIterations { get; }
  
  public AfterIterationsTerminator(int maximumIterations) {
    MaximumIterations = maximumIterations;
    CurrentCount = 0;
  }
  
  public int CurrentCount { get; private set; }
  
  public override bool ShouldTerminate() {
    CurrentCount += 1;
    return CurrentCount >= MaximumIterations;
  }
}


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


public class PauseToken {
  public bool IsPaused { get; private set; }
  public void RequestPause() => IsPaused = true;
}

public class PauseTokenTerminator<TGenotype> : Terminator<TGenotype> {
  private readonly PauseToken pauseToken;
  public PauseTokenTerminator(PauseToken pauseToken) {
    this.pauseToken = pauseToken;
  }

  public override bool ShouldTerminate() {
    return pauseToken.IsPaused;
  }
}


public class NeverTerminator<TGenotype> : Terminator<TGenotype>
{
  public override bool ShouldTerminate() {
    return false;
  }
}

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


public class AnyTerminator<TGenotype, TIterationResult, TEncoding, TProblem> : Terminator<TGenotype, TIterationResult, TEncoding, TProblem>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public IReadOnlyList<ITerminator<TGenotype, TIterationResult, TEncoding, TProblem>> Terminators { get; }
  public AnyTerminator(IReadOnlyList<ITerminator<TGenotype, TIterationResult, TEncoding, TProblem>> terminators) {
    Terminators = terminators;
  }
  public override bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, TProblem problem) {
    return Terminators.Any(criterion => criterion.ShouldTerminate(currentIterationState, previousIterationState, encoding, problem));
  }
}


public class AllTerminator<TGenotype, TIterationResult, TEncoding, TProblem> : Terminator<TGenotype, TIterationResult, TEncoding, TProblem>
  where TIterationResult : IIterationResult<TGenotype>
  where TEncoding : IEncoding<TGenotype>
  where TProblem : IProblem<TGenotype, TEncoding>
{
  public IReadOnlyList<ITerminator<TGenotype, TIterationResult, TEncoding, TProblem>> Terminators { get; }
  public AllTerminator(IReadOnlyList<ITerminator<TGenotype, TIterationResult, TEncoding, TProblem>> terminators) {
    Terminators = terminators;
  }
  public override bool ShouldTerminate(TIterationResult currentIterationState, TIterationResult? previousIterationState, TEncoding encoding, TProblem problem) {
    return Terminators.All(criterion => criterion.ShouldTerminate(currentIterationState, previousIterationState, encoding, problem));
  }
}
