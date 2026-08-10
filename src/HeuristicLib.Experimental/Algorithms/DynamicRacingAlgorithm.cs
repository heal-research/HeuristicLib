using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.States;
using JetBrains.Annotations;
using MetaOptimizationGenotype =
    HEAL.HeuristicLib.Genotypes.CompositeGenotype<HEAL.HeuristicLib.Genotypes.Vectors.RealVector,
        HEAL.HeuristicLib.Genotypes.Vectors.IntegerVector>;
using MetaOptimizationProblem =
    HEAL.HeuristicLib.Problems.IProblem<
        HEAL.HeuristicLib.Genotypes.CompositeGenotype<HEAL.HeuristicLib.Genotypes.Vectors.RealVector,
            HEAL.HeuristicLib.Genotypes.Vectors.IntegerVector>, HEAL.HeuristicLib.Genotypes.CompositeSearchSpace<
            HEAL.HeuristicLib.Genotypes.Vectors.RealVector, HEAL.HeuristicLib.SearchSpaces.Vectors.RealVectorSearchSpace
            , HEAL.HeuristicLib.Genotypes.Vectors.IntegerVector,
            HEAL.HeuristicLib.SearchSpaces.Vectors.IntegerVectorSearchSpace>>;
using MetaOptimizationSearchSpace =
    HEAL.HeuristicLib.Genotypes.CompositeSearchSpace<HEAL.HeuristicLib.Genotypes.Vectors.RealVector,
        HEAL.HeuristicLib.SearchSpaces.Vectors.RealVectorSearchSpace, HEAL.HeuristicLib.Genotypes.Vectors.IntegerVector,
        HEAL.HeuristicLib.SearchSpaces.Vectors.IntegerVectorSearchSpace>;

namespace HEAL.HeuristicLib.Algorithms;

