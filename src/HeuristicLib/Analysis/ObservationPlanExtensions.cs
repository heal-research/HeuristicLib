using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Analysis;

public static class ObservationPlanExtensions
{
    extension(ObservationPlan observations)
    {
        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm, IAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe(algorithm, observer, static (a, o) => a.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(IAlgorithm<TCandidate, TSearchState> algorithm, Action<TSearchState, TSearchState?, TSearchSpace, TProblem> afterIteration)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe(algorithm, new ActionAlgorithmObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterIteration));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate> creator, ICreatorObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(creator, observer, static (c, o) => c.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate> creator, Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(creator, new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(afterCreation));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate> crossover, ICrossoverObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(crossover, observer, static (c, o) => c.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate> crossover, Action<IReadOnlyList<TCandidate>, IReadOnlyList<Parents<TCandidate>>, TSearchSpace, TProblem> afterCross)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(crossover, new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(afterCross));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> evaluator, IEvaluatorObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(evaluator, observer, static (e, o) => e.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate> evaluator, Action<IReadOnlyList<ObjectiveVector>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterEvaluation)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(evaluator, new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(afterEvaluation));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate> interceptor, IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe(interceptor, observer, static (i, o) => i.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate> interceptor, Action<TSearchState, TSearchState, TSearchState?, TSearchSpace, TProblem> afterInterception)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe(interceptor, new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterInterception));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate> mutator, IMutatorObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(mutator, observer, static (m, o) => m.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate> mutator, Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterMutate)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(mutator, new ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>(afterMutate));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate> refiner, IRefinerObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(refiner, observer, static (r, o) => r.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IRefiner<TCandidate> refiner, Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterRefine)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(refiner, new ActionRefinerObserver<TCandidate, TSearchSpace, TProblem>(afterRefine));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> replacer, IReplacerObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(replacer, observer, static (r, o) => r.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate> replacer, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, TSearchSpace, TProblem> afterReplacement)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(replacer, new ActionReplacerObserver<TCandidate, TSearchSpace, TProblem>(afterReplacement));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate> selector, ISelectorObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(selector, observer, static (s, o) => s.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate> selector, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, int, TSearchSpace, TProblem> afterSelection)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(selector, new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>(afterSelection));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate> terminator, ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe(terminator, observer, static (t, o) => t.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate> terminator, Action<bool, TSearchState, TSearchSpace, TProblem> afterTerminalStateCheck)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe(terminator, new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterTerminalStateCheck));
    }
}
