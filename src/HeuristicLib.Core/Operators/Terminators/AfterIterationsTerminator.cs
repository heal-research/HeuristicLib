using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Terminators;

public class AfterIterationsTerminator<TGenotype>(int maximumIterations)
  : Terminator<TGenotype, IAlgorithmState>
{
  public int MaximumIterations { get; } = maximumIterations;

  public override bool ShouldTerminate(IAlgorithmState currentIterationState, IAlgorithmState? previousIterationState) => currentIterationState.CurrentIteration >= MaximumIterations;
}

public class TargetTerminator<TGenotype>(ObjectiveVector target)
  : Terminator<TGenotype, PopulationIterationState<TGenotype>, ISearchSpace<TGenotype>, IProblem<TGenotype, ISearchSpace<TGenotype>>>
{
  public override bool ShouldTerminate(PopulationIterationState<TGenotype> currentIterationState,
    PopulationIterationState<TGenotype>? previousIterationState,
    ISearchSpace<TGenotype> searchSpace,
    IProblem<TGenotype, ISearchSpace<TGenotype>> problem)
  {
    return currentIterationState.Population.Any(x => !target.Dominates(x.ObjectiveVector, problem.Objective));
  }
}

// public static class TerminationExtensions {
//   extension<TG, TS, TP, TR, TA>(AlgorithmBuilder<TG, TS, TP, TR, TA> builder)
//     where TG : class
//     where TS : class, ISearchSpace<TG>
//     where TP : class, IProblem<TG, TS>
//     where TR : class, IAlgorithmState 
//     where TA : Algorithm<TG, TS, TP, TR>
//   {
//     public void SetMaxEvaluations(int maxEvaluations, bool preventOverBudget = false) {
//       builder.AddAttachment(new AfterEvaluationsTermination<TG>(maxEvaluations, preventOverBudget));
//     }
//
//     public void SetMaxIterations(int maxIteration) {
//       builder.Terminator = new AnyTerminator<TG, TR, TS, TP>(builder.Terminator, new AfterIterationsTerminator<TG>(maxIteration));
//     }
//   }
//
//   extension<TG, TS, TP, TR, TA>(AlgorithmBuilder<TG, TS, TP, TR, TA> builder)
//     where TG : class
//     where TS : class, ISearchSpace<TG>
//     where TP : class, IProblem<TG, TS>
//     where TR : PopulationIterationState<TG>
//     where TA : IAlgorithm<TG, TS, TP, TR>
//   {
//     public void SetTargetObjective(ObjectiveVector target) {
//       builder.Terminator = new AnyTerminator<TG, TR, TS, TP>(builder.Terminator, new TargetTerminator<TG>(target));
//     }
//   }
// }
