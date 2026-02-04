using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithmBuilder
{
}

public interface IAlgorithmBuilder<TG, TS, TP, TR, out TA, TBuildSpec> : IAlgorithmBuilder
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TBuildSpec : IBuildSpec
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
  where TG : class
{
  // Actually i dont want this here only on implementation
  public void AddRewriter<TRewriter>(TRewriter rewriter) where TRewriter : IAlgorithmBuilderRewriter<TBuildSpec>;
  
  TA Build();
}
