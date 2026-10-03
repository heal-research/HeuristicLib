using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms;

public sealed class GeneticAlgorithmFactoryTests
{
    private static readonly IProblem<int, UnrestrictedSearchSpace<int>> Problem =
        FuncProblem.Create(static (int candidate) => candidate, UnrestrictedSearchSpace<int>.Instance, SingleObjective.Minimize);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Rebinding_ObservesContextualChildrenAndPreservesCreatorState(bool alps)
    {
        var creatorPreparations = 0;
        var interceptorPreparations = 0;
        ICreator<int> creator = new CounterCreator(() => creatorPreparations++);
        IInterceptor<int> interceptor = new IdentityInterceptor(() => interceptorPreparations++);
        var algorithm = CreateAlgorithm(alps, creator, interceptor);
        var parent = ResolutionScope.Create();
        (await Next(parent, algorithm)).ShouldBe(1);

        var observedCreations = new CountAccumulator();
        var observedInterceptions = new CountAccumulator();
        var child = parent.CreateChildScope(builder =>
        {
            builder.Wrap(creator, original => original.CountCalls(observedCreations));
            builder.Wrap(interceptor, original => original.CountCalls(observedInterceptions));
        });

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
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PausedIterator_KeepsItsInterceptorAndPreviousStateAcrossRebinding(bool alps)
    {
        var creator = new CounterCreator(static () => { });
        IInterceptor<int> interceptor = new IdentityInterceptor(static () => { });
        var algorithm = CreateAlgorithm(alps, creator, interceptor, mutationRate: 1);
        var parent = ResolutionScope.Create();
        await using var paused = Run(parent, algorithm).GetAsyncEnumerator();
        (await paused.MoveNextAsync()).ShouldBeTrue();
        Candidate(paused.Current).ShouldBe(1);

        var observedInterceptions = new CountAccumulator();
        var child = parent.CreateChildScope(builder => builder.Wrap(interceptor, original => original.CountCalls(observedInterceptions)));
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

    private static IAlgorithm<int> CreateAlgorithm(bool alps, ICreator<int> creator, IInterceptor<int> interceptor, double mutationRate = 0.5) =>
        alps
            ? new AlpsGeneticAlgorithm<int>
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
            }
            : new OpenEndedRelevantAllelesPreservingGeneticAlgorithm<int>
            {
                Creator = creator,
                Crossover = SelectFirstParentCrossover<int>.Instance,
                Mutator = new ImprovingMutator(),
                Interceptor = interceptor,
                Selector = new RandomSelector<int>(),
                PopulationSize = 1,
                Elites = 0,
                MaxEffort = 1,
                MaximumGenerations = 3
            };

    private static IAsyncEnumerable<ISearchState> Run(ResolutionScope scope, IAlgorithm<int> algorithm) =>
        algorithm is AlpsGeneticAlgorithm<int>
            ? scope.Resolve<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, AlpsState<int>>(algorithm)
                .RunStreamingAsync(Problem, RandomNumberGenerator.Create(7), ct: TestContext.Current.CancellationToken)
            : scope.Resolve<int, UnrestrictedSearchSpace<int>, IProblem<int, UnrestrictedSearchSpace<int>>, PopulationState<int>>(algorithm)
                .RunStreamingAsync(Problem, RandomNumberGenerator.Create(7), ct: TestContext.Current.CancellationToken);

    private static async Task<int> Next(ResolutionScope scope, IAlgorithm<int> algorithm)
    {
        await using var iterator = Run(scope, algorithm).GetAsyncEnumerator();
        (await iterator.MoveNextAsync()).ShouldBeTrue();
        return Candidate(iterator.Current);
    }

    private static int Candidate(ISearchState state) => state switch
    {
        AlpsState<int> alps => alps.Population.Single().EvaluatedCandidates.Single().Candidate,
        PopulationState<int> population => population.Population.EvaluatedCandidates.Single().Candidate,
        _ => throw new ArgumentOutOfRangeException(nameof(state))
    };

    private sealed class CounterState
    {
        public int Calls { get; set; }
    }

    private sealed record CounterCreator(Action Prepare) : StatefulCreator<int, CounterState>
    {
        protected override CounterState CreateInitialState()
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

    private sealed record IdentityInterceptor(Action Prepare) : IInterceptor<int>
    {
        public ExecutionFactory<IInterceptorExecution<int, TRunSearchSpace, TRunProblem, TRunSearchState>> CreateExecutionFactory<TRunSearchSpace, TRunProblem, TRunSearchState>()
            where TRunSearchSpace : class, ISearchSpace<int>
            where TRunProblem : class, IProblem<int, TRunSearchSpace>
            where TRunSearchState : class, ISearchState
        {
            Prepare();
            var execution = new Execution<TRunSearchSpace, TRunProblem, TRunSearchState>();
            return _ => execution;
        }

        private sealed class Execution<TSearchSpace, TProblem, TSearchState> : IInterceptorExecution<int, TSearchSpace, TProblem, TSearchState>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
            where TSearchState : class, ISearchState
        {
            public TSearchState Transform(TSearchState currentState, TSearchState? previousState, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) => currentState;
        }
    }
}
