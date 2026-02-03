using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Prototypes;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Operators.Analyzers;

public static class AlgorithmAttachment
{
  public static void
    AddAttachment<T1, TE1, TP1, TRes1, T, TE, TP, TRes>(this IAlgorithmBuilder<T1, TE1, TP1, TRes1> proto,
      IAlgorithmAttachment<T, TE, TP, TRes> analysis)
    where TE1 : class, TE
    where TP1 : class, TP, IProblem<T1, TE1>
    where TRes1 : TRes
    where TE : class, ISearchSpace<T>
    where TP : class, IProblem<T, TE>
    where TRes : IAlgorithmState
    where T1 : class, T =>
    analysis.AttachTo(proto);
}

public abstract class AlgorithmAttachment<T> : AlgorithmAttachment<T, IAlgorithmState>
  where T : class;

public abstract class AlgorithmAttachment<T, TRes> : AlgorithmAttachment<T, ISearchSpace<T>, TRes>
  where T : class where TRes : IAlgorithmState;

public abstract class AlgorithmAttachment<T, TE, TRes> : AlgorithmAttachment<T, TE, IProblem<T, TE>, TRes>
  where T : class where TE : class, ISearchSpace<T> where TRes : IAlgorithmState;

public abstract class AlgorithmAttachment<T, TE, TP, TRes> : IAlgorithmAttachment<T, TE, TP, TRes>
  where T : class
  where TE : class, ISearchSpace<T>
  where TP : class, IProblem<T, TE>
  where TRes : IAlgorithmState
{
  public virtual void AttachTo<T1, TRes1, TE1, TP1>(IAlgorithmBuilder<T1, TE1, TP1, TRes1> proto) where T1 : class, T
    where TRes1 : IAlgorithmState, TRes
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP
  {
    if (proto is IMutatorPrototype<T1, TE1, TP1> mutatorPrototype) {
      mutatorPrototype.Mutator = WrapMutator(mutatorPrototype.Mutator);
    }
    if (proto is ICrossoverPrototype<T1, TE1, TP1> crossoverPrototype) {
      crossoverPrototype.Crossover = WrapCrossover(crossoverPrototype.Crossover);
    }
    if (proto is ISelectorPrototype<T1, TE1, TP1> selectorPrototype) {
      selectorPrototype.Selector = WrapSelector(selectorPrototype.Selector);
    }
    proto.Evaluator = WrapEvaluator(proto.Evaluator);
    proto.Terminator = WrapTerminator(proto.Terminator);
    proto.Interceptor = WrapInterceptor(proto.Interceptor);
    proto.Creator = WrapCreator(proto.Creator);
  }

  public virtual IInterceptor<T1, TRes1, TE1, TP1>? WrapInterceptor<T1, TRes1, TE1, TP1>(IInterceptor<T1, TRes1, TE1, TP1>? interceptor) where T1 : T
    where TRes1 : IAlgorithmState, TRes
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP => interceptor;

  public virtual ITerminator<T1, TRes1, TE1, TP1> WrapTerminator<T1, TRes1, TE1, TP1>(ITerminator<T1, TRes1, TE1, TP1> terminator) where T1 : class, T
    where TRes1 : IAlgorithmState, TRes
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP => terminator;

  public virtual IEvaluator<T1, TE1, TP1> WrapEvaluator<T1, TE1, TP1>(IEvaluator<T1, TE1, TP1> evaluator) where T1 : class, T
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP => evaluator;

  public virtual IMutator<T1, TE1, TP1> WrapMutator<T1, TE1, TP1>(IMutator<T1, TE1, TP1> mutator) where T1 : class, T
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP => mutator;

  public virtual ICrossover<T1, TE1, TP1> WrapCrossover<T1, TE1, TP1>(ICrossover<T1, TE1, TP1> crossover) where T1 : class, T
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP => crossover;

  public virtual ISelector<T1, TE1, TP1> WrapSelector<T1, TE1, TP1>(ISelector<T1, TE1, TP1> selector) where T1 : class, T
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP => selector;

  public virtual ICreator<T1, TE1, TP1> WrapCreator<T1, TE1, TP1>(ICreator<T1, TE1, TP1> creator) where T1 : class, T
    where TE1 : class, ISearchSpace<T1>, TE
    where TP1 : class, IProblem<T1, TE1>, TP => creator;
}
