using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.OperatorPrototypes;

public interface IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TRes : IIterationResult {
  public ICreator<TGenotype, TSearchSpace, TProblem> Creator { get; set; }
  public ITerminator<TGenotype, TRes, TSearchSpace, TProblem> Terminator { get; set; }
  public IEvaluator<TGenotype, TSearchSpace, TProblem> Evaluator { get; set; }
  public IInterceptor<TGenotype, TRes, TSearchSpace, TProblem>? Interceptor { get; set; }

  public IAlgorithm<TGenotype, TSearchSpace, TProblem, TRes> BuildAlgorithm();
}

public interface IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes, out TAlg> : IAlgorithmBuilder<TGenotype, TSearchSpace, TProblem, TRes>
  where TSearchSpace : class, ISearchSpace<TGenotype>
  where TProblem : class, IProblem<TGenotype, TSearchSpace>
  where TRes : IIterationResult
  where TAlg : IAlgorithm<TGenotype, TSearchSpace, TProblem, TRes> {
  public new TAlg BuildAlgorithm();
}
