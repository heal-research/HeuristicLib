using HEAL.HeuristicLib.OperatorPrototypes;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

public interface IAlgorithmAttachment<in T, in TS, in TP, in TRes>
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TRes : IIterationResult {
  public void AttachTo<T1, TRes1, TS1, TP1>(IAlgorithmBuilder<T1, TS1, TP1, TRes1> proto) where T1 : class, T
    where TRes1 : IIterationResult, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP;
}
