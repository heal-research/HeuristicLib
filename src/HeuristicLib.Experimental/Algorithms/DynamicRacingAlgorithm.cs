using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using MetaOptimizationGenotype = HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;
using MetaOptimizationProblem = HEAL.HeuristicLib.Problems.IProblem<HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>, HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.RealVectors.RealVectorSearchSpace, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>>;
using MetaOptimizationSearchSpace = HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.RealVectors.RealVectorSearchSpace, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>;

namespace HEAL.HeuristicLib.Algorithms;

public record DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>
    : IterativeAlgorithm<DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>, TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
    where TAlgorithm : IAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState>
{
    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
                                  IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
                                  IRacingStateMerger<TCandidate, TSearchState> stateMerger,
                                  Func<MetaOptimizationGenotype, TAlgorithm> algBuilder,
                                  Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluatorSelector)
        : this(metaSpace, creator, mutator, stateMerger, (candidate, _) => algBuilder(candidate), evaluatorSelector)
    { }

    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
                                  IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
                                  Func<TSearchState[], TSearchState> stateMerger,
                                  Func<MetaOptimizationGenotype, TAlgorithm> algBuilder,
                                  Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluatorSelector)
        : this(metaSpace, creator, mutator,
            new DelegatingRacingStateMerger<TCandidate, TSearchState>((states, _) => stateMerger(states.ToArray())),
            (candidate, _) => algBuilder(candidate), evaluatorSelector)
    { }

    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
                                  IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
                                  IRacingStateMerger<TCandidate, TSearchState> stateMerger,
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

    public IRacingStateMerger<TCandidate, TSearchState> StateMerger { get; }
    public ICreator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Creator { get; }
    public IMutator<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> Mutator { get; }
    private MetaOptimizationSearchSpace MetaSpace { get; }
    private EmptyMetaOptProblem EmptyMetaOptProblem { get; }
    public Func<MetaOptimizationGenotype, TAlgorithm?, TAlgorithm> AlgBuilder { get; }
    public Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> EvaluatorSelector { get; }
    public required int NoRacers { get; init; } = 2;
    public double HallOfFameStrength { get; init; } = 0.1;
    public double EarlyTerminationStrength { get; init; } = 0.1;
    public int MinimumModelObservationCount { get; init; } = 10;
    public int ModelObservationInterval { get; init; } = 100;
    public Func<ObjectiveVector, double> ObjectiveValueSelector { get; init; } = static objectiveVector => objectiveVector[0];

    private readonly int burnInEpochs;

    public int BurnInEpochs
    {
        get => burnInEpochs;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            burnInEpochs = value;
        }
    }

    protected override IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState> CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry, IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? resolvedInterceptor) =>
        new Instance(instanceRegistry, resolvedInterceptor, instanceRegistry.Resolve(Creator), instanceRegistry.Resolve(Mutator), MetaSpace, EmptyMetaOptProblem, StateMerger, AlgBuilder,
            EvaluatorSelector, NoRacers, HallOfFameStrength, EarlyTerminationStrength, BurnInEpochs, MinimumModelObservationCount, ModelObservationInterval, ObjectiveValueSelector);

    private sealed class Instance(
        ExecutionInstanceRegistry registry,
        IInterceptorInstance<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor,
        ICreatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
        IMutatorInstance<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
        MetaOptimizationSearchSpace metaSpace,
        EmptyMetaOptProblem emptyMetaOptProblem,
        IRacingStateMerger<TCandidate, TSearchState> stateMerger,
        Func<MetaOptimizationGenotype, TAlgorithm?, TAlgorithm> algorithmBuilder,
        Func<TAlgorithm, IEvaluator<TCandidate, TSearchSpace, TProblem>> evaluatorSelector,
        int noRacers,
        double hallOfFameStrength,
        double earlyTerminationStrength,
        int burnInEpochs,
        int minimumModelObservationCount,
        int modelObservationInterval,
        Func<ObjectiveVector, double> objectiveValueSelector)
        : IterativeAlgorithmInstance<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor)
    {
        private readonly Dictionary<string, HallOfFameEntry> hallOfFame = [];
        private MetaOptimizationGenotype? incumbent;
        private TAlgorithm? incumbentAlgorithm;
        private long completedRaces;
        private long completedEpochs;

        protected override TSearchState ExecuteStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            incumbent ??= creator.Create(1, random, metaSpace, emptyMetaOptProblem)[0];
            if (completedEpochs < burnInEpochs)
                return ExecuteBurnInStep(previousState, problem, random);

            var entries = new List<Entry>(noRacers);
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

                    // MakeMove can raise OnEpochChange, which ends the race. Never overwrite that signal.
                    raceEnded |= CanTerminateRace(entries, problem);
                }
            }
            finally
            {
                problem.EpochClock.OnEpochChange -= OnEpochChange;
            }

            try
            {
                var winner = SelectWinner(entries, problem.Objective);
                incumbent = entries[winner].Candidate;
                incumbentAlgorithm = entries[winner].Algorithm;
                completedRaces++;
                completedEpochs++;
                RecordSuccess(incumbent);

                return stateMerger.Merge(entries.Select(x => x.LastState!).ToArray(), problem.Objective);
            }
            finally
            {
                foreach (var entry in entries)
                    entry.Dispose();
            }

            void OnEpochChange(object? sender, int epoch) => raceEnded = true;
        }

        private TSearchState ExecuteBurnInStep(TSearchState? previousState, TProblem problem, IRandomNumberGenerator random)
        {
            using var entry = CreateEntry(incumbent!, incumbentAlgorithm, previousState, problem, random);
            var epochEnded = false;
            try
            {
                problem.EpochClock.OnEpochChange += OnEpochChange;
                while (!epochEnded)
                    _ = entry.MakeMove(problem, random, CancellationToken.None);
            }
            finally
            {
                problem.EpochClock.OnEpochChange -= OnEpochChange;
            }

            incumbentAlgorithm = entry.Algorithm;
            completedEpochs++;
            return entry.LastState!;

            void OnEpochChange(object? sender, int epoch) => epochEnded = true;
        }

        private static int SelectWinner(IReadOnlyList<Entry> entries, ObjectiveDirections objective)
        {
            var winner = -1;
            ObjectiveVector? winnerObjectiveVector = null;
            var comparer = GetComparer(objective);
            for (var i = 0; i < entries.Count; i++)
            {
                var objectiveVector = entries[i].CurrentBestObjectiveVector;
                if (objectiveVector is null)
                    continue;

                if (winnerObjectiveVector is not null && comparer.Compare(objectiveVector, winnerObjectiveVector) >= 0)
                    continue;

                winner = i;
                winnerObjectiveVector = objectiveVector;
            }

            if (winner < 0)
                throw new InvalidOperationException("Race cannot select a winner before any contender observed a fresh objective vector.");

            return winner;
        }

        private bool CanTerminateRace(IReadOnlyList<Entry> entries, TProblem problem)
        {
            if (earlyTerminationStrength <= 0)
                return false;

            var bestIndex = SelectWinner(entries, problem.Objective);
            var best = entries[bestIndex];
            var predictionHorizon = problem.EpochClock.EpochLength;
            var requiredEvaluationCount = predictionHorizon * earlyTerminationStrength;
            return entries.Where((_, index) => index != bestIndex)
                .All(contender => CanTerminateContender(best, contender, requiredEvaluationCount, predictionHorizon, problem.Objective));
        }

        private bool CanTerminateContender(Entry best, Entry contender, double requiredEvaluationCount, double predictionHorizon, ObjectiveDirections objective)
        {
            if (best.CurrentBestObjectiveValue is not { } bestValue || contender.CurrentBestObjectiveValue is not { } contenderValue)
                return false;

            return IsBetter(bestValue, contenderValue, objective)
                   && best.ModelObservationCount > minimumModelObservationCount
                   && contender.ModelObservationCount > minimumModelObservationCount
                   && best.UsedCount > requiredEvaluationCount / 2
                   && contender.UsedCount > requiredEvaluationCount / 2
                   && IsBetter(best.PredictObjectiveValue(predictionHorizon), contender.PredictObjectiveValue(predictionHorizon), objective, Math.Abs(bestValue - contenderValue) / 2);
        }

        private static bool IsBetter(double left, double right, ObjectiveDirections objective, double noise = 0)
        {
            if (objective.Directions[0] == ObjectiveDirection.Minimize)
                (left, right) = (right, left);

            if (noise < 0)
                noise = 0;

            return left - right > noise;
        }

        private Entry CreateEntry(MetaOptimizationGenotype candidate, TAlgorithm? sourceAlgorithm, TSearchState? initialState, TProblem problem, IRandomNumberGenerator random)
        {
            var algorithm = algorithmBuilder(candidate, sourceAlgorithm);
            return new Entry(algorithm, evaluatorSelector(algorithm), candidate, problem, random, initialState, CancellationToken.None, registry, modelObservationInterval, objectiveValueSelector);
        }

        private MetaOptimizationGenotype CreateChallenger(MetaOptimizationGenotype currentIncumbent, IRandomNumberGenerator random) =>
            TryReviveFromHallOfFame(currentIncumbent, random) ?? mutator.Mutate([currentIncumbent], random, metaSpace, emptyMetaOptProblem)[0];

        private MetaOptimizationGenotype? TryReviveFromHallOfFame(MetaOptimizationGenotype currentIncumbent, IRandomNumberGenerator random)
        {
            if (hallOfFame.Count < 2 || hallOfFameStrength <= 0 || random.NextDouble() >= hallOfFameStrength)
                return null;

            var incumbentKey = CreateKey(currentIncumbent);
            var candidates = hallOfFame.Values.Where(entry => entry.Key != incumbentKey).ToArray();
            if (candidates.Length == 0)
                return null;

            var totalWeight = candidates.Sum(entry => entry.SuccessCount);
            var selectedWeight = random.NextDouble() * totalWeight;
            foreach (var entry in candidates)
            {
                selectedWeight -= entry.SuccessCount;
                if (selectedWeight <= 0)
                    return Copy(entry.Candidate);
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
        private readonly IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator;
        private readonly PerformanceTrackingEvaluatorObserver performanceObserver;
        private IEnumerator<TSearchState> running;

        public Entry(TAlgorithm algorithm, IEvaluator<TCandidate, TSearchSpace, TProblem> evaluator, MetaOptimizationGenotype candidate, TProblem problem, IRandomNumberGenerator random,
                     TSearchState? initialState, CancellationToken ct, ExecutionInstanceRegistry parentRegistry, int modelObservationInterval, Func<ObjectiveVector, double> objectiveValueSelector)
        {
            Algorithm = algorithm;
            this.evaluator = evaluator;
            ParentRegistry = parentRegistry;
            performanceObserver = new PerformanceTrackingEvaluatorObserver(modelObservationInterval, objectiveValueSelector);
            Candidate = candidate;
            LastState = initialState;
            running = CreateEnumerator(problem, random, initialState, ct);
        }

        public MetaOptimizationGenotype Candidate { get; }
        public TAlgorithm Algorithm { get; }
        private ExecutionInstanceRegistry ParentRegistry { get; }
        public TSearchState? LastState { get; private set; }
        public int UsedCount => performanceObserver.EvaluatedCandidateCount;
        public ObjectiveVector? CurrentBestObjectiveVector => performanceObserver.CurrentBestObjectiveVector;
        public double? CurrentBestObjectiveValue => performanceObserver.CurrentBestObjectiveValue;
        public int ModelObservationCount => performanceObserver.ModelObservationCount;

        public double PredictObjectiveValue(double x) => performanceObserver.PredictObjectiveValue(x);

        public TSearchState MakeMove(TProblem problem, IRandomNumberGenerator random, CancellationToken ct)
        {
            if (!running.MoveNext())
            {
                running.Dispose();
                running = CreateEnumerator(problem, random, LastState, ct);
                if (!running.MoveNext())
                    throw new InvalidOperationException("Algorithm cannot start or resume execution");
            }

            LastState = running.Current;
            return LastState;
        }

        public void Dispose() => running.Dispose();

        private IEnumerator<TSearchState> CreateEnumerator(TProblem problem, IRandomNumberGenerator random, TSearchState? initialState, CancellationToken ct)
        {
            var inheritedRegistry = ParentRegistry.CreateChildRegistry();
            var inheritedEvaluator = inheritedRegistry.Resolve(evaluator);
            var contenderRegistry = inheritedRegistry.CreateChildRegistry();
            contenderRegistry.RegisterInstance(evaluator, new PerformanceTrackingEvaluatorInstance(inheritedEvaluator, performanceObserver));
            return contenderRegistry.Resolve(Algorithm).Stream(problem, random, initialState, ct).GetEnumerator();
        }
    }

    private sealed class PerformanceTrackingEvaluatorInstance(
        IEvaluatorInstance<TCandidate, TSearchSpace, TProblem> innerEvaluator,
        IEvaluatorObserver<TCandidate, TSearchSpace, TProblem> observer)
        : IEvaluatorInstance<TCandidate, TSearchSpace, TProblem>
    {
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            var objectiveVectors = innerEvaluator.Evaluate(candidates, random, searchSpace, problem);
            observer.AfterEvaluation(objectiveVectors, candidates, searchSpace, problem);
            return objectiveVectors;
        }
    }

    private sealed class PerformanceTrackingEvaluatorObserver : IEvaluatorObserver<TCandidate, TSearchSpace, TProblem>
    {
        private readonly int modelObservationInterval;
        private readonly Func<ObjectiveVector, double> objectiveValueSelector;
        private int nextModelObservationAt;

        public PerformanceTrackingEvaluatorObserver(int modelObservationInterval, Func<ObjectiveVector, double> objectiveValueSelector)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modelObservationInterval);
            this.modelObservationInterval = modelObservationInterval;
            this.objectiveValueSelector = objectiveValueSelector;
            nextModelObservationAt = modelObservationInterval;
        }

        public int EvaluatedCandidateCount { get; private set; }
        public ObjectiveVector? CurrentBestObjectiveVector { get; private set; }
        public double? CurrentBestObjectiveValue { get; private set; }
        public int ModelObservationCount => CurveModel.ObservationCount;
        public OnlineWeibullCurveModel CurveModel { get; } = new();

        public void AfterEvaluation(IReadOnlyList<ObjectiveVector> objectiveVectors, IReadOnlyList<TCandidate> candidates, TSearchSpace searchSpace, TProblem problem)
        {
            EvaluatedCandidateCount += candidates.Count;
            if (objectiveVectors.Count == 0)
                return;

            var comparer = GetComparer(problem.Objective);
            var batchBest = objectiveVectors.MinBy(x => x, comparer)!;
            if (CurrentBestObjectiveVector is null || comparer.Compare(batchBest, CurrentBestObjectiveVector) < 0)
                CurrentBestObjectiveVector = batchBest;

            CurrentBestObjectiveValue = objectiveValueSelector(CurrentBestObjectiveVector);
            RecordModelObservationIfDue();
        }

        public double PredictObjectiveValue(double x) => CurveModel.Predict(x);

        private void RecordModelObservationIfDue()
        {
            if (EvaluatedCandidateCount < nextModelObservationAt || CurrentBestObjectiveVector is not { } currentBest)
                return;

            var objectiveValue = objectiveValueSelector(currentBest);
            if (double.IsFinite(objectiveValue))
                CurveModel.AddObservation(EvaluatedCandidateCount, objectiveValue);

            do
            {
                nextModelObservationAt += modelObservationInterval;
            } while (nextModelObservationAt <= EvaluatedCandidateCount);
        }
    }

    private static IComparer<ObjectiveVector> GetComparer(ObjectiveDirections objective) =>
        objective.TotalOrderComparer is NoTotalOrderComparer
            ? new LexicographicComparer(objective.Directions)
            : objective.TotalOrderComparer;
}
