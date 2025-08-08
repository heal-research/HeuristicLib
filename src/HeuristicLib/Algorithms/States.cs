using System.Collections;
using System.Diagnostics;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Algorithms;

//
// public interface IIterationResult {
//   double BestFitness { get; }
//   ObjectiveDirection ObjectiveDirection { get; }
// }
//
// public interface IIterationResult<TGenotype, out TSolutionLayout>
//   : IIterationResult
//   where TGenotype : IEquatable<TGenotype>
//   where TSolutionLayout : ISolutionLayout<TGenotype> 
//
// {
//   TSolutionLayout Solutions { get; }
//   Solution<TGenotype> BestSolution { get; }
// }
//
// public abstract record class IterationResult : IIterationResult {
//   public double BestFitness { get; protected set; }
//   public ObjectiveDirection ObjectiveDirection { get; }
//   
//   public IterationResult(double bestFitness, ObjectiveDirection objectiveDirection) {
//     BestFitness = bestFitness;
//     ObjectiveDirection = objectiveDirection;
//   }
// }
//
// public abstract record class IterationResult<TGenotype, TPopulationLayout> : IterationResult, IIterationResult<TGenotype, TPopulationLayout> 
//   where TGenotype : IEquatable<TGenotype>
//   where TPopulationLayout : ISolutionLayout<TGenotype>  
// {
//   public TPopulationLayout Solutions { get; init; }
//   public Solution<TGenotype> BestSolution { get; }
//
//   public IterationResult(TPopulationLayout solutions, ObjectiveDirection objectiveDirection) 
//     : base(double.NaN, objectiveDirection)  {
//     Solutions = solutions;
//     
//     
//     BestSolution = Solutions
//       .OrderBy(s => s.ObjectiveVector[0], objectiveDirection == ObjectiveDirection.Minimize ? Comparer<double>.Default : Comparer<double>.Create((x, y) => y.CompareTo(x)))
//       .First();
//     BestFitness = BestSolution.ObjectiveVector[0];
//   }
// }
//
//
// public record class SingleSolutionIterationResult<TGenotype> : IterationResult<TGenotype, SingleSolution<TGenotype>> 
//   where TGenotype : IEquatable<TGenotype> 
// {
//   public Solution<TGenotype> Solution => Solutions.Solution;
//
//   public SingleSolutionIterationResult(Solution<TGenotype> solution, ObjectiveDirection objectiveDirection)
//     : base(new SingleSolution<TGenotype>(solution), objectiveDirection) { }
//   public SingleSolutionIterationResult(SingleSolution<TGenotype> solution, ObjectiveDirection objectiveDirection)
//     : base(solution, objectiveDirection) { }
// }
//
// public record class PopulationIterationResult<TGenotype> : IterationResult<TGenotype, Population<TGenotype>>
//   where TGenotype : IEquatable<TGenotype> 
// {
//   public PopulationIterationResult(ImmutableList<Solution<TGenotype>> solutions, ObjectiveDirection objectiveDirection) 
//     : base(new Population<TGenotype>(solutions), objectiveDirection) { }
//   public PopulationIterationResult(Population<TGenotype> solutions, ObjectiveDirection objectiveDirection)
//     : base(solutions, objectiveDirection) { }
// }

// public interface IAlgorithmState {
//   
// }
//
// public interface IAlgorithmResult {
//   int CurrentIteration { get; }
//   int TotalIterations { get; }
//   
//   TimeSpan CurrentDuration { get; }
//   TimeSpan TotalDuration { get; }
// }

// public interface ISingleObjectiveAlgorithmResult<TGenotype> : IAlgorithmResult {
//   Solution<TGenotype>? CurrentBestSolution { get; }
//   Solution<TGenotype>? BestSolution { get; }
// }
//
// public interface IMultiObjectiveAlgorithmResult<TGenotype> : IAlgorithmResult {
//   IReadOnlyList<Solution<TGenotype>> CurrentParetoFront { get; }
//   IReadOnlyList<Solution<TGenotype>> ParetoFront { get; }
// }

// public interface IIterationResult {
//   int Iteration { get; }
//   TimeSpan TotalDuration { get; }
// }

// public interface ISingleObjectiveIterationResult<TGenotype> : IIterationResult {
//   EvaluatedIndividual<TGenotype> BestSolution { get; }
// }
//
// public interface IMultiObjectiveIterationResult<TGenotype> : IIterationResult {
//   IReadOnlyList<EvaluatedIndividual<TGenotype>> ParetoFront { get; }
// }

// public interface IContinuableAlgorithmResult<out TState> : IAlgorithmResult {
//   TState GetContinuationState();
//   TState GetRestartState();
// }

// public interface IContinuableIterationResult<out TState> : IIterationResult {
//   TState GetContinuationState();
//   TState GetRestartState();
// }

