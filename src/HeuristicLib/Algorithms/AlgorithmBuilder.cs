using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record AlgorithmBuilder<TG, TS, TP, TR, TAlg, TBuildSpec> : IAlgorithmBuilder<TG, TS, TP, TR, TAlg>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
  where TAlg : IAlgorithm<TG, TS, TP, TR>
  where TBuildSpec : IBuildSpec
{
  public List<IAlgorithmBuilderRewriter<TBuildSpec>> Rewriters { get; } = [];
  
  public IEvaluator<TG, TS, TP> Evaluator { get; set; } = new DirectEvaluator<TG>();
  
  public ITerminator<TG, TR, TS, TP> Terminator { get; set; } = new AfterIterationsTerminator<TG>(100);
  
  public IInterceptor<TG, TR, TS, TP>? Interceptor { get; set; }
  
  public void AddRewriter<TRewriter>(TRewriter rewriter) 
    where TRewriter : IAlgorithmBuilderRewriter<TBuildSpec>
  {
    Rewriters.Add(rewriter);
  }

  public TAlg Build() {
    var spec = CreateBuildSpec();
    ApplyRewriters(spec);
    return BuildFromSpec(spec);
  }

  protected abstract TBuildSpec CreateBuildSpec();
  protected abstract TAlg BuildFromSpec(TBuildSpec buildSpec);
  
  private void ApplyRewriters(TBuildSpec buildSpec) {
    foreach (var rewriter in Rewriters) {
      rewriter.Rewrite(buildSpec);
    }
  }
}
