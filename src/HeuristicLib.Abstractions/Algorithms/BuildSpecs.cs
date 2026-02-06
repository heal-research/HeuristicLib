using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

public abstract record AlgorithmBuildSpec<TG, TS, TP, TR> : IBuildSpec, ISpecWithEvaluator<TG, TS, TP>
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, IAlgorithmState
{
  public IEvaluator<TG, TS, TP> Evaluator { get; set; }
  public ITerminator<TG, TR, TS, TP> Terminator { get; set; }
  public IInterceptor<TG, TR, TS, TP>? Interceptor { get; set; }
  // public IIterationObserver<TG, TS, TP, TR>? Observer { get; set; }

  protected AlgorithmBuildSpec(
    IEvaluator<TG, TS, TP> evaluator,
    ITerminator<TG, TR, TS, TP> terminator,
    IInterceptor<TG, TR, TS, TP>? interceptor
    // IIterationObserver<TG, TS, TP, TR>? observer
  )
  {
    Evaluator = evaluator;
    Terminator = terminator;
    Interceptor = interceptor;
    // Observer = observer;
  }
}

public interface ISpecWithEvaluator<TG, TS, TP> : IBuildSpec
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  IEvaluator<TG, TS, TP> Evaluator { get; set; }
}

public interface ISpecWithCreator<TG, TS, TP> : IBuildSpec
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  ICreator<TG, TS, TP> Creator { get; set; }
}

public interface ISpecWithSelector<TG, TS, TP> : IBuildSpec
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  ISelector<TG, TS, TP> Selector { get; set; }
}

public interface ISpecWithCrossover<TG, TS, TP> : IBuildSpec
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  ICrossover<TG, TS, TP> Crossover { get; set; }
}

public interface ISpecWithMutator<TG, TS, TP> : IBuildSpec
  where TG : class
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  IMutator<TG, TS, TP> Mutator { get; set; }
}
