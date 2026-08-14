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
        public void Observe<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator, ICreatorObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe<ICreator<TCandidate, TSearchSpace, TProblem>, ICreatorInstance<TCandidate, TSearchSpace, TProblem>, ICreatorObserver<TCandidate, TSearchSpace, TProblem>>(creator, observer, static (c, o) => c.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ICreator<TCandidate, TSearchSpace, TProblem> creator, Action<IReadOnlyList<TCandidate>, int, TSearchSpace, TProblem> afterCreation)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(creator, new ActionCreatorObserver<TCandidate, TSearchSpace, TProblem>(afterCreation));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover, ICrossoverObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe<ICrossover<TCandidate, TSearchSpace, TProblem>, ICrossoverInstance<TCandidate, TSearchSpace, TProblem>, ICrossoverObserver<TCandidate, TSearchSpace, TProblem>>(crossover, observer, static (c, o) => c.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ICrossover<TCandidate, TSearchSpace, TProblem> crossover, Action<IReadOnlyList<TCandidate>, IReadOnlyList<Parents<TCandidate>>, TSearchSpace, TProblem> afterCross)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(crossover, new ActionCrossoverObserver<TCandidate, TSearchSpace, TProblem>(afterCross));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, IEvaluatorObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe<IEvaluator<TCandidate, TSearchSpace, TProblem>, IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>, IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>>(evaluator, observer, static (e, o) => e.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, Action<IReadOnlyList<TCandidate>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, TSearchSpace, TProblem> afterEvaluation)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(evaluator, new ActionEvaluatorObserver<TCandidate, TSearchSpace, TProblem>(afterEvaluation));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor, IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe<IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState>, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>, IInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>>(interceptor, observer, static (i, o) => i.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(IInterceptor<TCandidate, TSearchSpace, TProblem, TSearchState> interceptor, Action<TSearchState, TSearchState, TSearchState?, TSearchSpace, TProblem> afterInterception)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe(interceptor, new ActionInterceptorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterInterception));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator, IMutatorObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe<IMutator<TCandidate, TSearchSpace, TProblem>, IMutatorInstance<TCandidate, TSearchSpace, TProblem>, IMutatorObserver<TCandidate, TSearchSpace, TProblem>>(mutator, observer, static (m, o) => m.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IMutator<TCandidate, TSearchSpace, TProblem> mutator, Action<IReadOnlyList<TCandidate>, IReadOnlyList<TCandidate>, TSearchSpace, TProblem> afterMutate)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(mutator, new ActionMutatorObserver<TCandidate, TSearchSpace, TProblem>(afterMutate));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> replacer, IReplacerObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe<IReplacer<TCandidate, TSearchSpace, TProblem>, IReplacerInstance<TCandidate, TSearchSpace, TProblem>, IReplacerObserver<TCandidate, TSearchSpace, TProblem>>(replacer, observer, static (r, o) => r.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(IReplacer<TCandidate, TSearchSpace, TProblem> replacer, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, TSearchSpace, TProblem> afterReplacement)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(replacer, new ActionReplacerObserver<TCandidate, TSearchSpace, TProblem>(afterReplacement));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector, ISelectorObserver<TCandidate, TSearchSpace, TProblem> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe<ISelector<TCandidate, TSearchSpace, TProblem>, ISelectorInstance<TCandidate, TSearchSpace, TProblem>, ISelectorObserver<TCandidate, TSearchSpace, TProblem>>(selector, observer, static (s, o) => s.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem>(ISelector<TCandidate, TSearchSpace, TProblem> selector, Action<IReadOnlyList<EvaluatedCandidate<TCandidate>>, IReadOnlyList<EvaluatedCandidate<TCandidate>>, ObjectiveDirections, int, TSearchSpace, TProblem> afterSelection)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          => observations.Observe(selector, new ActionSelectorObserver<TCandidate, TSearchSpace, TProblem>(afterSelection));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator, ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState> observer)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe<ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState>, ITerminatorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>, ITerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>>(terminator, observer, static (t, o) => t.ObserveWith(o));

        public void Observe<TCandidate, TSearchSpace, TProblem, TSearchState>(ITerminator<TCandidate, TSearchSpace, TProblem, TSearchState> terminator, Action<bool, TSearchState, TSearchSpace, TProblem> afterTerminalStateCheck)
          where TSearchSpace : class, ISearchSpace<TCandidate>
          where TProblem : class, IProblem<TCandidate, TSearchSpace>
          where TSearchState : class, ISearchState
          => observations.Observe(terminator, new ActionTerminatorObserver<TCandidate, TSearchSpace, TProblem, TSearchState>(afterTerminalStateCheck));
    }
}
