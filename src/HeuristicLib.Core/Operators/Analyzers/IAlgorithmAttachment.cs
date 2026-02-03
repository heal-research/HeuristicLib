using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Analyzers;

public interface IAlgorithmAttachment<in T, in TE, in TP, in TRes>
  where TE : class, ISearchSpace<T>
  where TP : class, IProblem<T, TE>
  where TRes : IAlgorithmState
{
  void AttachTo<T1, TRes1, TE1, TP1>(IAlgorithmBuilder<T1, TE1, TP1, TRes1> proto) where T1 : class, T
    where TRes1 : IAlgorithmState, TRes
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP;
}
