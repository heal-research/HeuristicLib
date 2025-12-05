using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Operators.Analyzer;

public interface IAlgorithmAttachment<in T, in TE, in TP, in TRes>
  where TE : class, IEncoding<T>
  where TP : class, IProblem<T, TE>
  where TRes : IIterationResult {
  public void AttachTo<T1, TRes1, TE1, TP1>(IAlgorithmBuilder<T1, TE1, TP1, TRes1> proto) where T1 : class, T
    where TRes1 : IIterationResult, TRes
    where TE1 : class, IEncoding<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP;
}
