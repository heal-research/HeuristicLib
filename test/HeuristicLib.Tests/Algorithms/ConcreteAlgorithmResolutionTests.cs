using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public sealed class ConcreteAlgorithmResolutionTests
{
    private static readonly IProblem<int, DummySearchSpace<int>> Problem =
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    [Theory]
    [InlineData(nameof(HillClimber<int>))]
    [InlineData(nameof(GeneticAlgorithm<int>))]
    [InlineData(nameof(EvolutionStrategy<int>))]
    [InlineData(nameof(NSGA2<int>))]
    public async Task Rebinding_ObservesContextualChildrenAndPreservesCreatorState(string kind)
    {
        var creatorPreparations = 0;
        var interceptorPreparations = 0;
        IOperator<Creation, int> creator = new CounterCreator(() => creatorPreparations++);
        var (algorithm, countInterceptions) = CreateAlgorithm(kind, creator, () => interceptorPreparations++);
        var parent = ResolutionScope.Create();
        (await Next(parent, algorithm)).ShouldBe(1);

        var observedCreations = new CountAccumulator();
        var observedInterceptions = new CountAccumulator();
        var child = parent.CreateChildScope(Aspect.CountCalls(creator, observedCreations),
            countInterceptions(observedInterceptions));

        (await Next(child, algorithm)).ShouldBe(2);
        observedCreations.CurrentCount.ShouldBe(1);
        observedInterceptions.CurrentCount.ShouldBe(1);
        (await Next(parent, algorithm)).ShouldBe(3);
        observedCreations.CurrentCount.ShouldBe(1);
        observedInterceptions.CurrentCount.ShouldBe(1);
        (await Next(child, algorithm)).ShouldBe(4);
        observedCreations.CurrentCount.ShouldBe(2);
        observedInterceptions.CurrentCount.ShouldBe(2);
        creatorPreparations.ShouldBe(1);
        interceptorPreparations.ShouldBe(1);

        (await Next(ResolutionScope.Create(), algorithm)).ShouldBe(1);
        creatorPreparations.ShouldBe(2);
        interceptorPreparations.ShouldBe(2);
        observedCreations.CurrentCount.ShouldBe(2);
        observedInterceptions.CurrentCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(nameof(HillClimber<int>))]
    [InlineData(nameof(GeneticAlgorithm<int>))]
    [InlineData(nameof(EvolutionStrategy<int>))]
    [InlineData(nameof(NSGA2<int>))]
    public async Task PausedIterator_KeepsItsInterceptorAndPreviousStateAcrossRebinding(string kind)
    {
        var creator = new CounterCreator(static () => { });
        var (algorithm, countInterceptions) = CreateAlgorithm(kind, creator, static () => { }, mutationRate: 1);
        var parent = ResolutionScope.Create();
        await using var paused = Run(parent, algorithm).GetAsyncEnumerator();
        (await paused.MoveNextAsync()).ShouldBeTrue();
        Candidate(paused.Current).ShouldBe(1);

        var observedInterceptions = new CountAccumulator();
        var child = parent.CreateChildScope(countInterceptions(observedInterceptions));
        await using var observed = Run(child, algorithm).GetAsyncEnumerator();
        (await observed.MoveNextAsync()).ShouldBeTrue();
        Candidate(observed.Current).ShouldBe(2);
        observedInterceptions.CurrentCount.ShouldBe(1);

        (await paused.MoveNextAsync()).ShouldBeTrue();
        Candidate(paused.Current).ShouldBe(0);
        observedInterceptions.CurrentCount.ShouldBe(1);
        (await observed.MoveNextAsync()).ShouldBeTrue();
        Candidate(observed.Current).ShouldBe(1);
        observedInterceptions.CurrentCount.ShouldBe(2);
    }

    // Each algorithm gets an interceptor over its own search state, together with a way to count that interceptor's calls.
    private static (IAlgorithm<int> Algorithm, Func<CountAccumulator, Aspect> CountInterceptions) CreateAlgorithm(string kind, IOperator<Creation, int> creator,
        Action prepareInterceptor, double mutationRate = 0.5)
    {
        if (kind == nameof(HillClimber<int>))
        {
            var solutionInterceptor = new IdentityInterceptor<SingleSolutionState<int>>(prepareInterceptor);
            var hillClimber = new HillClimber<int>
            {
                Creator = creator,
                Mutator = new ImprovingMutator(),
                Interceptor = solutionInterceptor,
                MaxNeighbors = 1,
                BatchSize = 1,
                Direction = LocalSearchDirection.FirstImprovement
            };
            return (hillClimber, counter => Aspect.CountCalls(solutionInterceptor, counter));
        }

        var interceptor = new IdentityInterceptor<PopulationState<int>>(prepareInterceptor);
        IAlgorithm<int> algorithm = kind switch
        {
            nameof(GeneticAlgorithm<int>) => new GeneticAlgorithm<int>
            {
                Creator = creator,
                Crossover = SelectFirstParentCrossover<int>.Instance,
                Mutator = new ImprovingMutator(),
                Interceptor = interceptor,
                Selector = new RandomSelector<int>(),
                PopulationSize = 1,
                Elites = 0,
                MaximumGenerations = 3,
                MutationRate = mutationRate
            },
            nameof(EvolutionStrategy<int>) => new EvolutionStrategy<int>
            {
                Creator = creator,
                Mutator = new ImprovingMutator(),
                Interceptor = interceptor,
                Selector = new RandomSelector<int>(),
                PopulationSize = 1,
                NumberOfChildren = 1,
                MaximumGenerations = 3
            },
            nameof(NSGA2<int>) => new NSGA2<int>
            {
                Creator = creator,
                Crossover = SelectFirstParentCrossover<int>.Instance,
                Mutator = new ImprovingMutator(),
                Interceptor = interceptor,
                Selector = new RandomSelector<int>(),
                Replacer = new ElitismReplacer<int>(0),
                PopulationSize = 1,
                MaximumGenerations = 3,
                MutationRate = mutationRate
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return (algorithm, counter => Aspect.CountCalls(interceptor, counter));
    }

    private static IAsyncEnumerable<PopulationState<int>> Run(ResolutionScope scope, IAlgorithm<int> algorithm) =>
        algorithm is HillClimber<int>
            ? scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, SingleSolutionState<int>>(algorithm)
                .RunStreamingAsync(Problem, RandomNumberGenerator.Create(7), ct: TestContext.Current.CancellationToken)
            : scope.Resolve<int, DummySearchSpace<int>, IProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm)
                .RunStreamingAsync(Problem, RandomNumberGenerator.Create(7), ct: TestContext.Current.CancellationToken);

    private static async Task<int> Next(ResolutionScope scope, IAlgorithm<int> algorithm)
    {
        await using var iterator = Run(scope, algorithm).GetAsyncEnumerator();
        (await iterator.MoveNextAsync()).ShouldBeTrue();
        return Candidate(iterator.Current);
    }

    private static int Candidate(PopulationState<int> state) => state.Population.EvaluatedCandidates.Single().Candidate;

    private sealed class CounterState
    {
        public int Calls { get; set; }
    }

    private sealed record CounterCreator(Action Prepare) : StatefulCreator<int, CounterState>
    {
        protected override CounterState CreateScopedState()
        {
            Prepare();
            return new CounterState();
        }

        protected override IReadOnlyList<int> Create(int count, CounterState state, IRandomNumberGenerator random) =>
            Enumerable.Repeat(++state.Calls, count).ToArray();
    }

    private sealed record ImprovingMutator : SingleCandidateMutator<int>
    {
        public override int MutateCandidate(int parent, IRandomNumberGenerator random) => parent - 1;
    }

    private sealed record IdentityInterceptor<TSearchState>(Action Prepare) : Operator<Interception<TSearchState>, int>
        where TSearchState : class, ISearchState
    {
        // Creating the scoped state is the once-per-owner preparation the test counts.
        protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TRunSearchState>(ResolutionScope<int, TSearchSpace, TProblem, TRunSearchState> scope)
        {
            scope.GetScopedState(this, () =>
            {
                Prepare();
                return new Preparation();
            });
            return new Executor<TSearchSpace, TProblem>();
        }

        private sealed class Preparation;

        private sealed class Executor<TSearchSpace, TProblem> : IInterceptionExecutor<int, TSearchSpace, TProblem, TSearchState>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => currentState;
        }
    }
}
