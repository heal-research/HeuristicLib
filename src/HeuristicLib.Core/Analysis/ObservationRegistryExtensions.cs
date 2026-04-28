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

public static class ObservationRegistryExtensions
{
  extension(IObservationRegistry observationRegistry)
  {
    public void Add<TG, TS, TP>(ICreator<TG, TS, TP> creator, ICreatorObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(new CreatorObservationInstaller<TG, TS, TP>(creator, observer));

    public void Add<TG, TS, TP>(ICreator<TG, TS, TP> creator, Action<IReadOnlyList<TG>, int, TS, TP> afterCreation)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(creator, new ActionCreatorObserver<TG, TS, TP>(afterCreation));

    public void Add<TG, TS, TP>(ICrossover<TG, TS, TP> crossover, ICrossoverObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(new CrossoverObservationInstaller<TG, TS, TP>(crossover, observer));

    public void Add<TG, TS, TP>(ICrossover<TG, TS, TP> crossover, Action<IReadOnlyList<TG>, IReadOnlyList<IParents<TG>>, TS, TP> afterCross)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(crossover, new ActionCrossoverObserver<TG, TS, TP>(afterCross));

    public void Add<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator, IEvaluatorObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(new EvaluatorObservationInstaller<TG, TS, TP>(evaluator, observer));

    public void Add<TG, TS, TP>(IEvaluator<TG, TS, TP> evaluator, Action<IReadOnlyList<TG>, IReadOnlyList<ObjectiveVector>, TS, TP> afterEvaluation)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(evaluator, new ActionEvaluatorObserver<TG, TS, TP>(afterEvaluation));

    public void Add<TG, TS, TP, TR>(IInterceptor<TG, TS, TP, TR> interceptor, IInterceptorObserver<TG, TS, TP, TR> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      where TR : class, ISearchState
      => observationRegistry.Add(new InterceptorObservationInstaller<TG, TS, TP, TR>(interceptor, observer));

    public void Add<TG, TS, TP, TR>(IInterceptor<TG, TS, TP, TR> interceptor, Action<TR, TR, TR?, TS, TP> afterInterception)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      where TR : class, ISearchState
      => observationRegistry.Add(interceptor, new ActionInterceptorObserver<TG, TS, TP, TR>(afterInterception));

    public void Add<TG, TS, TP>(IMutator<TG, TS, TP> mutator, IMutatorObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(new MutatorObservationInstaller<TG, TS, TP>(mutator, observer));

    public void Add<TG, TS, TP>(IMutator<TG, TS, TP> mutator, Action<IReadOnlyList<TG>, IReadOnlyList<TG>, TS, TP> afterMutate)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(mutator, new ActionMutatorObserver<TG, TS, TP>(afterMutate));

    public void Add<TG, TS, TP>(IReplacer<TG, TS, TP> replacer, IReplacerObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(new ReplacerObservationInstaller<TG, TS, TP>(replacer, observer));

    public void Add<TG, TS, TP>(IReplacer<TG, TS, TP> replacer, Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, TS, TP> afterReplacement)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(replacer, new ActionReplacerObserver<TG, TS, TP>(afterReplacement));

    public void Add<TG, TS, TP>(ISelector<TG, TS, TP> selector, ISelectorObserver<TG, TS, TP> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(new SelectorObservationInstaller<TG, TS, TP>(selector, observer));

    public void Add<TG, TS, TP>(ISelector<TG, TS, TP> selector, Action<IReadOnlyList<ISolution<TG>>, IReadOnlyList<ISolution<TG>>, Objective, int, TS, TP> afterSelection)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      => observationRegistry.Add(selector, new ActionSelectorObserver<TG, TS, TP>(afterSelection));

    public void Add<TG, TS, TP, TR>(ITerminator<TG, TS, TP, TR> terminator, ITerminatorObserver<TG, TS, TP, TR> observer)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      where TR : class, ISearchState
      => observationRegistry.Add(new TerminatorObservationInstaller<TG, TS, TP, TR>(terminator, observer));

    public void Add<TG, TS, TP, TR>(ITerminator<TG, TS, TP, TR> terminator, Action<bool, TR, TS, TP> afterTerminationCheck)
      where TS : class, ISearchSpace<TG>
      where TP : class, IProblem<TG, TS>
      where TR : class, ISearchState
      => observationRegistry.Add(terminator, new ActionTerminatorObserver<TG, TS, TP, TR>(afterTerminationCheck));
  }
}
