using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Execution;

// A scope holds one executor per configuration reference, and sharing lets child scopes, such as Cycle turns, use the
// executor of the scope that shares it. See plans/operator-state-lifetimes.md.
public class ConfigurationSharingTests
{
    // Shared fixture notation from the system spec: A makes three calls to M per state, B makes two calls and yields once.

    [Fact]
    public void WithoutSharing_EveryTurnConstructsItsOwnExecutor()
    {
        var (box, m) = Mutator();
        var cycle = Cycle(NewCaller("A", 3, 1, m, box), NewCaller("B", 2, 1, m, box));

        Run(cycle);

        box.Builds.ShouldBe(4);
        box.LastPrivateCount.ShouldBe(2); // B's last turn, counted from zero by its own executor
    }

    [Fact]
    public void SharedConfiguration_IsConstructedOnceAndKeepsItsPrivateFieldsAcrossTurns()
    {
        var (box, m) = Mutator();
        var cycle = Cycle(NewCaller("A", 3, 1, m, box), NewCaller("B", 2, 1, m, box)).Sharing(m);

        Run(cycle);

        box.Builds.ShouldBe(1);
        box.LastPrivateCount.ShouldBe(10); // 3 + 2 + 3 + 2 calls through one executor
    }

    [Fact]
    public void EveryRun_GetsItsOwnSharedExecutor()
    {
        var (box, m) = Mutator();
        var cycle = Cycle(NewCaller("A", 3, 1, m, box), NewCaller("B", 2, 1, m, box)).Sharing(m);

        Run(cycle);
        box.LastPrivateCount.ShouldBe(10);
        Run(cycle);

        box.Builds.ShouldBe(2);
        box.LastPrivateCount.ShouldBe(10); // not 20: the second run started from a fresh executor
    }

    // The use case from the review: keep the cache across turns, restart the per-turn budget.
    [Fact]
    public void SharedCache_SurvivesTurnsWhileTheInnerBudgetRestarts()
    {
        var (box, m) = Mutator();
        var cached = m.Cached();
        var a = NewCaller("A", 3, 3, cached, box);
        var b = NewCaller("B", 2, 1, cached, box);

        var unshared = Cycle(a.LimitedToInvocations(cached, 5), b);
        Run(unshared);
        var unsharedEvaluations = box.Calls;

        box.Reset();
        var shared = Cycle(a.LimitedToInvocations(cached, 5), b).Sharing(cached);
        Run(shared);

        box.Yields.ShouldBe("A1 A2 B1 A1 A2 B1"); // the inner budget still stops A after two states, every turn
        unsharedEvaluations.ShouldBe(6);           // each cache starts empty: A 1,2 | B 1 | A 1,2 | B 1
        box.Calls.ShouldBe(2);                     // one cache: only candidates 1 and 2 are ever evaluated
    }

    [Fact]
    public void SharingAChildAlgorithm_KeepsOnlyThatChildAcrossTurns()
    {
        var (box, m) = Mutator();
        var limitedA = NewCaller("A", 3, 3, m, box).LimitedToInvocations(m, 5);
        var b = NewCaller("B", 2, 1, m, box);
        Run(Cycle(limitedA, b).Sharing(limitedA, b));
        var bothShared = box.Log;

        box.Reset();
        Run(Cycle(limitedA, b).Sharing(limitedA));

        bothShared.ShouldBe("build A A1 A2 build B B1 A1 B1");              // both are kept, A's budget is spent
        box.Log.ShouldBe("build A A1 A2 build B B1 A1 build B B1");         // only A is kept, B is rebuilt
    }

    [Fact]
    public void WithinOneScope_EveryUseOfAReferenceSharesOneExecutor()
    {
        var (box, m) = Mutator();
        var twoSlots = NewCaller("A", 3, 1, m, box) with { Second = m };

        Run(Cycle(twoSlots, NewCaller("B", 2, 1, m, box)));

        box.Builds.ShouldBe(4); // A's two slots share one M within each A turn; B and the next A turn get new ones
    }

