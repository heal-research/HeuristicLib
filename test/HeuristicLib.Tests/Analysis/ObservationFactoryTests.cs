using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Interceptors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Replacers;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Tests.ExecutionInfrastructure.OperatorFoundation;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class ObservationFactoryTests
{
    [Fact]
    public async Task AlgorithmObservation_RebindingPreservesIterationsAndPausedInvocationContext()
    {
        var algorithm = new TwoStepAlgorithm();
        var problem = CreateProblem();
        var parentObservations = new List<AlgorithmObservation<int, PopulationState<int>>>();
        var childObservations = new List<AlgorithmObservation<int, PopulationState<int>>>();
        var childEvaluations = new List<int>();
        var root = ResolutionScope.Create(Aspect.Observe(algorithm.Iterations, parentObservations.Add));
        var original = root.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(algorithm);
        var parentInitial = CreateState(1);
        await using var paused = original.RunStreamingAsync(problem, RandomNumberGenerator.Create(1), parentInitial,
            TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var parentFirst = paused.Current;

        var child = root.CreateChildScope(Aspect.Observe(algorithm.Iterations, childObservations.Add),
            Aspect.Observe(algorithm.Evaluator.Calls, observation => childEvaluations.AddRange(observation.Candidates)));
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
        var independent = ResolutionScope.Create(Aspect.Observe(algorithm.Iterations, parentObservations.Add));
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
        IOperator<Mutation, int> source = new RecordingMutator(events);
        IReadOnlyList<int> parents = [1, 2];
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
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
        IOperator<Crossover, int> source = new RecordingCrossover(events, offspring);
        IReadOnlyList<Parents<int>> parents = [new(1, 2)];
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
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
        IOperator<Evaluation, int> source = new RecordingEvaluator(events, objectiveVectors);
        IReadOnlyList<int> candidates = [3];
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
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
        IOperator<Interception<PopulationState<int>>, int> source = new RecordingInterceptor(events, transformed);
        var current = CreateState(2);
        var previous = CreateState(1);
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
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

    [Fact]
    public void CreatorObservation_RebindsItsPredecessorAndPreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<int> created = [4, 5];
        IOperator<Creation, int> source = new RecordingCreator(events, created);
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
            {
                observation.Creator.ShouldBeSameAs(source);
                observation.Candidates.ShouldBeSameAs(created);
                observation.Count.ShouldBe(2);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source),
            execution => execution.Create(2, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBeSameAs(created));
    }

    [Fact]
    public void SelectorObservation_RebindsItsPredecessorAndPreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<EvaluatedCandidate<int>> population = [EvaluatedCandidate.From(1, 1), EvaluatedCandidate.From(2, 2)];
        IReadOnlyList<EvaluatedCandidate<int>> selected = [population[1]];
        IOperator<Selection, int> source = new RecordingSelector(events, selected);
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
            {
                observation.Selector.ShouldBeSameAs(source);
                observation.Selected.ShouldBeSameAs(selected);
                observation.Population.ShouldBeSameAs(population);
                observation.Objective.ShouldBe(problem.Objective);
                observation.Count.ShouldBe(1);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source),
            execution => execution.Select(population, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem)
                .ShouldBeSameAs(selected));
    }

    [Fact]
    public void RefinerObservation_RebindsItsPredecessorAndPreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<int> candidates = [1, 2];
        IReadOnlyList<int> refined = [3, 4];
        IOperator<Refinement, int> source = new RecordingRefiner(events, refined);
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
            {
                observation.Refiner.ShouldBeSameAs(source);
                observation.Refined.ShouldBeSameAs(refined);
                observation.Candidates.ShouldBeSameAs(candidates);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source),
            execution => execution.Refine(candidates, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBeSameAs(refined));
    }

    [Fact]
    public void ReplacerObservation_RebindsItsPredecessorAndPreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<EvaluatedCandidate<int>> previous = [EvaluatedCandidate.From(1, 1)];
        IReadOnlyList<EvaluatedCandidate<int>> offspring = [EvaluatedCandidate.From(2, 2)];
        IReadOnlyList<EvaluatedCandidate<int>> replaced = [offspring[0]];
        IOperator<Replacement, int> source = new RecordingReplacer(events, replaced);
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
            {
                observation.Replacer.ShouldBeSameAs(source);
                observation.Replaced.ShouldBeSameAs(replaced);
                observation.PreviousPopulation.ShouldBeSameAs(previous);
                observation.OffspringPopulation.ShouldBeSameAs(offspring);
                observation.Objective.ShouldBe(problem.Objective);
                observation.Count.ShouldBe(1);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(source),
            execution => execution.Replace(previous, offspring, problem.Objective, 1, RandomNumberGenerator.Create(1), problem.SearchSpace, problem)
                .ShouldBeSameAs(replaced));
    }

    [Fact]
    public void TerminatorObservation_RebindsItsPredecessorAndPreservesStateAndDecision()
    {
        var events = new List<string>();
        IOperator<Termination, int> source = new RecordingTerminator(events);
        var state = CreateState(1);
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
            {
                observation.Terminator.ShouldBeSameAs(source);
                observation.State.ShouldBeSameAs(state);
                observation.IsTerminal.ShouldBeTrue();
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(source),
            execution => execution.IsTerminalState(state, RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBeTrue());
    }

    /// <summary>
    /// An operation defined outside the library is observed like the library's own once its author declares its
    /// observation and source.
    /// </summary>
    [Fact]
    public void ExternalOperationObservation_RebindsItsPredecessorAndPreservesSourceAndResult()
    {
        var events = new List<string>();
        IOperator<Checksum, int> source = new RecordingChecksum(events, 17);
        var problem = CreateProblem();
        AssertContextualObservations(events,
            observe => Aspect.Observe(source.Calls, observation =>
            {
                observation.Checksum.ShouldBeSameAs(source);
                observation.Result.ShouldBe(17);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            scope => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>().Resolve(source),
            execution => execution.Read(RandomNumberGenerator.Create(1), problem.SearchSpace, problem).ShouldBe(17));
    }

    private static void AssertContextualObservations<TExecutor>(
List<string> events,
        Func<Action, Aspect> createAspect, Func<ResolutionScope, TExecutor> resolve, Action<TExecutor> invoke)
        where TExecutor : class, IExecutor
    {
        var root = ResolutionScope.Create(createAspect(() => events.Add("parent")));
        var original = resolve(root);
        events.ShouldBeEmpty();
        invoke(original);
        events.ShouldBe(["operation", "parent"]);
        events.Clear();

        var child = root.CreateChildScope(createAspect(() => events.Add("child")));
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

    private sealed record RecordingTerminator(List<string> Events) : StatelessTerminator<int, PopulationState<int>>
    {
        public override bool IsTerminalState(PopulationState<int> state, IRandomNumberGenerator random)
        {
            Events.Add("operation");
            return true;
        }
    }

    private sealed record RecordingChecksum(List<string> Events, int Value) : IOperator<Checksum, int>
    {
        IExecutor IOperator<Checksum, int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => new Executor(this);

        private sealed class Executor(RecordingChecksum source) : IChecksumExecutor<int, ISearchSpace<int>, IProblem<int, ISearchSpace<int>>>
        {
            public int Read(IRandomNumberGenerator random, ISearchSpace<int> searchSpace, IProblem<int, ISearchSpace<int>> problem)
            {
                source.Events.Add("operation");
                return source.Value;
            }
        }
    }

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

    private sealed record RecordingCreator(List<string> Events, IReadOnlyList<int> Created) : StatelessCreator<int>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random)
        {
            Events.Add("operation");
            return Created;
        }
    }

    private sealed record RecordingSelector(List<string> Events, IReadOnlyList<EvaluatedCandidate<int>> Selected) : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population,
            ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            Events.Add("operation");
            return Selected;
        }
    }

    private sealed record RecordingRefiner(List<string> Events, IReadOnlyList<int> Refined) : StatelessRefiner<int>
    {
        public override IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
        {
            Events.Add("operation");
            return Refined;
        }
    }

    private sealed record RecordingReplacer(List<string> Events, IReadOnlyList<EvaluatedCandidate<int>> Replaced) : StatelessReplacer<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation,
            IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            Events.Add("operation");
            return Replaced;
        }
    }

    private sealed record TwoStepAlgorithm : Algorithm<TwoStepAlgorithm, int, PopulationState<int>>
    {
        public IOperator<Evaluation, int> Evaluator { get; } = new ProblemEvaluator<int>();

        public override ExecutorFactory<IAlgorithmExecutor<int, TRunSearchSpace, TRunProblem, PopulationState<int>>> CreateExecutorFactory<TRunSearchSpace, TRunProblem>() =>
            scope => new Executor<TRunSearchSpace, TRunProblem>(scope.Resolve<int, TRunSearchSpace, TRunProblem>(Evaluator));

        private sealed class Executor<TSearchSpace, TProblem>(IEvaluationExecutor<int, TSearchSpace, TProblem> evaluator)
            : IAlgorithmExecutor<int, TSearchSpace, TProblem, PopulationState<int>>
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
