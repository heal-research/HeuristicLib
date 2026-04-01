using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record AlgorithmBuilder<TG, TS, TP, TR, TAlg>
  : IAlgorithmBuilder<TG, TS, TP, TR, TAlg>,
    IBuilderWithEvaluator<TG, TS, TP>, IBuilderWithInterceptor<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
  where TAlg : IAlgorithm<TG, TS, TP, TR>
{
  public IEvaluator<TG, TS, TP> Evaluator { get; set; } = new DirectEvaluator<TG>();

  public IInterceptor<TG, TS, TP, TR>? Interceptor { get; set; }

  public abstract TAlg Build();
}
