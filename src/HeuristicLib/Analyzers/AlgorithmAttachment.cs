using HEAL.HeuristicLib.OperatorPrototypes;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analyzers;

public static class AlgorithmAttachment {
  public static void
    AddAttachment<T1, TS1, TP1, TRes1, T, TS, TP, TRes>(this IAlgorithmBuilder<T1, TS1, TP1, TRes1> proto,
                                                        IAlgorithmAttachment<T, TS, TP, TRes> analysis)
    where TS1 : class, TS
    where TP1 : class, TP, IProblem<T1, TS1>
    where TRes1 : TRes
    where TS : class, ISearchSpace<T>
    where TP : class, IProblem<T, TS>
    where TRes : IIterationState
    where T1 : class, T {
    analysis.AttachTo(proto);
  }
}

public abstract class AlgorithmAttachment<T> : AlgorithmAttachment<T, IIterationState>
  where T : class;

public abstract class AlgorithmAttachment<T, TRes> : AlgorithmAttachment<T, ISearchSpace<T>, TRes>
  where T : class where TRes : IIterationState;

public abstract class AlgorithmAttachment<T, TS, TRes> : AlgorithmAttachment<T, TS, IProblem<T, TS>, TRes>
  where T : class where TS : class, ISearchSpace<T> where TRes : IIterationState;

public abstract class AlgorithmAttachment<T, TS, TP, TRes> : IAlgorithmAttachment<T, TS, TP, TRes>
  where T : class
  where TS : class, ISearchSpace<T>
  where TP : class, IProblem<T, TS>
  where TRes : IIterationState {
  public void AttachTo<T1, TRes1, TS1, TP1>(IAlgorithmBuilder<T1, TS1, TP1, TRes1> proto) where T1 : class, T
    where TRes1 : IIterationState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP {
    if (proto is IMutatorPrototype<T1, TS1, TP1> mutatorPrototype)
      mutatorPrototype.Mutator = WrapMutator(mutatorPrototype.Mutator);
    if (proto is ICrossoverPrototype<T1, TS1, TP1> crossoverPrototype)
      crossoverPrototype.Crossover = WrapCrossover(crossoverPrototype.Crossover);
    if (proto is ISelectorPrototype<T1, TS1, TP1> selectorPrototype)
      selectorPrototype.Selector = WrapSelector(selectorPrototype.Selector);
    proto.Evaluator = WrapEvaluator(proto.Evaluator);
    proto.Terminator = WrapTerminator(proto.Terminator);
    proto.Interceptor = WrapInterceptor(proto.Interceptor);
    proto.Creator = WrapCreator(proto.Creator);
  }

  public virtual IInterceptor<T1, TRes1, TS1, TP1>? WrapInterceptor<T1, TRes1, TS1, TP1>(IInterceptor<T1, TRes1, TS1, TP1>? interceptor) where T1 : T
    where TRes1 : IIterationState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => interceptor;

  public virtual ITerminator<T1, TRes1, TS1, TP1> WrapTerminator<T1, TRes1, TS1, TP1>(ITerminator<T1, TRes1, TS1, TP1> terminator) where T1 : class, T
    where TRes1 : IIterationState, TRes
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => terminator;

  public virtual IEvaluator<T1, TS1, TP1> WrapEvaluator<T1, TS1, TP1>(IEvaluator<T1, TS1, TP1> evaluator) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => evaluator;

  public virtual IMutator<T1, TS1, TP1> WrapMutator<T1, TS1, TP1>(IMutator<T1, TS1, TP1> mutator) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => mutator;

  public virtual ICrossover<T1, TS1, TP1> WrapCrossover<T1, TS1, TP1>(ICrossover<T1, TS1, TP1> crossover) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => crossover;

  public virtual ISelector<T1, TS1, TP1> WrapSelector<T1, TS1, TP1>(ISelector<T1, TS1, TP1> selector) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => selector;

  public virtual ICreator<T1, TS1, TP1> WrapCreator<T1, TS1, TP1>(ICreator<T1, TS1, TP1> creator) where T1 : class, T
    where TS1 : class, ISearchSpace<T1>, TS
    where TP1 : class, IProblem<T1, TS1>, TP => creator;
}
