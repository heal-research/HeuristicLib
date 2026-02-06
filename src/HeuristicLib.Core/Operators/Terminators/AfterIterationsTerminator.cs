namespace HEAL.HeuristicLib.Operators.Terminators;

public class AfterIterationsTerminator<TGenotype>(int maximumIterations)
  : Terminator<TGenotype>
{
  public int MaximumIterations { get; } = maximumIterations;
  
  public override Func<bool> CreateShouldTerminatePredicate()
  {
    var currentCounter = 0;
    return () => currentCounter++ >= MaximumIterations;
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
