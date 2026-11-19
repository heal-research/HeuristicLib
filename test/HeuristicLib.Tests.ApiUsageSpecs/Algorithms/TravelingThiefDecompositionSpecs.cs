using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Encodings.Composite;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
using ThiefCandidate = HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.Permutations.Permutation, HEAL.HeuristicLib.Encodings.BoolVectors.BoolVector>;
using ThiefSpace = HEAL.HeuristicLib.Encodings.Composite.CompositeSearchSpace<HEAL.HeuristicLib.Encodings.Permutations.Permutation, HEAL.HeuristicLib.Encodings.Permutations.PermutationSearchSpace, HEAL.HeuristicLib.Encodings.BoolVectors.BoolVector, HEAL.HeuristicLib.Encodings.BoolVectors.BoolVectorSearchSpace>;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Algorithms;

public sealed class TravelingThiefDecompositionSpecs
{
    [Fact]
    public void OneAlgorithm_BindsChildrenToDistinctSubproblemsSpacesAndCandidates()
    {
        var problem = new TravelingThiefProblem();
        var algorithm = new Decomposition(
            new BestOf<Permutation, PermutationSearchSpace, TourProblem>(
                [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]]),
            new BestOf<BoolVector, BoolVectorSearchSpace, PackingProblem>(
                [[false, false], [true, false], [false, true], [true, true]]));

