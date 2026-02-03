using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Prototypes;

public interface IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TRes : IAlgorithmState
{
  ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; set; }
  ITerminator<TGenotype, TRes, TSearchSpace, TProblem> Terminator { get; set; }
  IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; set; }
  IInterceptor<TGenotype, TRes, TSearchSpace, TProblem>? Interceptor { get; set; }

  IAlgorithm<TGenotype, TSearchSpace, TProblem, TRes> BuildAlgorithm();
}

public interface IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes, out TAlg> : IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TRes : IAlgorithmState
  where TAlg : IAlgorithm<TGenotype, TSearchSpace, TProblem, TRes>
{
  new TAlg BuildAlgorithm();
}
