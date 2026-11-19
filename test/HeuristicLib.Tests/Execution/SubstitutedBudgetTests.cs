using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;
using static HEAL.HeuristicLib.Tests.Execution.SubstitutionFixture;
using GaussianMutator = HEAL.HeuristicLib.Encodings.RealVectors.GaussianMutator;
using SinglePointCrossover = HEAL.HeuristicLib.Encodings.RealVectors.SinglePointCrossover;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Execution;

// Counting and budgets placed by substitution, with the fixture notation of the system spec: A makes three calls to M per
// state, B makes two calls and yields once. See plans/core-simplification-plan.md, D29, D30 and D31, and the S6 and S9
// tables of plans/operator-unification-system-spec.md.
public class SubstitutedBudgetTests
{
    [Fact]
    public void Budget_EqualsTheGraphWrittenByHand()
    {
        var (box, m) = NewMutator();
        var a = NewCaller("A", 3, 3, m, box);

        var limited = a.LimitedToInvocations(m, 5);

        var counter = limited.Counter;
        limited.ShouldBe(new OperatorCountBudgetAlgorithm<int, PopulationState<int>>(a with { Mutator = m.Counted(counter) }, counter, 5));
    }

    [Fact]
    public void Counting_EqualsTheGraphWrittenByHandAndKeepsTheReceiversType()
    {
        var (box, m) = NewMutator();
        var a = NewCaller("A", 3, 3, m, box);

        Caller counted = a.Counting(m, out var calls);

        counted.ShouldBe(a with { Mutator = m.Counted(calls) });
    }

    // S6, version 1: the inner budget restarts each turn.
    [Fact]
    public void BudgetAroundAInsideTheCycle_RestartsEachTurn()
    {
        var (box, m) = NewMutator();
        var cycle = Cycle(NewCaller("A", 3, 3, m, box).LimitedToInvocations(m, 5), NewCaller("B", 2, 1, m, box));

        Run(cycle);

        box.Yields.ShouldBe("A1 A2 B1 A1 A2 B1");
        box.Calls.ShouldBe(16);
    }

    // S6, version 2: one budget counts A's and B's calls across turns.
    [Fact]
    public void BudgetAroundTheCycle_CountsEveryCallBeneathIt()
    {
        var (box, m) = NewMutator();
        var cycle = Cycle(NewCaller("A", 3, 3, m, box), NewCaller("B", 2, 1, m, box));

        Run(cycle.LimitedToInvocations(m, 10));

        box.Yields.ShouldBe("A1 A2 A3 B1");
        box.Calls.ShouldBe(11);
    }

    // S6, version 3: both budgets count the same calls independently.
    [Fact]
    public void BudgetsInsideAndAroundTheCycle_CountIndependently()
    {
        var (box, m) = NewMutator();
        var cycle = Cycle(NewCaller("A", 3, 3, m, box).LimitedToInvocations(m, 5), NewCaller("B", 2, 1, m, box));

        Run(cycle.LimitedToInvocations(m, 10));

        box.Yields.ShouldBe("A1 A2 B1 A1");
        box.Calls.ShouldBe(11);
    }

    // S9, part 1: a budget stops between states.
    [Fact]
    public void Budget_StopsAtTheFirstStateAfterItsLimit()
    {
        var (box, m) = NewMutator();

        Run(NewCaller("A", 4, 3, m, box).LimitedToInvocations(m, 5));

        box.Yields.ShouldBe("A1 A2");
        box.Calls.ShouldBe(8);
    }

    // S9, part 2: a failed attempt is not an invocation.
    [Fact]
    public void Counting_DoesNotCountFailedAttempts()
    {
        var box = new Recording();
        IOperator<Evaluation, int> flaky = new M(box) { FailFirstCall = true };
        IOperator<Evaluation, int> retried = flaky.Retry(2);

        var counted = NewCaller("A", 3, 1, retried, box)
            .Counting(retried, out var onRetry)
            .Counting(flaky, out var onFlaky);
        var run = counted.CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        box.Calls.ShouldBe(4);
        run.Result(onFlaky).CurrentCount.ShouldBe(3);
        run.Result(onRetry).CurrentCount.ShouldBe(3);
    }

    [Fact]
    public void CandidateBudget_CountsCandidateWork()
    {
        var (box, m) = NewMutator();

        Run(NewCaller("A", 3, 3, m, box).LimitedToCandidates(m, 3));

        box.Yields.ShouldBe("A1");
    }