public record DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>
    : IterativeAlgorithm<DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>,
        TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
    where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace,
                                      MetaOptimizationProblem> creator,
                                  IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace,
                                      MetaOptimizationProblem> mutator,
                                  Func<TSearchState[], TSearchState> stateMerger,
                                  Func<MetaOptimizationGenotype, TAlgorithm> algBuilder,
                                  Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluatorSelector)
        : this(metaSpace, creator, mutator, stateMerger, (candidate, _) => algBuilder(candidate),
            evaluatorSelector)
    { }

    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace,
                                      MetaOptimizationProblem> creator,
                                  IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace,
                                      MetaOptimizationProblem> mutator,
                                  Func<TSearchState[], TSearchState> stateMerger,
                                  Func<MetaOptimizationGenotype, TAlgorithm?, TAlgorithm> algBuilder,
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

    public Func<TSearchState[], TSearchState>
        StateMerger { get; } //TODO this could almost be a replacer or an interface
    public ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Creator { get; }
    public IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Mutator { get; }
    private MetaOptimizationSearchSpace MetaSpace { get; }
    private EmptyMetaOptProblem EmptyMetaOptProblem { get; }
    public Func<MetaOptimizationGenotype, TAlgorithm?, TAlgorithm> AlgBuilder { get; }
    public Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> EvaluatorSelector { get; }
    public required int NoRacers { get; init; } = 2;
    public double HallOfFameStrength { get; init; } = 0.1;

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>
        CreateIterativeAlgorithmInstance(ExecutionInstanceRegistry registry,
                                         IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>?
                                             resolvedInterceptor) =>
        new Instance(resolvedInterceptor, registry.Resolve(Creator), registry.Resolve(Mutator), MetaSpace,
            EmptyMetaOptProblem, StateMerger, AlgBuilder, EvaluatorSelector, NoRacers, HallOfFameStrength);

    private sealed class Instance(
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor,
        ICreatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
        IMutatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
        MetaOptimizationSearchSpace metaSpace,
        EmptyMetaOptProblem emptyMetaOptProblem,
        Func<TSearchState[], TSearchState> stateMerger,
        Func<MetaOptimizationGenotype, TAlgorithm?, TAlgorithm> algorithmBuilder,
        Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluatorSelector,
        int noRacers,
        double hallOfFameStrength)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor)
    {
        private readonly Dictionary<string, HallOfFameEntry> hallOfFame = [];
        private MetaOptimizationGenotype? incumbent;
        private TAlgorithm? incumbentAlgorithm;
        private long completedRaces;

        protected override TSearchState ExecuteStep(TSearchState? previousState, TProblem problem,
                                                    IRandomNumberGenerator random)
        {
            var entries = new List<Entry>(noRacers);
            incumbent ??= creator.Create(1, random, metaSpace, emptyMetaOptProblem)[0];
            entries.Add(CreateEntry(incumbent, incumbentAlgorithm, previousState, problem, random));
            for (var i = 1; i < noRacers; i++)
            {
                var challenger = CreateChallenger(incumbent, random);
                entries.Add(CreateEntry(challenger, entries[0].Algorithm, previousState, problem, random));
            }

            var raceEnded = false;
            try
            {
                problem.EpochClock.OnEpochChange += OnEpochChange;
                while (!raceEnded)
                {
                    var lowest = entries.MinBy(x => x.UsedCount);
                    _ = lowest!.MakeMove(problem, random, CancellationToken.None);
                }
            }
            finally
            {
                problem.EpochClock.OnEpochChange -= OnEpochChange;
            }

            try
            {
                var best = entries.Select((entry, index) => (
                    entry.LastState!.Population.Select(solution => solution.ObjectiveVector).Best(problem.Objective),
                    index));
                var winner = best.OrderBy(x => x.Item1, problem.Objective.TotalOrderComparer).First().index;
                incumbent = entries[winner].Candidate;
                incumbentAlgorithm = entries[winner].Algorithm;
                completedRaces++;
                RecordSuccess(incumbent);

                return stateMerger(entries.Select(x => x.LastState!).ToArray());
            }
            finally
            {
                foreach (var entry in entries)
                {
                    entry.Dispose();
                }
            }

            void OnEpochChange(object? sender, int epoch) => raceEnded = true;
        }

        private Entry CreateEntry(MetaOptimizationGenotype candidate, TAlgorithm? sourceAlgorithm,
                                  TSearchState? initialState, TProblem problem, IRandomNumberGenerator random)
        {
            var algorithm = algorithmBuilder(candidate, sourceAlgorithm);
            return new Entry(algorithm, evaluatorSelector(algorithm), candidate, problem, random, initialState,
                CancellationToken.None);
        }

        private MetaOptimizationGenotype CreateChallenger(MetaOptimizationGenotype currentIncumbent,
                                                          IRandomNumberGenerator random)
        {
            return TryReviveFromHallOfFame(currentIncumbent, random)
                   ?? mutator.Mutate([currentIncumbent], random, metaSpace, emptyMetaOptProblem)[0];
        }

        private MetaOptimizationGenotype? TryReviveFromHallOfFame(MetaOptimizationGenotype currentIncumbent,
                                                                  IRandomNumberGenerator random)
        {
            if (hallOfFame.Count < 2 || hallOfFameStrength <= 0 || random.NextDouble() >= hallOfFameStrength)
            {
                return null;
            }

            var incumbentKey = CreateKey(currentIncumbent);
            var candidates = hallOfFame.Values
                                       .Where(entry => entry.Key != incumbentKey)
                                       .ToArray();
            if (candidates.Length == 0)
            {
                return null;
            }

            var totalWeight = candidates.Sum(entry => entry.SuccessCount);
            var selectedWeight = random.NextDouble() * totalWeight;
            foreach (var entry in candidates)
            {
                selectedWeight -= entry.SuccessCount;
                if (selectedWeight <= 0)
                {
                    return Copy(entry.Candidate);
                }
            }

            return Copy(candidates[^1].Candidate);
        }

        private void RecordSuccess(MetaOptimizationGenotype candidate)
        {
            var key = CreateKey(candidate);
            if (hallOfFame.TryGetValue(key, out var entry))
            {
                entry.SuccessCount++;
                entry.LastWinRace = completedRaces;
                return;
            }

            hallOfFame.Add(key, new HallOfFameEntry(key, Copy(candidate), completedRaces));
        }

        private static MetaOptimizationGenotype Copy(MetaOptimizationGenotype candidate) =>
            new(RealVector.Create(candidate.Part1), IntegerVector.Create(candidate.Part2));

        private static string CreateKey(MetaOptimizationGenotype candidate)
        {
            var realKey = string.Join(",", candidate.Part1.Select(BitConverter.DoubleToInt64Bits));
            var integerKey = string.Join(",", candidate.Part2);
            return $"{realKey}|{integerKey}";
        }
    }

    private sealed class HallOfFameEntry(string key, MetaOptimizationGenotype candidate, long lastWinRace)
    {
        public string Key { get; } = key;
        public MetaOptimizationGenotype Candidate { get; } = candidate;
        public int SuccessCount { get; set; } = 1;
        public long LastWinRace { get; set; } = lastWinRace;
    }

    private sealed class Entry : IDisposable
    {
        private readonly TAlgorithm algorithm;
        private readonly IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator;
        private readonly ObservationCounter counter = new();
        private IEnumerator<TSearchState> running;

        public Entry(TAlgorithm algorithm, IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator,
                     MetaOptimizationGenotype candidate, TProblem problem, IRandomNumberGenerator random,
                     TSearchState? initialState, CancellationToken ct)
        {
            this.algorithm = algorithm;
            this.evaluator = evaluator;
            Candidate = candidate;
            LastState = initialState;
            running = CreateEnumerator(problem, random, initialState, ct);
        }

        public MetaOptimizationGenotype Candidate { get; }
        public TAlgorithm Algorithm => algorithm;
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

        [MustDisposeResource]
        private IEnumerator<TSearchState> CreateEnumerator(TProblem problem, IRandomNumberGenerator random,
                                                           TSearchState? initialState, CancellationToken ct)
        {
            var registry = new ExecutionInstanceRegistry();
            registry.RegisterReplacement(evaluator, evaluator.CountEvaluatedCandidates(counter));
            return registry.Resolve(algorithm).Stream(problem, random, initialState, ct).GetEnumerator();
        }
    }
}
