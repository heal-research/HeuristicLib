using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithmBuilder {
  
}

public interface IAlgorithmBuilder<TG, TS, TP, TR, out TA> : IAlgorithmBuilder
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
  where TG : class
{
  TA Build();
}

public interface IAlgorithmBuilderRewriter<in TBuilder>
  where TBuilder : IAlgorithmBuilder 
{
  void Rewrite(TBuilder builder);
}

public interface IAlgorithmBuilderRewriter<in TBuilder, TG, TS, TP, TR, out TA> : IAlgorithmBuilderRewriter<TBuilder>
  where TG : class
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TBuilder : IAlgorithmBuilder<TG, TS, TP, TR, TA>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  
}



public interface IBuilderWithCrossover<TG, TS, TP, TR, out TA> : IAlgorithmBuilder<TG, TS, TP, TR, TA> 
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
  where TG : class
{
  ICrossover<TG, TS, TP> Crossover { get; set; }
} 

public interface IBuilderWithEvaluator<TG, TS, TP, TR, out TA> : IAlgorithmBuilder<TG, TS, TP, TR, TA> 
  where TA : IAlgorithm<TG, TS, TP, TR>
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
  where TG : class
{
  IEvaluator<TG, TS, TP> Evaluator { get; set; }
}