    [Fact]
    public void CandidateCounting_OfAnOperationWithoutCandidateWork_FailsWhenBuilt()
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        IOperator<Termination, RealVector> terminator = new AfterOperatorCountTerminator<RealVector>(new Counter(), 1);
        var ga = RealGa(problem) with { Terminator = terminator };

        var limited = ga.LimitedToCandidates(terminator, 1);

        Should.Throw<InvalidOperationException>(() => limited.CreateRun(problem, RandomNumberGenerator.Create(1)).Complete(cancellationToken: Ct))
            .Message.ShouldContain("Counting candidates");
    }

    // D29: a budget's shared counter does not stop another budget from sharing its own.
    [Fact]
    public void TwoBudgetsWithDistinctTargets_ResolveInOneScopeInEitherOrder()
    {
        var (box, m) = NewMutator();
        var (_, other) = NewMutator();
        var first = NewCaller("A", 1, 1, m, box).LimitedToInvocations(m, 1);
        var second = NewCaller("B", 1, 1, other, box).LimitedToInvocations(other, 1);

        ResolveBoth(first, second);
        ResolveBoth(second, first);

        static void ResolveBoth(IAlgorithm<int, PopulationState<int>> one, IAlgorithm<int, PopulationState<int>> two)
        {
            var scope = ResolutionScope.Create();
            scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(one, Problem).ShouldNotBeNull();
            scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(two, Problem).ShouldNotBeNull();
        }
    }

    [Fact]
    public void NestedBudgetsWithDistinctTargets_EachStopAtTheirOwnCount()
    {
        var (box, m) = NewMutator();
        var (_, second) = NewMutator();
        var a = NewCaller("A", 2, 3, m, box) with { Second = second };

        Run(a.LimitedToInvocations(second, 100).LimitedToInvocations(m, 4));
        box.Yields.ShouldBe("A1 A2");

        box.Reset();
        Run(a.LimitedToInvocations(m, 100).LimitedToInvocations(second, 2));
        box.Yields.ShouldBe("A1");
    }

    // D26: the budget's wrapper is built per scope around the shared executor.
    [Fact]
    public void SharedOperator_StillReceivesEachRequestersBudget()
    {
        var (box, m) = NewMutator();
        var cycle = Cycle(NewCaller("A", 3, 3, m, box).LimitedToInvocations(m, 5), NewCaller("B", 2, 1, m, box)).Sharing(m);

        Run(cycle);

        box.Yields.ShouldBe("A1 A2 B1 A1 A2 B1");
        box.Calls.ShouldBe(16);
        box.Builds.ShouldBe(1);
    }

    [Fact]
    public void SharedCache_SurvivesTurnsWhileABudgetOnItRestarts()
    {
        var (box, m) = NewMutator();
        var cached = m.Cached();
        var cycle = Cycle(NewCaller("A", 3, 3, cached, box).LimitedToInvocations(cached, 5), NewCaller("B", 2, 1, cached, box)).Sharing(cached);

        Run(cycle);

        box.Yields.ShouldBe("A1 A2 B1 A1 A2 B1");
        box.Calls.ShouldBe(2);
    }

    // D9: a target inside a shared configuration produces a private copy of what surrounds it, so this cache is not shared.
    [Fact]
    public void BudgetInsideASharedCache_GivesItsCallerAPrivateCopy()
    {
        var (box, m) = NewMutator();
        var cached = m.Cached();
        var cycle = Cycle(NewCaller("A", 3, 3, cached, box).LimitedToInvocations(m, 2), NewCaller("B", 2, 1, cached, box)).Sharing(cached);

        Run(cycle);

        box.Yields.ShouldBe("A1 A2 B1 A1 A2 B1");
        box.Calls.ShouldBe(5); // A's private cache misses candidates 1 and 2 in each turn, and the shared one misses candidate 1 once
    }

    // Around the sharing node, the entry follows the copy, so one cache stays shared and counts into the budget.
    [Fact]
    public void BudgetAroundTheSharingNode_ReachesInsideTheSharedCache()
    {
        var (box, m) = NewMutator();
        var cached = m.Cached();
        var shared = Cycle(NewCaller("A", 3, 3, cached, box), NewCaller("B", 2, 1, cached, box)).Sharing(cached);

        Run(shared.LimitedToInvocations(m, 2));

        box.Yields.ShouldBe("A1 A2");
        box.Builds.ShouldBe(1);
    }

    // D1: the budget holds a copy, so a direct request for the original neither counts nor shares that executor.
    [Fact]
    public void Budget_DoesNotReachADirectRequestForTheOriginalAlgorithm()
    {
        var (box, m) = NewMutator();
        var a = NewCaller("A", 3, 1, m, box);
        var scope = ResolutionScope.Create();

        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(a.LimitedToInvocations(m, 5), Problem);
        scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(a, Problem);

        box.Log.ShouldBe("build A build A");
    }

    // D26: one configuration performing two operations keeps one executor when only one operation is wrapped.
    [Fact]
    public void MultiOperationOperator_KeepsOneExecutorWhenOneOperationIsCounted()
    {
        var box = new Recording();
        var both = new BothOperations(box);
        var user = new BothUser(both, both, box);

        var counted = user.Counting((IOperator<Mutation, int>)both, out var mutations);
        var run = counted.CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        box.Builds.ShouldBe(1);
        box.Calls.ShouldBe(2);
        run.Result(mutations).CurrentCount.ShouldBe(1);
        counted.Evaluator.ShouldBeSameAs(both);
    }

    [Fact]
    public void MultiOperationOperator_KeepsOneExecutorAcrossTurnsUnderSharing()
    {
        var box = new Recording();
        var both = new BothOperations(box);
        var user = new BothUser(both, both, box);
        var cycle = (CycleAlgorithm.Create<int, PopulationState<int>>(user) with { MaximumCycles = 3 }).Sharing(both);

        var counted = cycle.Counting((IOperator<Mutation, int>)both, out var mutations);
        var run = counted.CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        box.Builds.ShouldBe(1);
        box.Calls.ShouldBe(6);
        run.Result(mutations).CurrentCount.ShouldBe(3);
        counted.Shared[0].ShouldBeSameAs(both);
    }

    // D30: the count spans the run across fresh executors, without a budget.
    [Fact]
    public void Counter_SpansTheRunAcrossFreshExecutors()
    {
        var (box, m) = NewMutator();
        var cycle = Cycle(NewCaller("A", 3, 1, m, box), NewCaller("B", 2, 1, m, box));

        var counted = cycle.Counting(m, out var calls);
        var run = counted.CreateRun(Problem, Rng);
        var readBeforeTheRun = run.Result(calls);
        run.Complete(cancellationToken: Ct);

        box.Builds.ShouldBe(4);
        readBeforeTheRun.CurrentCount.ShouldBe(10);
        run.Result(calls).ShouldBeSameAs(readBeforeTheRun);
    }

    [Fact]
    public void Counter_IsPerRun()
    {
        var (box, m) = NewMutator();
        var counted = NewCaller("A", 3, 1, m, box).Counting(m, out var calls);

        var first = counted.CreateRun(Problem, Rng);
        first.Complete(cancellationToken: Ct);
        var second = counted.CreateRun(Problem, Rng);
        second.Complete(cancellationToken: Ct);

        first.Result(calls).CurrentCount.ShouldBe(3);
        second.Result(calls).CurrentCount.ShouldBe(3);
        first.Result(calls).ShouldNotBeSameAs(second.Result(calls));
    }

    [Fact]
    public void Counter_StaysReadableAfterAFailedRun()
    {
        var box = new Recording();
        IOperator<Evaluation, int> failing = new M(box) { FailFirstCall = true };
        var run = NewCaller("A", 1, 1, failing, box).Counting(failing, out var calls).CreateRun(Problem, Rng);

        Should.Throw<RetryableOperationException>(() => run.Complete(cancellationToken: Ct));

        run.Result(calls).CurrentCount.ShouldBe(0);
    }

    [Fact]
    public void Terminator_ReadsTheCounterWhicheverIsBuiltFirst()
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var ga = RealGa(problem) with { MaximumGenerations = 50 };

        var counted = ga.Counting(ga.Mutator, out var calls);
        var terminated = counted with { Terminator = new AfterOperatorCountTerminator<RealVector>(calls, 3) };
        var run = terminated.CreateRun(problem, RandomNumberGenerator.Create(42));
        var states = run.Stream(cancellationToken: Ct).Count();

        states.ShouldBeLessThan(50);
        run.Result(calls).CurrentCount.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void CountingAndBudgets_DoNotChangeASeededRun()
    {
        var problem = new TestFunctionProblem(new SphereFunction(3));
        var ga = RealGa(problem) with { MaximumGenerations = 5 };

        var plain = States(ga, problem);

        States(ga.Counting(ga.Mutator, out _).Counting(ga.Evaluator, out _), problem).ShouldBe(plain);
        States(ga.LimitedToInvocations(ga.Mutator, 1_000_000), problem).ShouldBe(plain);
        States(ga.LimitedToCandidates(ga.Evaluator, 1_000_000), problem).ShouldBe(plain);
    }

    [Fact]
    public void AdaptiveMutator_KeepsItsStrengthControlThroughCounting()
    {
        var problem = new TestFunctionProblem(new SphereFunction(2));
        var mutator = new GaussianMutator(0.1, 0.1) as IOperator<Mutation, RealVector>;
        var strategy = new EvolutionStrategy<RealVector>
        {
            PopulationSize = 4,
            NumberOfChildren = 8,
            Strategy = EvolutionStrategyType.Plus,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Mutator = mutator,
            MaximumGenerations = 5
        };

        var plain = States(strategy, problem);

        States(strategy.Counting(mutator, out _), problem).ShouldBe(plain);
        States((strategy.LimitedToInvocations(mutator, 1_000_000) as IAlgorithm<RealVector, PopulationState<RealVector>>).Sharing(mutator), problem).ShouldBe(plain);
    }

    // D32 for the shortcuts: the routes the error names are enough for these compositions.
    [Fact]
    public void Counting_AfterABudget_FindsAHookThatIsStillInTheCopy()
    {
        var (box, m) = NewMutator();
        var (_, second) = NewMutator();
        var a = NewCaller("A", 2, 2, m, box) with { Second = second };

        var counted = a.LimitedToInvocations(m, 100).Counting(second, out var seconds);
        var run = counted.CreateRun(Problem, Rng);
        run.Complete(cancellationToken: Ct);

        run.Result(seconds).CurrentCount.ShouldBe(4);
    }

    [Fact]
    public void Counting_OfAWrapperWhoseChildAnEarlierCallChanged_IsAnErrorAndTheRoutesWork()
    {
        var (box, m) = NewMutator();
        IOperator<Evaluation, int> cached = m.Cached();
        var a = NewCaller("A", 3, 2, cached, box);

        Should.Throw<InvalidOperationException>(() => a.Counting(m, out _).Counting(cached, out _)).Message.ShouldContain("one Substitute call");

        Caller Expected(Counter inner, Counter outer) =>
            a with { Mutator = ((CachingEvaluator<int>)cached with { ChildOperator = m.Counted(inner) }).Counted(outer) };

        // Ancestors first.
        a.Counting(cached, out var outer).Counting(m, out var inner).ShouldBe(Expected(inner, outer));
        // Naming the target from the current configuration.
        var changed = a.Counting(m, out var first);
        changed.Counting(changed.Mutator, out var second).ShouldBe(Expected(first, second));
        // One call.
        var innerCall = new Counter();
        var outerCall = new Counter();
        a.Substitute(m.Invocations.Counting(innerCall), cached.Invocations.Counting(outerCall)).ShouldBe(Expected(innerCall, outerCall));
    }

    // A read is not a resolution, so looking at a result before the run cannot break the run.
    [Fact]
    public void ReadingABudgetsCounterBeforeTheRun_LeavesTheBudgetWorking()
    {
        var (box, m) = NewMutator();
        var limited = NewCaller("A", 3, 5, m, box).LimitedToInvocations(m, 5);

        var run = limited.CreateRun(Problem, Rng);
        var count = run.Result(limited.Counter);
        run.Complete(cancellationToken: Ct);

        count.CurrentCount.ShouldBe(6);
    }

    // A counter is a link: two counters differ, so a graph comparison tells them apart.
    [Fact]
    public void Counters_AreDifferentButCopiesKeepTheSameOne()
    {
        var (box, m) = NewMutator();
        var a = NewCaller("A", 3, 1, m, box);

        m.Counted(new Counter()).ShouldNotBe(m.Counted(new Counter()));
        a.Counting(m, out _).ShouldNotBe(a.Counting(m, out _));

        var counted = a.Counting(m, out _);
        var copy = counted with { States = 2 };
        copy.Mutator.ShouldBeSameAs(counted.Mutator);
        (copy with { States = 1 }).ShouldBe(counted);
    }

    // Copies of a configuration share the counter reference, as the cases of a grid do, and each run counts on its own.
    [Fact]
    public void Counter_CopiedConfigurationsCountSeparatelyInTheirRuns()
    {
        var (box, m) = NewMutator();
        var counted = NewCaller("A", 3, 1, m, box).Counting(m, out var calls);
        var copy = counted with { States = 2 };

        var first = counted.CreateRun(Problem, Rng);
        var second = copy.CreateRun(Problem, Rng);
        first.Complete(cancellationToken: Ct);
        second.Complete(cancellationToken: Ct);

        first.Result(calls).CurrentCount.ShouldBe(3);
        second.Result(calls).CurrentCount.ShouldBe(6);
    }

    // A concretely typed variable can be wrapped through the operator or the algorithm interface.
    [Fact]
    public void ConcretelyTypedTarget_CanBeWrapped()
    {
        var (box, m) = NewMutator();
        var concrete = new CachingEvaluator<int>(m);
        var caller = NewCaller("A", 3, 1, concrete, box);
        var counter = new Counter();

        caller.Substitute(concrete, node => node.Counted(counter)).Mutator.ShouldBe(concrete.Counted(counter));
        caller.Substitute(Substitution.Of(concrete, node => node.Counted(counter))).Mutator.ShouldBe(concrete.Counted(counter));
        caller.Substitute(concrete, node => node with { }).ShouldBe(caller);

        var cycle = CycleAlgorithm.Create<int, PopulationState<int>>(caller);
        cycle.Substitute(caller, node => node.Named("turn")).Algorithms[0].ShouldBe(((IAlgorithm<int, PopulationState<int>>)caller).Named("turn"));
    }

    private static List<string> States<TSearchState>(IAlgorithm<RealVector, TSearchState> algorithm, TestFunctionProblem problem)
        where TSearchState : PopulationState<RealVector>
    {
        var stream = algorithm.RunStreaming(problem, RandomNumberGenerator.Create(42), ct: Ct);
        return [.. Enumerable.Select(stream,
            static state => string.Join(";", state.Population.EvaluatedCandidates.Select(static member => $"{member.Candidate}|{member.ObjectiveVector}")))];
    }

    private static GeneticAlgorithm<RealVector> RealGa(TestFunctionProblem problem) => new()
    {
        PopulationSize = 5,
        Creator = new UniformDistributedCreator(problem.SearchSpace),
        Crossover = new SinglePointCrossover(),
        Mutator = new GaussianMutator(0.1, 0.1),
        MutationRate = 1.0,
        Selector = RandomSelector.For(problem),
        Elites = 0
    };

    // One configuration that performs evaluation and termination with one executor, standing in for a consumer's multi-operation node.
    private sealed record BothOperations(Recording Box) : IOperator<Evaluation, int>, IOperator<Mutation, int>
    {
        public IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope)
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            Box.Builds++;
            return new Executor<TSearchSpace, TProblem>(Box, scope.Problem);
        }

        private sealed class Executor<TSearchSpace, TProblem>(Recording box, TProblem problem)
            : IEvaluationExecutor<int>, IMutationExecutor<int>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
            {
                box.Calls++;
                return problem.Evaluate(candidates, random).ToArray();
            }

            public IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random)
            {
                box.Calls++;
                return parents;
            }
        }
    }

    private sealed record BothUser(IOperator<Evaluation, int> Evaluator, IOperator<Mutation, int> Mutator, Recording Box)
        : Algorithm<BothUser, int, PopulationState<int>>
    {
        public static Parameter<BothUser, IOperator<Evaluation, int>> EvaluatorParameter { get; } =
            new(nameof(Evaluator), static user => user.Evaluator, static (user, value) => user with { Evaluator = value });

        public static Parameter<BothUser, IOperator<Mutation, int>> MutatorParameter { get; } =
            new(nameof(Mutator), static user => user.Mutator, static (user, value) => user with { Mutator = value });

        public static IReadOnlyList<IParameter> Parameters { get; } = [EvaluatorParameter, MutatorParameter];

        public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor(scope.Resolve(Evaluator), scope.Resolve(Mutator));

        private sealed class Executor(IEvaluationExecutor<int> evaluator, IMutationExecutor<int> mutator)
            : AlgorithmExecutor<PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                var offspring = mutator.Mutate([1], random);
                var quality = evaluator.Evaluate(offspring, random).Single();
                yield return Population.From([offspring[0].ToEvaluated(quality)]).ToPopulationState();
                await Task.CompletedTask;
            }
        }
    }
}
