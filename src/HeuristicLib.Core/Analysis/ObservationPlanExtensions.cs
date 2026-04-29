using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Analysis;

public static class ObservationPlanExtensions
{
  extension(ObservationPlan observations)
  {
    public void Observe<TG, TS, TP>(ICreator<TG, TS, TP> creator, ICreatorObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe<ICreator<TG, TS, TP>, ICreatorInstance<TG, TS, TP>, ICreatorObserver<TG, TS, TP>>(creator, observer, static (c, o) => c.ObserveWith(o));

    public void Observe<TG, TS, TP>(ICreator<TG, TS, TP> creator, Action<IReadOnlyList<TG>, int, TS, TP> afterCreation)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe(creator, new ActionCreatorObserver<TG, TS, TP>(afterCreation));

    public void Observe<TG, TS, TP>(ICrossover<TG, TS, TP> crossover, ICrossoverObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe<ICrossover<TG, TS, TP>, ICrossoverInstance<TG, TS, TP>, ICrossoverObserver<TG, TS, TP>>(crossover, observer, static (c, o) => c.ObserveWith(o));

    public void Observe<TG, TS, TP>(ICrossover<TG, TS, TP> crossover, Action<IReadOnlyList<TG>, IReadOnlyList<IParents<TG>>, TS, TP> afterCross)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe(crossover, new ActionCrossoverObserver<TG, TS, TP>(afterCross));

    public void Observe<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator, IEvaluatorObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe<IEvaluator<TG, TS, TP>, IEvaluatorInstance<TG, TS, TP>, IEvaluatorObserver<TG, TS, TP>>(evaluator, observer, static (e, o) => e.ObserveWith(o));

    public void Observe<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator, Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, TS, TP> afterEvaluation)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe(evaluator, new ActionEvaluatorObserver<TG, TS, TP>(afterEvaluation));

    public void Observe<TG, TS, TP, TR>(IInterceptor<TG, TS, TP, TR> interceptor, IInterceptorObserver<TG, TS, TP, TR> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      where TR : class, ISearchState
      => observations.Observe<IInterceptor<TG, TS, TP, TR>, IInterceptorInstance<TG, TS, TP, TR>, IInterceptorObserver<TG, TS, TP, TR>>(interceptor, observer, static (i, o) => i.ObserveWith(o));

    public void Observe<TG, TS, TP, TR>(IInterceptor<TG, TS, TP, TR> interceptor, Action<TR, TR, TR?, TS, TP> afterInterception)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      where TR : class, ISearchState
      => observations.Observe(interceptor, new ActionInterceptorObserver<TG, TS, TP, TR>(afterInterception));

    public void Observe<TG, TS, TP>(IMutator<TG, TS, TP> mutator, IMutatorObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe<IMutator<TG, TS, TP>, IMutatorInstance<TG, TS, TP>, IMutatorObserver<TG, TS, TP>>(mutator, observer, static (m, o) => m.ObserveWith(o));

    public void Observe<TG, TS, TP>(IMutator<TG, TS, TP> mutator, Action<IReadOnlyList<TG>, IReadOnlyList<TG>, TS, TP> afterMutate)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe(mutator, new ActionMutatorObserver<TG, TS, TP>(afterMutate));

    public void Observe<TG, TS, TP>(IReplacer<TG, TS, TP> replacer, IReplacerObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe<IReplacer<TG, TS, TP>, IReplacerInstance<TG, TS, TP>, IReplacerObserver<TG, TS, TP>>(replacer, observer, static (r, o) => r.ObserveWith(o));

    public void Observe<TG, TS, TP>(IReplacer<TG, TS, TP> replacer, Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, TS, TP> afterReplacement)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe(replacer, new ActionReplacerObserver<TG, TS, TP>(afterReplacement));

    public void Observe<TG, TS, TP>(ISelector<TG, TS, TP> selector, ISelectorObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe<ISelector<TG, TS, TP>, ISelectorInstance<TG, TS, TP>, ISelectorObserver<TG, TS, TP>>(selector, observer, static (s, o) => s.ObserveWith(o));

    public void Observe<TG, TS, TP>(ISelector<TG, TS, TP> selector, Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observations.Observe(selector, new ActionSelectorObserver<TG, TS, TP>(afterSelection));

    public void Observe<TG, TS, TP, TR>(ITerminator<TG, TS, TP, TR> terminator, ITerminatorObserver<TG, TS, TP, TR> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      where TR : class, ISearchState
      => observations.Observe<ITerminator<TG, TS, TP, TR>, ITerminatorInstance<TG, TS, TP, TR>, ITerminatorObserver<TG, TS, TP, TR>>(terminator, observer, static (t, o) => t.ObserveWith(o));

    public void Observe<TG, TS, TP, TR>(ITerminator<TG, TS, TP, TR> terminator, Action<bool, TR, TS, TP> afterTerminationCheck)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      where TR : class, ISearchState
      => observations.Observe(terminator, new ActionTerminatorObserver<TG, TS, TP, TR>(afterTerminationCheck));
  }
}