    // A wrapper such as a budget resolves its child in its own scope, so it opens no sharing boundary. The budget holds a
    // copy of A with its counter in place, so the direct request names that copy.
    [Fact]
    public void AlgorithmWrappers_ResolveInTheScopeOfWhatTheyWrap()
    {
        var (box, m) = Mutator();
        var budget = NewCaller("A", 3, 1, m, box).LimitedToInvocations(m, 5);
        var scope = ResolutionScope.Create();

        var limited = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(budget, Problem);
        var bare = scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(budget.Algorithm, Problem);

        limited.ShouldNotBeSameAs(bare);
        box.Log.ShouldBe("build A"); // the budget and the direct request use one A executor
        box.Builds.ShouldBe(1);
    }

    [Fact]
    public void NestedSharing_TheNearestSharingPointWins()
    {
        var (box, m) = Mutator();
        var cycle = Cycle(NewCaller("A", 3, 1, m, box).Sharing(m), NewCaller("B", 2, 1, m, box)).Sharing(m);

        Run(cycle);

        box.Builds.ShouldBe(3); // one per A turn from the inner sharing point, one for both B turns from the outer one
    }

    [Fact]
    public void CompositeOperator_ChildSlotsShareOneExecutorInTheirScope()
    {
        var (box, m) = Mutator();
        var bothSlots = new TwoChildEvaluator(m, m);

        Run(Cycle(NewCaller("A", 3, 1, bothSlots, box), NewCaller("B", 2, 1, m, box)));

        box.Builds.ShouldBe(4); // one M per A turn for both slots, one per B turn
    }

    [Fact]
    public void CycleSharing_ReachesTheSlotsOfACompositeOperatorInEveryTurn()
    {
        var (box, m) = Mutator();
        var bothSlots = new TwoChildEvaluator(m, m);

        Run(Cycle(NewCaller("A", 3, 1, bothSlots, box), NewCaller("B", 2, 1, m, box)).Sharing(m));

        box.Builds.ShouldBe(1);
        box.LastPrivateCount.ShouldBe(16); // A's turns call M twice per call, B's once: (6 + 2) * 2
    }

    [Fact]
    public void AdviceAboveTheSharingPoint_ReachesInsideTheSharedExecutor()
    {
        var (box, m) = Mutator();
        var cached = m.Cached();
        var cycle = Cycle(NewCaller("A", 3, 1, cached, box), NewCaller("B", 2, 1, cached, box)).Sharing(cached);
        var counter = new Counter();

        var run = cycle.CreateRun(Problem, Rng, [m.Invocations.Counting(counter)]);
        run.Complete();

        run.Result(counter).CurrentCount.ShouldBe(1); // the inner evaluator is reached once: candidate 1 is cached for every later call
    }

    // Compatible static views of one configuration/problem pair reuse its executor.
    [Fact]
    public void Configuration_RequestedThroughCompatibleProblemTypes_ReusesItsExecutor()
    {
        var (_, m) = Mutator();
        var limited = m.LimitEvaluations(2);
        var scope = ResolutionScope.Create();
        var executor = scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(limited, Problem);

        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>(limited, Problem).ShouldBeSameAs(executor);
        scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(limited, Problem).ShouldBeSameAs(executor);
    }

    private static readonly FuncProblem<int, DummySearchSpace<int>> Problem = FuncProblem.Create(static (int x) => x, DummySearchSpace<int>.Instance, SingleObjective.Minimize);
    private static IRandomNumberGenerator Rng => RandomNumberGenerator.Create(1);

    private static void Run(IAlgorithm<int, PopulationState<int>> algorithm) => algorithm.CreateRun(Problem, Rng).Complete();

    private static CycleAlgorithm<int, PopulationState<int>> Cycle(
        IAlgorithm<int, PopulationState<int>> first, IAlgorithm<int, PopulationState<int>> second) =>
        CycleAlgorithm.Create(first, second) with { MaximumCycles = 2 };

