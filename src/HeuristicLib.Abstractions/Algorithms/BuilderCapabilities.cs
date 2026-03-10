using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public interface IBuilderWithEvaluator<TG, TS, TP> : IAlgorithmBuilder
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  IEvaluator<TG, TS, TP> Evaluator { get; set; }
}

public interface IBuilderWithCreator<TG, TS, TP> : IAlgorithmBuilder
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  ICreator<TG, TS, TP> Creator { get; set; }
}

public interface IBuilderWithSelector<TG, TS, TP> : IAlgorithmBuilder
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  ISelector<TG, TS, TP> Selector { get; set; }
}

public interface IBuilderWithCrossover<TG, TS, TP> : IAlgorithmBuilder
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  ICrossover<TG, TS, TP> Crossover { get; set; }
}

public interface IBuilderWithMutator<TG, TS, TP> : IAlgorithmBuilder
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  IMutator<TG, TS, TP> Mutator { get; set; }
}

public interface IBuilderWithReplacer<TG, TS, TP> : IAlgorithmBuilder
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  IReplacer<TG, TS, TP> Replacer { get; set; }
}

public interface IBuilderWithTerminator<TG, TR, TS, TP> : IAlgorithmBuilder
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  ITerminator<TG, TR, TS, TP> Terminator { get; set; }
}

public interface IBuilderWithInterceptor<TG, TR, TS, TP> : IAlgorithmBuilder
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  IInterceptor<TG, TS, TP, TR>? Interceptor { get; set; }
}
