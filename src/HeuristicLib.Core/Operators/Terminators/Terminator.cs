using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

// Move Terminators out of Operators?

public abstract class Terminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> 
  : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class TerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> 
  : ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
 where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public abstract bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem);
}


public abstract class Terminator<TGenotype, TAlgorithmState, TSearchSpace> 
  : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class TerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace> 
  : ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public abstract bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace);
  
  bool ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => 
    ShouldTerminate(state, searchSpace);
}


public abstract class Terminator<TGenotype, TAlgorithmState> 
  : ITerminator<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class TerminatorInstance<TGenotype, TAlgorithmState>
  : ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TAlgorithmState : class, IAlgorithmState
{
  public abstract bool ShouldTerminate(TAlgorithmState state);

  bool ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(TAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => 
    ShouldTerminate(state);
}

public abstract class Terminator<TGenotype> 
  : ITerminator<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry);
}

public abstract class TerminatorInstance<TGenotype>
  : ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public abstract bool ShouldTerminate();

  bool ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(IAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => 
    ShouldTerminate();
}


public abstract class StatelessTerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem> 
  : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, TProblem>,
    ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
{
  public ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, TProblem> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, TProblem problem);
}

public abstract class StatelessTerminator<TGenotype, TAlgorithmState, TSearchSpace> 
  : ITerminator<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>,
    ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>
  where TAlgorithmState : class, IAlgorithmState
  where TSearchSpace : class, ISearchSpace<TGenotype>
{
  public ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract bool ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace);
  
  bool ITerminatorInstance<TGenotype, TAlgorithmState, TSearchSpace, IProblem<TGenotype, TSearchSpace>>.ShouldTerminate(TAlgorithmState state, TSearchSpace searchSpace, IProblem<TGenotype, TSearchSpace> problem) => 
    ShouldTerminate(state, searchSpace);
}

public abstract class StatelessTerminator<TGenotype, TAlgorithmState> 
  : ITerminator<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>,
    ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
  where TAlgorithmState : class, IAlgorithmState
{
  public ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract bool ShouldTerminate(TAlgorithmState state);

  bool ITerminatorInstance<TGenotype, TAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(TAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => 
    ShouldTerminate(state);
}

public abstract class StatelessTerminator<TGenotype> 
  : ITerminator<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>,
    ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) => this;
  
  public abstract bool ShouldTerminate();

  bool ITerminatorInstance<TGenotype, IAlgorithmState, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>.ShouldTerminate(IAlgorithmState state, ISearchSpace<TGenotype> searchSpace, IProblem<TGenotype, ISearchSpace<TGenotype>> problem) => 
    ShouldTerminate();
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
