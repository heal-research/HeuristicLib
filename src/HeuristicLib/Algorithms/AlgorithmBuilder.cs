using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record AlgorithmBuilder<TG, TS, TP, TR, TAlg> : IAlgorithmBuilder<TG, TS, TP, TR, TAlg>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
  where TAlg : IAlgorithm<TG, TS, TP, TR> 
{
  public List<IAlgorithmBuilderRewriter<IAlgorithmBuilder<TG, TS, TP, TR, TAlg>, TG, TS, TP, TR, TAlg>> Rewriters { get; } = [];
  
  public IEvaluator<TG, TS, TP> Evaluator { get; set; } = new DirectEvaluator<TG>();
  
  public ITerminator<TG, TR, TS, TP> Terminator { get; set; } = new AfterIterationsTerminator<TG>(100);
  
  public IInterceptor<TG, TR, TS, TP>? Interceptor { get; set; }

  public abstract TAlg Build();

  public void AddRewriter<TRewriter>(TRewriter rewriter) 
    where TRewriter : IAlgorithmBuilderRewriter<IAlgorithmBuilder<TG, TS, TP, TR, TAlg>, TG, TS, TP, TR, TAlg>
  {
    Rewriters.Add(rewriter);
  }
  
  protected void ApplyRewriters() {
    // this needs to be applied to a clone to avoid modifying the current builder during build
    foreach (var rewriter in Rewriters) {
      rewriter.Rewrite(this);
    }
  }
}
