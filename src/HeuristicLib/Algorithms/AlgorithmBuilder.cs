using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record AlgorithmBuilder<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>
  : IAlgorithmBuilder<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>,
    IBuilderWithEvaluator<TCandidate, TSearchSpace, TProblem>, IBuilderWithInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>
  where TSearchSpace : class, ISearchSpace<TCandidate>
  where TProblem : class, IProblem<TCandidate, TSearchSpace>
  where TSearchState : class, ISearchState
  where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    public IEvaluator<TCandidate, TSearchSpace, TProblem> Evaluator { get; set; } = new DirectEvaluator<TCandidate>();

    public IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>? Interceptor { get; set; }

    public abstract TAlgorithm Build();
}