    private static (Box, M) Mutator()
    {
        var box = new Box();
        return (box, new M(box));
    }

    private static Caller NewCaller(string label, int callsPerState, int states, IOperator<Evaluation, int> mutator, Box box) =>
        new(label, callsPerState, states, mutator, box);

    private sealed class Box
    {
        public int Calls;
        public int Builds;
        public int LastPrivateCount;
        public readonly List<string> Entries = [];
        public string Log => string.Join(" ", Entries);
        public string Yields => string.Join(" ", Entries.Where(entry => !entry.StartsWith("build", StringComparison.Ordinal)));

        public void Reset()
        {
            Calls = Builds = LastPrivateCount = 0;
            Entries.Clear();
        }
    }

    // Stands in for the mutator M. Its executor keeps a private call count, like an adaptive mutation strength would.
    private sealed record M(Box Box) : Evaluator<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>>
    {
        protected override IEvaluationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, TRunSearchState> scope)
        {
            Box.Builds++;
            return new Executor(Box, scope.Problem);
        }

        private sealed class Executor(Box box, IProblem<int, DummySearchSpace<int>> problem) : EvaluationExecutor<int>
        {
            private int calls;

            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                box.Calls++;
                box.LastPrivateCount = ++calls;
                return problem.Evaluate(candidates, random).ToArray();
            }
        }
    }

    // Produces up to States states. Each state costs CallsPerState calls to Mutator with the candidate equal to the state
    // number, plus the same calls to Second when it is set.
    private sealed record Caller(string Label, int CallsPerState, int States, IOperator<Evaluation, int> Mutator, Box Box)
        : Algorithm<Caller, int, PopulationState<int>>
    {
        public IOperator<Evaluation, int>? Second { get; init; }

        public static Parameter<Caller, IOperator<Evaluation, int>> MutatorParameter { get; } =
            new(nameof(Mutator), static algorithm => algorithm.Mutator, static (algorithm, value) => algorithm with { Mutator = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), MutatorParameter];

        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope)
        {
            Box.Entries.Add($"build {Label}");
            return new Executor(this, scope.Resolve(Mutator), Second is null ? null : scope.Resolve(Second));
        }

        private sealed class Executor(Caller caller, IEvaluationExecutor<int> mutator,
            IEvaluationExecutor<int>? second)
            : AlgorithmExecutor<PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                for (var state = 1; state <= caller.States; state++)
                {
                    ObjectiveVector quality = default!;
                    for (var call = 0; call < caller.CallsPerState; call++)
                    {
                        quality = mutator.Evaluate([state], random).Single();
                        second?.Evaluate([state], random);
                    }
                    caller.Box.Entries.Add($"{caller.Label}{state}");
                    yield return Population.From([state.ToEvaluated(quality)]).ToPopulationState();
                }
                await Task.CompletedTask;
            }
        }
    }

    // A composite operator with two evaluator slots. Each call evaluates through both and returns the second result.
    private sealed record TwoChildEvaluator(IOperator<Evaluation, int> First, IOperator<Evaluation, int> Second) : Operator<Evaluation, int>
    {
        private static Parameter<TwoChildEvaluator, IOperator<Evaluation, int>> FirstParameter { get; } =
            new(nameof(First), static evaluator => evaluator.First, static (evaluator, value) => evaluator with { First = value });

        private static Parameter<TwoChildEvaluator, IOperator<Evaluation, int>> SecondParameter { get; } =
            new(nameof(Second), static evaluator => evaluator.Second, static (evaluator, value) => evaluator with { Second = value });

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), FirstParameter, SecondParameter];

        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
        {
            var first = scope.Resolve(First);
            var second = scope.Resolve(Second);
            return new Executor(first, second);
        }

        private sealed class Executor(IEvaluationExecutor<int> first, IEvaluationExecutor<int> second)
            : EvaluationExecutor<int>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                first.Evaluate(candidates, random);
                return second.Evaluate(candidates, random);
            }
        }
    }
}
