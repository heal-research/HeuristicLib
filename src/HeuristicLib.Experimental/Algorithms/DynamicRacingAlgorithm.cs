using System.Runtime.ExceptionServices;
using HEAL.HeuristicLib.Algorithms.AutoEC;
using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using MetaOptimizationGenotype =
    HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;
using MetaOptimizationProblem =
    HEAL.HeuristicLib.Problems.IProblem<
        HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
            HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>, HEAL.HeuristicLib.Encodings.Composite.
        CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
            HEAL.HeuristicLib.Encodings.RealVectors.BoundedRealVectorSearchSpace,
            HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector,
            HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>>;
using MetaOptimizationSearchSpace =
    HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.RealVectors.RealVector,
        HEAL.HeuristicLib.Encodings.RealVectors.BoundedRealVectorSearchSpace,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector,
        HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVectorSearchSpace>;

namespace HEAL.HeuristicLib.Algorithms;

public record DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>
    : IterativeAlgorithm<DynamicRacingAlgorithm<TCandidate, TSearchSpace, TProblem, TSearchState, TAlgorithm>,
        TCandidate, TSearchSpace, TProblem, TSearchState>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    where TSearchState : PopulationState<TCandidate>
    where TAlgorithm : IAlgorithm<TCandidate, TSearchState>
{
    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype> creator,
                                  IMutator<MetaOptimizationGenotype> mutator,
                                  IRacingStateMerger<TCandidate, TSearchState> stateMerger,
                                  Func<MetaOptimizationGenotype, TAlgorithm> algBuilder,
                                  Func<TAlgorithm, IEvaluator<TCandidate>> evaluatorSelector)
        : this(metaSpace, creator, mutator, stateMerger, (candidate, _) => algBuilder(candidate), evaluatorSelector)
    { }

    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype> creator,
                                  IMutator<MetaOptimizationGenotype> mutator,
                                  Func<TSearchState[], TSearchState> stateMerger,
                                  Func<MetaOptimizationGenotype, TAlgorithm> algBuilder,
                                  Func<TAlgorithm, IEvaluator<TCandidate>> evaluatorSelector)
        : this(metaSpace, creator, mutator,
            new DelegatingRacingStateMerger<TCandidate, TSearchState>((states, _) => stateMerger(states.ToArray())),
            (candidate, _) => algBuilder(candidate), evaluatorSelector)
    { }

    public DynamicRacingAlgorithm(MetaOptimizationSearchSpace metaSpace,
                                  ICreator<MetaOptimizationGenotype> creator,
                                  IMutator<MetaOptimizationGenotype> mutator,
                                  IRacingStateMerger<TCandidate, TSearchState> stateMerger,
                                  Func<MetaOptimizationGenotype, TAlgorithm?, TAlgorithm> algBuilder,
                                  Func<TAlgorithm, IEvaluator<TCandidate>> evaluatorSelector)
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
    public ICreator<MetaOptimizationGenotype> Creator { get; }
    public IMutator<MetaOptimizationGenotype> Mutator { get; }
    private MetaOptimizationSearchSpace MetaSpace { get; }
    private EmptyMetaOptProblem EmptyMetaOptProblem { get; }
    public Func<MetaOptimizationGenotype, TAlgorithm?, TAlgorithm> AlgBuilder { get; }
    public Func<TAlgorithm, IEvaluator<TCandidate>> EvaluatorSelector { get; }
    public required int NoRacers { get; init; } = 2;
    public double HallOfFameStrength { get; init; } = 0.1;
    public double EarlyTerminationStrength { get; init; } = 0.1;
    public int MinimumModelObservationCount { get; init; } = 10;
    public int ModelObservationInterval { get; init; } = 100;
    public Func<ObjectiveVector, double> ObjectiveValueSelector { get; init; } =
        static objectiveVector => objectiveVector[0];

    public Func<IDynamicRacingQualityTraceSink<TCandidate>?>? QualityTraceSinkFactory { get; init; }

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

    /// <remarks>
    /// A bound algorithm: unlike the general-purpose ones it reads the problem itself, requiring a
    /// <see cref="DynamicProblem{TSelf, TCandidate, TSearchSpace}"/>, so it names the search space and problem it is
    /// written for and the base reconciles them with the run's.
    /// </remarks>
    protected override ExecutionFactory<IterativeAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>>
        CreateIterationFactory()
    {
        var state = new ExecutionState { QualityTraceSink = QualityTraceSinkFactory?.Invoke() };
        return scope => new Execution(scope,
            scope.ResolveOptional<TCandidate, TSearchSpace, TProblem, TSearchState>(Interceptor),
            scope.Resolve<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem>(Creator),
            scope.Resolve<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem>(Mutator),
            MetaSpace, EmptyMetaOptProblem, StateMerger, AlgBuilder,
            EvaluatorSelector, NoRacers, HallOfFameStrength, EarlyTerminationStrength, BurnInEpochs,
            MinimumModelObservationCount, ModelObservationInterval, ObjectiveValueSelector, state);
    }

    private sealed class ExecutionState
    {
        public IDynamicRacingQualityTraceSink<TCandidate>? QualityTraceSink { get; init; }
        public Dictionary<string, HallOfFameEntry> HallOfFame { get; } = [];
        public MetaOptimizationGenotype? Incumbent { get; set; }
        public RacingEntry? IncumbentEntry { get; set; }
        public long CompletedRaces { get; set; }
        public long CompletedEpochs { get; set; }
    }

    private sealed class Execution(
        ResolutionScope scope,
        IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TSearchState>? interceptor,
        ICreatorExecution<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> creator,
        IMutatorExecution<MetaOptimizationGenotype, MetaOptimizationSearchSpace, MetaOptimizationProblem> mutator,
        MetaOptimizationSearchSpace metaSpace,
        EmptyMetaOptProblem emptyMetaOptProblem,
        IRacingStateMerger<TCandidate, TSearchState> stateMerger,
        Func<MetaOptimizationGenotype, TAlgorithm?, TAlgorithm> algorithmBuilder,
        Func<TAlgorithm, IEvaluator<TCandidate>> evaluatorSelector,
        int noRacers,
        double hallOfFameStrength,
        double earlyTerminationStrength,
        int burnInEpochs,
        int minimumModelObservationCount,
        int modelObservationInterval,
        Func<ObjectiveVector, double> objectiveValueSelector,
        ExecutionState state)
        : IterativeAlgorithmExecution<TCandidate, TSearchSpace, TProblem, TSearchState>(interceptor)
    {
        protected override TSearchState ExecuteStep(TSearchState? previousState, TProblem problem,
                                                    IRandomNumberGenerator random)
        {
            state.Incumbent ??= creator.Create(1, random, metaSpace, emptyMetaOptProblem)[0];
            if (state.CompletedEpochs < burnInEpochs)
                return ExecuteBurnInStep(previousState, problem, random);

            var entries = new List<RacingEntry>(noRacers);
            Exception? failure = null;
            TSearchState? nextState = null;
            try
            {
                entries.Add(GetOrRestartIncumbentEntry(previousState, problem, random));
                for (var i = 1; i < noRacers; i++)
                {
                    var revived = TryReviveFromHallOfFame(state.Incumbent, random);
                    var challenger = revived ??
                                     mutator.Mutate([state.Incumbent], random, metaSpace, emptyMetaOptProblem)[0];
                    entries.Add(CreateEntry(challenger, entries[0].Algorithm, previousState, problem, random, i,
                        revived is null
                            ? DynamicRacingConfigurationOrigin.Mutation
                            : DynamicRacingConfigurationOrigin.HallOfFame, entries[0].Scope.CreateForkedScope()));
                }

                var epoch = problem.CurrentEpoch;
                while (problem.CurrentEpoch == epoch)
                {
                    var lowest = entries.MinBy(x => x.UsedCount);
                    _ = lowest!.MakeMove(problem, random, CancellationToken.None);
                    if (CanTerminateRace(entries, problem))
                        break;
                }

                var winner = SelectWinner(entries, problem.Objective);
                state.Incumbent = entries[winner].Candidate;
                state.IncumbentEntry = entries[winner];
                state.QualityTraceSink?.AddRace(new DynamicRacingRaceSummary(state.CompletedRaces, epoch,
                    problem.CurrentEpoch != epoch
                        ? DynamicRacingRaceTerminationReason.EpochChange
                        : DynamicRacingRaceTerminationReason.EarlyTermination,
                    winner, Copy(state.Incumbent), problem.TotalEvaluations));
                state.CompletedRaces++;
                state.CompletedEpochs++;
                RecordSuccess(state.Incumbent);

                nextState = stateMerger.Merge(entries.Select(x => x.LastState!).ToArray(), problem.Objective);
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            List<Exception> cleanupFailures = [];
            foreach (var entry in entries)
            {
                try
                {
                    entry.Dispose();
                }
                catch (Exception exception)
                {
                    cleanupFailures.Add(exception);
                }
            }

            if (cleanupFailures.Count > 0)
            {
                if (failure is not null)
                    cleanupFailures.Insert(0, failure);
                throw new AggregateException("Failed to dispose racing contenders.", cleanupFailures);
            }

            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
            return nextState!;
        }

        private RacingEntry GetOrRestartIncumbentEntry(TSearchState? previousState, TProblem problem,
                                                       IRandomNumberGenerator random)
        {
            if (state.IncumbentEntry is not { } entry)
                return CreateEntry(state.Incumbent!, default, previousState, problem, random, 0,
                    DynamicRacingConfigurationOrigin.Incumbent);
            entry.Restart(scope, previousState, problem, random, modelObservationInterval, objectiveValueSelector,
                state.QualityTraceSink, state.CompletedRaces, 0, DynamicRacingConfigurationOrigin.Incumbent);
            return entry;
        }

        private TSearchState ExecuteBurnInStep(TSearchState? previousState, TProblem problem,
                                               IRandomNumberGenerator random)
        {
            using var entry = GetOrRestartIncumbentEntry(previousState, problem, random);
            var epoch = problem.CurrentEpoch;
            while (problem.CurrentEpoch == epoch)
                _ = entry.MakeMove(problem, random, CancellationToken.None);

            state.IncumbentEntry = entry;
            state.CompletedEpochs++;
            return entry.LastState!;
        }

        private static int SelectWinner(IReadOnlyList<RacingEntry> entries, ObjectiveDirections objective)
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
                throw new InvalidOperationException(
                    "Race cannot select a winner before any contender observed a fresh objective vector.");

            return winner;
        }

        private bool CanTerminateRace(IReadOnlyList<RacingEntry> entries, TProblem problem)
        {
            if (earlyTerminationStrength <= 0)
                return false;

            // Early termination projects a contender forward to the end of the epoch, so it needs a schedule that
            // measures epochs in the same evaluations the projection is made in.
            if (problem.EpochSchedule is not EvaluationCountSchedule { EvaluationsPerEpoch: var predictionHorizon })
                return false;

            var bestIndex = SelectWinner(entries, problem.Objective);
            var best = entries[bestIndex];
            var requiredEvaluationCount = predictionHorizon * earlyTerminationStrength;
            return entries.Where((_, index) => index != bestIndex)
                          .All(contender => CanTerminateContender(best, contender, requiredEvaluationCount,
                              predictionHorizon, problem.Objective));
        }

        private bool CanTerminateContender(RacingEntry best, RacingEntry contender, double requiredEvaluationCount,
                                           double predictionHorizon, ObjectiveDirections objective)
        {
            if (best.CurrentBestObjectiveValue is not { } bestValue ||
                contender.CurrentBestObjectiveValue is not { } contenderValue)
                return false;

            return IsBetter(bestValue, contenderValue, objective)
                   && best.ModelObservationCount > minimumModelObservationCount
                   && contender.ModelObservationCount > minimumModelObservationCount
                   && best.UsedCount > requiredEvaluationCount / 2
                   && contender.UsedCount > requiredEvaluationCount / 2
                   && IsBetter(best.PredictObjectiveValue(predictionHorizon),
                       contender.PredictObjectiveValue(predictionHorizon), objective,
                       Math.Abs(bestValue - contenderValue) / 2);
        }

        private static bool IsBetter(double left, double right, ObjectiveDirections objective, double noise = 0)
        {
            if (objective.Directions[0] == ObjectiveDirection.Minimize)
                (left, right) = (right, left);

            if (noise < 0)
                noise = 0;

            return left - right > noise;
        }

        private RacingEntry CreateEntry(MetaOptimizationGenotype candidate, TAlgorithm? sourceAlgorithm,
                                        TSearchState? initialState, TProblem problem, IRandomNumberGenerator random,
                                        int contenderIndex, DynamicRacingConfigurationOrigin origin,
                                        ResolutionScope? contenderScope = null)
        {
            var algorithm = algorithmBuilder(candidate, sourceAlgorithm);
            return new RacingEntry(algorithm, evaluatorSelector(algorithm), candidate, problem, random, initialState,
                CancellationToken.None, scope, modelObservationInterval, objectiveValueSelector, state.QualityTraceSink,
                state.CompletedRaces, contenderIndex, origin, contenderScope);
        }

        private MetaOptimizationGenotype CreateChallenger(MetaOptimizationGenotype currentIncumbent,
                                                          IRandomNumberGenerator random) =>
            TryReviveFromHallOfFame(currentIncumbent, random) ??
            mutator.Mutate([currentIncumbent], random, metaSpace, emptyMetaOptProblem)[0];

        private MetaOptimizationGenotype? TryReviveFromHallOfFame(MetaOptimizationGenotype currentIncumbent,
                                                                  IRandomNumberGenerator random)
        {
            if (state.HallOfFame.Count < 2 || hallOfFameStrength <= 0 || random.NextDouble() >= hallOfFameStrength)
                return null;

            var incumbentKey = CreateKey(currentIncumbent);
            var candidates = state.HallOfFame.Values.Where(entry => entry.Key != incumbentKey).ToArray();
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
            if (state.HallOfFame.TryGetValue(key, out var entry))
            {
                entry.SuccessCount++;
                entry.LastWinRace = state.CompletedRaces;
                return;
            }

            state.HallOfFame.Add(key, new HallOfFameEntry(key, Copy(candidate), state.CompletedRaces));
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

    private sealed class RacingEntry : IDisposable
    {
        private readonly IEvaluator<TCandidate> evaluator;
        private ContenderPerformance performance;
        private IEnumerator<TSearchState>? running;

        public RacingEntry(TAlgorithm algorithm, IEvaluator<TCandidate> evaluator, MetaOptimizationGenotype candidate,
                           TProblem problem, IRandomNumberGenerator random,
                           TSearchState? initialState, CancellationToken ct, ResolutionScope parentScope,
                           int modelObservationInterval, Func<ObjectiveVector, double> objectiveValueSelector,
                           IDynamicRacingQualityTraceSink<TCandidate>? qualityTraceSink, long race, int contenderIndex,
                           DynamicRacingConfigurationOrigin origin, ResolutionScope? contenderScope = null)
        {
            Algorithm = algorithm;
            this.evaluator = evaluator;
            _ = parentScope.Resolve<TCandidate, TSearchSpace, TProblem>(evaluator);
            Scope = contenderScope ?? parentScope.CreateChildScope();
            performance = new ContenderPerformance(modelObservationInterval, objectiveValueSelector, qualityTraceSink,
                race, contenderIndex, contenderIndex == 0, origin, candidate);
            Candidate = candidate;
            LastState = initialState;
            running = CreateEnumerator(problem, random, initialState, ct);
        }

        public MetaOptimizationGenotype Candidate { get; }
        public TAlgorithm Algorithm { get; }
        public ResolutionScope Scope { get; private set; }
        public TSearchState? LastState { get; private set; }
        public int UsedCount => performance.EvaluatedCandidateCount;
        public ObjectiveVector? CurrentBestObjectiveVector => performance.CurrentBestObjectiveVector;
        public double? CurrentBestObjectiveValue => performance.CurrentBestObjectiveValue;
        public int ModelObservationCount => performance.ModelObservationCount;

        public double PredictObjectiveValue(double x) => performance.PredictObjectiveValue(x);

        public TSearchState MakeMove(TProblem problem, IRandomNumberGenerator random, CancellationToken ct)
        {
            var current = running ?? throw new InvalidOperationException("Contender execution has been disposed.");
            if (!current.MoveNext())
            {
                Dispose();
                current = running = CreateEnumerator(problem, random, LastState, ct);
                if (!current.MoveNext())
                    throw new InvalidOperationException("Algorithm cannot start or resume execution");
            }

            LastState = current.Current;
            return LastState;
        }

        public void Dispose()
        {
            var current = running;
            running = null;
            current?.Dispose();
        }

        public void Restart(ResolutionScope context, TSearchState? initialState, TProblem problem,
                            IRandomNumberGenerator random,
                            int modelObservationInterval, Func<ObjectiveVector, double> objectiveValueSelector,
                            IDynamicRacingQualityTraceSink<TCandidate>? qualityTraceSink, long race, int contenderIndex,
                            DynamicRacingConfigurationOrigin origin)
        {
            Dispose();
            Scope = Scope.WithContext(context);
            performance = new ContenderPerformance(modelObservationInterval, objectiveValueSelector,
                qualityTraceSink, race, contenderIndex, contenderIndex == 0, origin, Candidate);
            LastState = initialState;
            running = CreateEnumerator(problem, random, initialState, CancellationToken.None);
        }

        private IEnumerator<TSearchState> CreateEnumerator(TProblem problem, IRandomNumberGenerator random,
                                                           TSearchState? initialState, CancellationToken ct)
        {
            // Evaluation observations finish in the environment that scored them. When iteration updates are
            // selected, apply pending changes only after this contender yields its completed generation.
            var observed = Scope.With(
                Aspect.Observe<TCandidate, TSearchSpace, TProblem>(evaluator, performance.Observe),
                problem.CreateIterationUpdateAspect(Algorithm));
            return observed.Resolve<TCandidate, TSearchSpace, TProblem, TSearchState>(Algorithm)
                           .Stream(problem, random, initialState, ct).GetEnumerator();
        }
    }

    private sealed class ContenderPerformance
    {
        private readonly int modelObservationInterval;
        private readonly Func<ObjectiveVector, double> objectiveValueSelector;
        private readonly IDynamicRacingQualityTraceSink<TCandidate>? qualityTraceSink;
        private readonly long race;
        private readonly int contenderIndex;
        private readonly bool isIncumbent;
        private readonly DynamicRacingConfigurationOrigin origin;
        private readonly MetaOptimizationGenotype configuration;
        private int nextModelObservationAt;

        public ContenderPerformance(int modelObservationInterval,
                                    Func<ObjectiveVector, double> objectiveValueSelector,
                                    IDynamicRacingQualityTraceSink<TCandidate>? qualityTraceSink,
                                    long race,
                                    int contenderIndex,
                                    bool isIncumbent,
                                    DynamicRacingConfigurationOrigin origin,
                                    MetaOptimizationGenotype configuration)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(modelObservationInterval);
            this.modelObservationInterval = modelObservationInterval;
            this.objectiveValueSelector = objectiveValueSelector;
            this.qualityTraceSink = qualityTraceSink;
            this.race = race;
            this.contenderIndex = contenderIndex;
            this.isIncumbent = isIncumbent;
            this.origin = origin;
            this.configuration = Copy(configuration);
            nextModelObservationAt = modelObservationInterval;
        }

        public int EvaluatedCandidateCount { get; private set; }
        public TCandidate? CurrentBestCandidate { get; private set; }
        public ObjectiveVector? CurrentBestObjectiveVector { get; private set; }
        public double? CurrentBestObjectiveValue { get; private set; }
        public int ModelObservationCount => CurveModel.ObservationCount;
        public OnlineWeibullCurveModel CurveModel { get; } = new();

        public void Observe(EvaluatorObservation<TCandidate, TSearchSpace, TProblem> observation)
        {
            var objectiveVectors = observation.ObjectiveVectors;
            var candidates = observation.Candidates;
            var problem = observation.Problem;
            EvaluatedCandidateCount += candidates.Count;
            if (objectiveVectors.Count == 0)
                return;

            var comparer = GetComparer(problem.Objective);
            var batchBestIndex = 0;
            for (var i = 1; i < objectiveVectors.Count; i++)
            {
                if (comparer.Compare(objectiveVectors[i], objectiveVectors[batchBestIndex]) < 0)
                    batchBestIndex = i;
            }

            var batchBest = objectiveVectors[batchBestIndex];
            if (CurrentBestObjectiveVector is null || comparer.Compare(batchBest, CurrentBestObjectiveVector) < 0)
            {
                CurrentBestCandidate = candidates[batchBestIndex];
                CurrentBestObjectiveVector = batchBest;
            }

            CurrentBestObjectiveValue = objectiveValueSelector(CurrentBestObjectiveVector);
            RecordModelObservationIfDue();
            RecordTracePoint(problem);
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

        private void RecordTracePoint(TProblem problem)
        {
            if (qualityTraceSink is null ||
                CurrentBestCandidate is not { } currentBestCandidate ||
                CurrentBestObjectiveVector is not { } currentBestObjective ||
                CurrentBestObjectiveValue is not { } currentBestObjectiveValue)
            {
                return;
            }

            qualityTraceSink.Add(new DynamicRacingQualityTracePoint<TCandidate>(
                race,
                problem.CurrentEpoch,
                contenderIndex,
                isIncumbent,
                origin,
                Copy(configuration),
                currentBestCandidate,
                currentBestObjective,
                currentBestObjectiveValue,
                problem.TotalEvaluations,
                EvaluatedCandidateCount));
        }

        private static MetaOptimizationGenotype Copy(MetaOptimizationGenotype candidate) =>
            new(RealVector.Create(candidate.Part1), IntegerVector.Create(candidate.Part2));
    }

    private static IComparer<ObjectiveVector> GetComparer(ObjectiveDirections objective) =>
        objective.TotalOrderComparer is NoTotalOrderComparer
            ? new LexicographicComparer(objective.Directions)
            : objective.TotalOrderComparer;
}