        var states = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(1), ct: TestContext.Current.CancellationToken).ToArray();

        states.Length.ShouldBe(3);
        states.ShouldAllBe(state => problem.SearchSpace.Contains(new ThiefCandidate(state.Tour, state.Packing)));
        var scores = states.Select(state => TravelingThiefProblem.Score(state.Tour, state.Packing)).ToArray();
        scores.ShouldAllBe(score => double.IsFinite(score));
        scores.ShouldBe(scores.Order().ToArray());
    }

    // Three cities and two items. Packing affects speed; the tour affects carrying distance.
    private sealed class TravelingThiefProblem : Problem<TravelingThiefProblem, ThiefCandidate, ThiefSpace>
    {
        public TravelingThiefProblem() : base(SingleObjective.Maximize, new ThiefSpace(new(3), new(2))) { }
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<ThiefCandidate> candidates, IRandomNumberGenerator random) =>
            candidates.Select(candidate => new ObjectiveVector(Score(candidate.Part1, candidate.Part2))).ToArray();

        public static double Score(Permutation tour, BoolVector packing)
        {
            var weight = (packing[0] ? 1 : 0) + (packing[1] ? 2 : 0);
            if (weight > 2)
                return double.NegativeInfinity;
            var profit = (packing[0] ? 3 : 0) + (packing[1] ? 5 : 0);
            var distance = Distance(0, tour[0]) + Distance(tour[0], tour[1]) + Distance(tour[1], tour[2]) + Distance(tour[2], 0);
            var carryingDistance = packing[1] && tour[0] == 2 ? Distance(2, tour[1]) : 0;
            return profit - distance / (1.0 - 0.15 * weight) - carryingDistance;
        }

        private static double Distance(int a, int b) => a == b ? 0 : (a + b) switch { 1 => 1, 2 => 2, _ => 2.5 };
    }

    private sealed class TourProblem : SingleSolutionProblem<TourProblem, Permutation, PermutationSearchSpace>
    {
        public TourProblem() : base(SingleObjective.Maximize, new PermutationSearchSpace(3)) { }
        public BoolVector Packing { get; set; } = [false, false];
        public override ObjectiveVector Evaluate(Permutation candidate, IRandomNumberGenerator random) => new(TravelingThiefProblem.Score(candidate, Packing));
    }

    private sealed class PackingProblem : SingleSolutionProblem<PackingProblem, BoolVector, BoolVectorSearchSpace>
    {
        public PackingProblem() : base(SingleObjective.Maximize, new BoolVectorSearchSpace(2)) { }
        public Permutation Tour { get; set; } = [0, 1, 2];
        public override ObjectiveVector Evaluate(BoolVector candidate, IRandomNumberGenerator random) => new(TravelingThiefProblem.Score(Tour, candidate));
    }

    // The typed construction hook supplies the problem. Invocation needs only random, initial state and cancellation.
    private sealed record BestOf<TCandidate, TSearchSpace, TProblem>(ValueArray<TCandidate> Candidates)
        : Algorithm<BestOf<TCandidate, TSearchSpace, TProblem>, TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
    {
        protected override IAlgorithmExecutor<SingleSolutionState<TCandidate>> CreateExecutor(ResolutionScope<TCandidate, TSearchSpace, TProblem, SingleSolutionState<TCandidate>> scope) =>
            new Executor(Candidates.AsImmutableArray(), scope.Problem);

        private sealed class Executor(ImmutableArray<TCandidate> candidates, TProblem problem) : AlgorithmExecutor<SingleSolutionState<TCandidate>>
        {
            public override async IAsyncEnumerable<SingleSolutionState<TCandidate>> RunStreamingAsync(IRandomNumberGenerator random, SingleSolutionState<TCandidate>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();
                var evaluated = candidates.Zip(problem.Evaluate(candidates, random), EvaluatedCandidate.From).ToArray();
                var best = evaluated.MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!;
                yield return SingleSolutionState.From(best.Candidate, best.ObjectiveVector);
                await Task.CompletedTask;
            }
        }
    }

    private sealed record ThiefState(Permutation Tour, BoolVector Packing) : ISearchState;

    private sealed record Decomposition(IAlgorithm<Permutation, SingleSolutionState<Permutation>> Tours, IAlgorithm<BoolVector, SingleSolutionState<BoolVector>> Packings)
        : Algorithm<Decomposition, ThiefCandidate, ThiefSpace, TravelingThiefProblem, ThiefState>
    {
        private static readonly Parameter<Decomposition, IAlgorithm<Permutation, SingleSolutionState<Permutation>>> ToursParameter =
            new(nameof(Tours), static algorithm => algorithm.Tours, static (algorithm, value) => algorithm with { Tours = value })
            { ChildExecution = static _ => ExecutionSignature.For<PermutationSearchSpace, TourProblem, SingleSolutionState<Permutation>>() };
        private static readonly Parameter<Decomposition, IAlgorithm<BoolVector, SingleSolutionState<BoolVector>>> PackingsParameter =
            new(nameof(Packings), static algorithm => algorithm.Packings, static (algorithm, value) => algorithm with { Packings = value })
            { ChildExecution = static _ => ExecutionSignature.For<BoolVectorSearchSpace, PackingProblem, SingleSolutionState<BoolVector>>() };

        public override IReadOnlyList<IParameter> GetParameters() => [ToursParameter, PackingsParameter];

        protected override IAlgorithmExecutor<ThiefState> CreateExecutor(ResolutionScope<ThiefCandidate, ThiefSpace, TravelingThiefProblem, ThiefState> scope)
        {
            var tourProblem = new TourProblem();
            var packingProblem = new PackingProblem();
            var tours = scope.For<Permutation, PermutationSearchSpace, TourProblem, SingleSolutionState<Permutation>>(tourProblem).Resolve(Tours);
            var packings = scope.For<BoolVector, BoolVectorSearchSpace, PackingProblem, SingleSolutionState<BoolVector>>(packingProblem).Resolve(Packings);
            return new Executor(tourProblem, packingProblem, tours, packings);
        }

        private sealed class Executor(TourProblem tourProblem, PackingProblem packingProblem, IAlgorithmExecutor<SingleSolutionState<Permutation>> tours, IAlgorithmExecutor<SingleSolutionState<BoolVector>> packings) : AlgorithmExecutor<ThiefState>
        {
            public override async IAsyncEnumerable<ThiefState> RunStreamingAsync(IRandomNumberGenerator random, ThiefState? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var turn = 0; turn < 3; turn++)
                {
                    packingProblem.Tour = tours.RunToCompletion(random, ct: ct).EvaluatedCandidate.Candidate;
                    tourProblem.Packing = packings.RunToCompletion(random, ct: ct).EvaluatedCandidate.Candidate;
                    yield return new ThiefState(packingProblem.Tour, tourProblem.Packing);
                }
                await Task.CompletedTask;
            }
        }
    }
}
