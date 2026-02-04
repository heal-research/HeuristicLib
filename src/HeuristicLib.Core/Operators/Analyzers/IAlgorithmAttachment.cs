//using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Analyzers;

public interface IAlgorithmAttachment<T, TE, TP, TRes, TAlg, TBuildSpec>
  where T : class
  where TE : class, ISearchSpace<T>
  where TP : class, IProblem<T, TE>
  where TRes : class, IAlgorithmState
  where TAlg : IAlgorithm<T, TE, TP, TRes>
  where TBuildSpec : IBuildSpec
{
  void AttachTo/*<T1, TRes1, TE1, TP1>*/(IAlgorithmBuilder<T, TE, TP, TRes, TAlg, TBuildSpec> proto) 
    /*where T1 : class, T
    where TRes1 : class, IAlgorithmState, TRes
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP*/
  ;
}
