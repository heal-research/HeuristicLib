using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Algorithms;

using MetaOptimizationGenotype = CompositeGenotype<RealVector, IntegerVector>;
using MetaOptimizationProblem = IProblem<CompositeGenotype<RealVector, IntegerVector>, CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>>;
using MetaOptimizationSearchSpace = CompositeSearchSpace<RealVector, RealVectorSearchSpace, IntegerVector, IntegerVectorSearchSpace>;

public class EmptyMetaOptProblem : MetaOptimizationProblem
{
    public EmptyMetaOptProblem(MetaOptimizationSearchSpace searchSpace)
    {
        SearchSpace = searchSpace;
    }

    public MetaOptimizationSearchSpace SearchSpace { get; }
    public ObjectiveDirections Objective => throw new NotImplementedException();
    public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<MetaOptimizationGenotype> candidates, IRandomNumberGenerator random) => throw new NotImplementedException();
}

public record DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>
    : IterativeAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
    where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
                                  IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
                                  Func<TSearchState[], TSearchState> stateMerger,
                                  Func<MetaOptimizationGenotype, TAlgorithm> algBuilder,
                                  Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluatorSelector)
    {
        MetaSpace = metaSpace;
        Creator = creator;
        Mutator = mutator;
        StateMerger = stateMerger;
        AlgBuilder = algBuilder;
        EvaluatorSelector = evaluatorSelector;
        EmptyMetaOptProblem = new EmptyMetaOptProblem(MetaSpace);
    }

    public Func<TSearchState[], TSearchState> StateMerger { get; } //TODO this could almost be a replacer or an interface
    public ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Creator { get; }
    public IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Mutator { get; }
    private MetaOptimizationSearchSpace MetaSpace { get; }
    private EmptyMetaOptProblem EmptyMetaOptProblem { get; }
    public Func<MetaOptimizationGenotype, TAlgorithm> AlgBuilder { get; }
    public Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> EvaluatorSelector { get; }
    public required int NoRacers { get; init; } = 2;

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateIterativeAlgorithmInstance(ExecutionInstanceRegistry registry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? resolvedInterceptor) =>
        new Instance(resolvedInterceptor, registry.Resolve(Creator), registry.Resolve(Mutator), MetaSpace, EmptyMetaOptProblem, StateMerger, AlgBuilder, EvaluatorSelector, NoRacers);

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor,
        ICreatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
        IMutatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
        MetaOptimizationSearchSpace metaSpace,
        EmptyMetaOptProblem emptyMetaOptProblem,
        Func<TSearchState[], TSearchState> stateMerger,
        Func<MetaOptimizationGenotype, TAlgorithm> algorithmBuilder,
        Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluatorSelector,
        int noRacers)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor)
    {
        private MetaOptimizationGenotype? incumbent;

        protected override TSearchState ExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            var entries = new List<Entry>(noRacers);
            incumbent ??= creator.Create(1, random, metaSpace, emptyMetaOptProblem)[0];
            entries.Add(CreateEntry(incumbent, previousState, problem, random));
            for (var i = 1; i < noRacers; i++)
            {
                var challenger = mutator.Mutate([incumbent], random, metaSpace, emptyMetaOptProblem)[0];
                entries.Add(CreateEntry(challenger, previousState, problem, random));
            }

            problem.EpochClock.OnEpochChange += OnEpochChange;
            var raceEnded = false;
            while (!raceEnded)
            {
                var lowest = entries.MinBy(x => x.UsedCount);
                _ = lowest!.MakeMove(problem, random, CancellationToken.None);
            }

            problem.EpochClock.OnEpochChange -= OnEpochChange;
            foreach (var entry in entries)
            {
                entry.Dispose();
            }

            var best = entries.Select((entry, index) => (entry.LastState!.Population.Select(solution => solution.ObjectiveVector).Best(problem.Objective), index));
            var winner = best.OrderBy(x => x.Item1, problem.Objective.TotalOrderComparer).First().index;
            incumbent = entries[winner].Candidate;
            return stateMerger(entries.Select(x => x.LastState!).ToArray());

            void OnEpochChange(object? sender, int epoch) => raceEnded = true;
        }

        private Entry CreateEntry(MetaOptimizationGenotype candidate, TSearchState? initialState, TProblem problem, IRandomNumberGenerator random)
        {
            var algorithm = algorithmBuilder(candidate);
            return new Entry(algorithm, evaluatorSelector(algorithm), candidate, problem, random, initialState, CancellationToken.None);
        }
    }

    private sealed class Entry : IDisposable
    {
        private readonly TAlgorithm algorithm;
        private readonly IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator;
        private readonly ObservationCounter counter = new();
        private IEnumerator<TSearchState> running;

        public Entry(TAlgorithm algorithm, IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, MetaOptimizationGenotype candidate, TProblem problem, IRandomNumberGenerator random, TSearchState? initialState, CancellationToken ct)
        {
            this.algorithm = algorithm;
            this.evaluator = evaluator;
            Candidate = candidate;
            LastState = initialState;
            running = CreateEnumerator(problem, random, initialState, ct);
        }

        public MetaOptimizationGenotype Candidate { get; }
        public TSearchState? LastState { get; private set; }
        public int UsedCount => counter.CurrentCount;

        public TSearchState MakeMove(TProblem problem, IRandomNumberGenerator random, CancellationToken ct)
        {
            if (!running.MoveNext())
            {
                running.Dispose();
                running = CreateEnumerator(problem, random, LastState, ct);
                if (!running.MoveNext())
                {
                    throw new InvalidOperationException("Algorithm cannot start or resume execution");
                }
            }

            LastState = running.Current;
            return LastState;
        }

        public void Dispose() => running.Dispose();

        private IEnumerator<TSearchState> CreateEnumerator(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState, CancellationToken ct)
        {
            var run = algorithm.CreateRun(problem);
            var registry = run.CreateNewRegistry();
            registry.RegisterReplacement(evaluator, evaluator.CountEvaluatedCandidates(counter));
            return registry.Resolve(algorithm).RunStreaming(problem, random, initialState, ct).GetEnumerator();
        }
    }
}
