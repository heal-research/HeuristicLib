using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Terminator;

public static class TerminationExtensions {
  extension<TG, TE, TP, TR>(IAlgorithmBuilder<TG, TE, TP, TR> builder)
    where TG : class
    where TE : class, IEncoding<TG>
    where TP : class, IProblem<TG, TE>
    where TR : class, IIterationResult {
    public void SetMaxEvaluations(int maxEvaluations, bool preventOverBudget = false) {
      builder.AddAttachment(new AfterEvaluationsTermination<TG>(maxEvaluations, preventOverBudget));
    }

    public void SetMaxIterations(int maxIteration) {
      builder.Terminator = new AnyTerminator<TG, TR, TE, TP>(builder.Terminator, new AfterIterationsTerminator<TG>(maxIteration));
    }
  }

  extension<TG, TE, TP, TR>(IAlgorithmBuilder<TG, TE, TP, TR> builder)
    where TG : class
    where TE : class, IEncoding<TG>
    where TP : class, IProblem<TG, TE>
    where TR : PopulationIterationResult<TG> {
    public void SetTargetObjective(ObjectiveVector target) {
      builder.Terminator = new AnyTerminator<TG, TR, TE, TP>(builder.Terminator, new TargetTerminator<TG>(target));
    }
  }
}
