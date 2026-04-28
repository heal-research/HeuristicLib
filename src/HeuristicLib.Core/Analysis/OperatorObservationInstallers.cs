using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

internal abstract class OperatorObservationInstaller<TObserver>(IOperator @operator, TObserver observer) : IOperatorObservationInstaller
  where TObserver : class
{
  private readonly List<TObserver> observers = [observer];

  public IOperator Operator { get; } = @operator;

  protected ImmutableArray<TObserver> Observers => [.. observers];

  public bool TryMerge(IOperatorObservationInstaller other)
  {
    if (other is not OperatorObservationInstaller<TObserver> typedOther ||
        typedOther.GetType() != GetType() ||
        !ReferenceEquals(typedOther.Operator, Operator)) {
      return false;
    }

    observers.AddRange(typedOther.observers);
    return true;
  }

  public abstract void Install(ExecutionInstanceRegistry registry);
}

internal sealed class CreatorObservationInstaller<TG, TS, TP>(ICreator<TG, TS, TP> creator, ICreatorObserver<TG, TS, TP> observer)
  : OperatorObservationInstaller<ICreatorObserver<TG, TS, TP>>(creator, observer)
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public override void Install(ExecutionInstanceRegistry registry)
    => registry.PreRegister(creator, creator.ObserveWith(Observers));
}

internal sealed class CrossoverObservationInstaller<TG, TS, TP>(ICrossover<TG, TS, TP> crossover, ICrossoverObserver<TG, TS, TP> observer)
  : OperatorObservationInstaller<ICrossoverObserver<TG, TS, TP>>(crossover, observer)
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public override void Install(ExecutionInstanceRegistry registry)
    => registry.PreRegister(crossover, crossover.ObserveWith(Observers));
}

internal sealed class EvaluatorObservationInstaller<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator, IEvaluatorObserver<TG, TS, TP> observer)
  : OperatorObservationInstaller<IEvaluatorObserver<TG, TS, TP>>(evaluator, observer)
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public override void Install(ExecutionInstanceRegistry registry)
    => registry.PreRegister(evaluator, evaluator.ObserveWith(Observers));
}

internal sealed class InterceptorObservationInstaller<TG, TS, TP, TR>(IInterceptor<TG, TS, TP, TR> interceptor, IInterceptorObserver<TG, TS, TP, TR> observer)
  : OperatorObservationInstaller<IInterceptorObserver<TG, TS, TP, TR>>(interceptor, observer)
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, ISearchState
{
  public override void Install(ExecutionInstanceRegistry registry)
    => registry.PreRegister(interceptor, interceptor.ObserveWith(Observers));
}

internal sealed class MutatorObservationInstaller<TG, TS, TP>(IMutator<TG, TS, TP> mutator, IMutatorObserver<TG, TS, TP> observer)
  : OperatorObservationInstaller<IMutatorObserver<TG, TS, TP>>(mutator, observer)
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public override void Install(ExecutionInstanceRegistry registry)
    => registry.PreRegister(mutator, mutator.ObserveWith(Observers));
}

internal sealed class ReplacerObservationInstaller<TG, TS, TP>(IReplacer<TG, TS, TP> replacer, IReplacerObserver<TG, TS, TP> observer)
  : OperatorObservationInstaller<IReplacerObserver<TG, TS, TP>>(replacer, observer)
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public override void Install(ExecutionInstanceRegistry registry)
    => registry.PreRegister(replacer, replacer.ObserveWith(Observers));
}

internal sealed class SelectorObservationInstaller<TG, TS, TP>(ISelector<TG, TS, TP> selector, ISelectorObserver<TG, TS, TP> observer)
  : OperatorObservationInstaller<ISelectorObserver<TG, TS, TP>>(selector, observer)
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
{
  public override void Install(ExecutionInstanceRegistry registry)
    => registry.PreRegister(selector, selector.ObserveWith(Observers));
}

internal sealed class TerminatorObservationInstaller<TG, TS, TP, TR>(ITerminator<TG, TS, TP, TR> terminator, ITerminatorObserver<TG, TS, TP, TR> observer)
  : OperatorObservationInstaller<ITerminatorObserver<TG, TS, TP, TR>>(terminator, observer)
  where TS : class, ISearchSpace<TG>
  where TP : class, IProblem<TG, TS>
  where TR : class, ISearchState
{
  public override void Install(ExecutionInstanceRegistry registry)
    => registry.PreRegister(terminator, terminator.ObserveWith(Observers));
}
