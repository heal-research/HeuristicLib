using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class ObservationFactoryTests
{
    [Fact]
    public async Task AlgorithmObservation_RebindingPreservesIterationsAndPausedInvocationContext()
    {
        var algorithm = new TwoStepAlgorithm();
        var problem = CreateProblem();
        var parentObservations = new List<AlgorithmObservation<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>>();
        var childObservations = new List<AlgorithmObservation<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>>();
        var childEvaluations = new List<int>();
        var root = ResolutionScope.Create(builder =>
            builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm, parentObservations.Add));
        var original = root.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm);
        var parentInitial = CreateState(1);
        await using var paused = original.RunStreamingAsync(problem, RandomNumberGenerator.Create(1), parentInitial,
            TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var parentFirst = paused.Current;

        var child = root.CreateChildScope(builder =>
        {
            builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm, childObservations.Add);
            builder.Observe(algorithm.Evaluator, observation => childEvaluations.AddRange(observation.Candidates));
        });
        var rebound = child.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm);
        rebound.ShouldNotBeSameAs(original);
        var childInitial = CreateState(10);
        var childStates = new List<PopulationState<int>>();
        await foreach (var state in rebound.RunStreamingAsync(problem, RandomNumberGenerator.Create(2), childInitial, TestContext.Current.CancellationToken))
            childStates.Add(state);

        (await paused.MoveNextAsync()).ShouldBeTrue();
        var parentSecond = paused.Current;
        (await paused.MoveNextAsync()).ShouldBeFalse();

        parentObservations.Select(observation => observation.Iteration).ShouldBe([1L, 2L, 3L, 4L]);
        parentObservations.Select(observation => observation.State).ShouldBe([parentFirst, childStates[0], childStates[1], parentSecond]);
        parentObservations.Select(observation => observation.PreviousState).ShouldBe([parentInitial, childInitial, childStates[0], parentFirst]);
        parentObservations.ShouldAllBe(observation => ReferenceEquals(observation.Algorithm, algorithm));
        parentObservations.ShouldAllBe(observation => ReferenceEquals(observation.SearchSpace, problem.SearchSpace) && ReferenceEquals(observation.Problem, problem));
        childObservations.Select(observation => observation.Iteration).ShouldBe([1L, 2L]);
        childObservations.Select(observation => observation.State).ShouldBe(childStates);
        childObservations.ShouldAllBe(observation => ReferenceEquals(observation.Algorithm, algorithm));
        childEvaluations.ShouldBe([11, 12]);

        // A separate root selects a separate execution and observation counter for the same configuration.
        var independent = ResolutionScope.Create(builder =>
            builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm, parentObservations.Add));
        var independentStates = new List<PopulationState<int>>();
        await foreach (var state in independent.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm)
            .RunStreamingAsync(problem, RandomNumberGenerator.Create(3), ct: TestContext.Current.CancellationToken))
            independentStates.Add(state);
        independentStates.Select(state => state.Population.EvaluatedCandidates.Single().Candidate).ShouldBe([1, 2]);
        parentObservations.Select(observation => observation.Iteration).ShouldBe([1L, 2L, 3L, 4L, 1L, 2L]);
    }

    [Fact]
    public void MutatorObservation_RebindsItsPredecessorAndPreservesSourceAndResult()
    {
        var events = new List<string>();
        IMutator<int> source = new RecordingMutator(events);
        IReadOnlyList<int> parents = [1, 2];
        var problem = CreateProblem();
        AssertContextualObservations(events,
            (builder, observe) => builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source, observation =>
            {
                observation.Mutator.ShouldBeSameAs(source);
                observation.Parents.ShouldBeSameAs(parents);
                observation.Offspring.ShouldBeSameAs(parents);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source),
            execution => execution.Mutate(parents, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBeSameAs(parents));
    }

    [Fact]
    public void CrossoverObservation_RebindsItsPredecessorAndPreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<int> offspring = [3];
        ICrossover<int> source = new RecordingCrossover(events, offspring);
        IReadOnlyList<Parents<int>> parents = [new(1, 2)];
        var problem = CreateProblem();
        AssertContextualObservations(events,
            (builder, observe) => builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source, observation =>
            {
                observation.Crossover.ShouldBeSameAs(source);
                observation.Parents.ShouldBeSameAs(parents);
                observation.Offspring.ShouldBeSameAs(offspring);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source),
            execution => execution.Cross(parents, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBeSameAs(offspring));
    }

    [Fact]
    public void EvaluatorObservation_RebindsItsPredecessorAndPreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<ObjectiveVector> objectiveVectors = [new(3.0)];
        IEvaluator<int> source = new RecordingEvaluator(events, objectiveVectors);
        IReadOnlyList<int> candidates = [3];
        var problem = CreateProblem();
        AssertContextualObservations(events,
            (builder, observe) => builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source, observation =>
            {
                observation.Evaluator.ShouldBeSameAs(source);
                observation.Candidates.ShouldBeSameAs(candidates);
                observation.ObjectiveVectors.ShouldBeSameAs(objectiveVectors);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source),
            execution => execution.Evaluate(candidates, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBeSameAs(objectiveVectors));
    }

    [Fact]
    public void InterceptorObservation_RebindsItsPredecessorAndPreservesSourceAndStates()
    {
        var events = new List<string>();
        var transformed = CreateState(3);
        IInterceptor<int> source = new RecordingInterceptor(events, transformed);
        var current = CreateState(2);
        var previous = CreateState(1);
        var problem = CreateProblem();
        AssertContextualObservations(events,
            (builder, observe) => builder.Observe<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(source, observation =>
            {
                observation.Interceptor.ShouldBeSameAs(source);
                observation.State.ShouldBeSameAs(transformed);
                observation.UntransformedState.ShouldBeSameAs(current);
                observation.PreviousState.ShouldBeSameAs(previous);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(source),
            execution => execution.Transform(current, previous, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBeSameAs(transformed));
    }

    private static void AssertContextualObservations<TExecution>(List<string> events,
        Action<ResolutionScopeBuilder, Action> declare, Func<ResolutionScope, TExecution> resolve, Action<TExecution> invoke)
        where TExecution : class, IExecutionNode
    {
        var root = ResolutionScope.Create(builder => declare(builder, () => events.Add("parent")));
        var original = resolve(root);
        events.ShouldBeEmpty();
        invoke(original);
        events.ShouldBe(["operation", "parent"]);
        events.Clear();

        var child = root.CreateChildScope(builder => declare(builder, () => events.Add("child")));
        var rebound = resolve(child);
        rebound.ShouldNotBeSameAs(original);
        events.ShouldBeEmpty();
        invoke(rebound);
        invoke(original);
        invoke(rebound);
        events.ShouldBe(["operation", "child", "parent", "operation", "parent", "operation", "child", "parent"]);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private static PopulationState<int> CreateState(int candidate) => Population.From([EvaluatedCandidate.From(candidate, candidate)]).ToPopulationState();

    private sealed record RecordingMutator(List<string> Events) : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random)
        {
            Events.Add("operation");
            return parents;
        }
    }

    private sealed record RecordingCrossover(List<string> Events, IReadOnlyList<int> Offspring) : StatelessCrossover<int>
    {
        public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random)
        {
            Events.Add("operation");
            return Offspring;
        }
    }

    private sealed record RecordingEvaluator(List<string> Events, IReadOnlyList<ObjectiveVector> ObjectiveVectors)
        : StatelessEvaluator<int>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
        {
            Events.Add("operation");
            return ObjectiveVectors;
        }
    }

    private sealed record RecordingInterceptor(List<string> Events, PopulationState<int> Transformed) : StatelessInterceptor<int, PopulationState<int>>
    {
        public override PopulationState<int> Transform(PopulationState<int> currentState, PopulationState<int>? previousState, IRandomNumberGenerator random)
        {
            Events.Add("operation");
            return Transformed;
        }
    }

    private sealed record TwoStepAlgorithm : Algorithm<TwoStepAlgorithm, int, PopulationState<int>>
    {
        public IEvaluator<int> Evaluator { get; } = new ProblemEvaluator<int>();

        public override ExecutionFactory<IAlgorithmExecution<int, TRunSearchSpace, TRunProblem, PopulationState<int>>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>() =>
            scope => new Execution<TRunSearchSpace, TRunProblem>(scope.Resolve<int, TRunSearchSpace, TRunProblem>(Evaluator));

        private sealed class Execution<TSearchSpace, TProblem>(IEvaluatorExecution<int, TSearchSpace, TProblem> evaluator)
            : IAlgorithmExecution<int, TSearchSpace, TProblem, PopulationState<int>>
            where TSearchSpace : class, ISearchSpace<int>
            where TProblem : class, IProblem<int, TSearchSpace>
        {
            public async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(TProblem problem, IRandomNumberGenerator random,
                PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                var start = initialState?.Population.EvaluatedCandidates.Single().Candidate ?? 0;
                for (var step = 1; step <= 2; step++)
                {
                    ct.ThrowIfCancellationRequested();
                    var candidate = start + step;
                    var objective = evaluator.Evaluate([candidate], random, problem.SearchSpace, problem).Single();
                    yield return Population.From([candidate.ToEvaluated(objective)]).ToPopulationState();
                }
                await Task.CompletedTask;
            }
        }
    }
}
