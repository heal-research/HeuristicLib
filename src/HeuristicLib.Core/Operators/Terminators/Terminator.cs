using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

// Move Terminators out of Operators?

public abstract class Terminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract Func<TAlgorithmState, bool> CreateShouldTerminatePredicate(TSearchSpace searchSpace, TProblem problem);
}

public abstract class Terminator<TGenotype, TAlgorithmState, TSearchSpace> : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TAlgorithmState : IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract Func<TAlgorithmState, bool> CreateShouldTerminatePredicate(TSearchSpace searchSpace);

  Func<TAlgorithmState, bool> ITerminator<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.CreateShouldTerminatePredicate(TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => CreateShouldTerminatePredicate(searchSpace);
}

public abstract class Terminator<TGenotype, TAlgorithmState> : ITerminator<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TAlgorithmState : IAlgorithmState
{
  public abstract Func<TAlgorithmState, bool> CreateShouldTerminatePredicate();

  Func<TAlgorithmState, bool> ITerminator<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.CreateShouldTerminatePredicate(ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => CreateShouldTerminatePredicate();
}

public abstract class Terminator<TGenotype> : ITerminator<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract Func<bool> CreateShouldTerminatePredicate();


  public Func<IAlgorithmState, bool> CreateShouldTerminatePredicate(ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    var predicate = CreateShouldTerminatePredicate();
    return _ => predicate();
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

// public class StagnatedFitnessTerminator : IterationStateBasedTerminator<IAlgorithmState> {
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
//   public override bool ShouldTerminate(IAlgorithmState iterationResult) {
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
