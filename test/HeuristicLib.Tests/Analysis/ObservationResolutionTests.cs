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

public class ObservationResolutionTests
{
    [Fact]
    public async Task AlgorithmObservation_NumbersEachCallAndPreservesThePausedInvocationContext()
    {
        IAlgorithm<int, PopulationState<int>> algorithm = new TwoStepAlgorithm();
        var problem = CreateProblem();
        var parentObservations = new List<AlgorithmObservation<int, PopulationState<int>>>();
        var observed = algorithm.Substitute(algorithm.Iterations.Observing(parentObservations.Add));
        var root = ResolutionScope.Create();
        var original = root.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(observed, problem);
        var parentInitial = CreateState(1);
        await using var paused = original.RunStreamingAsync(RandomNumberGenerator.Create(1), parentInitial, TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        (await paused.MoveNextAsync()).ShouldBeTrue();
        var parentFirst = paused.Current;

        // A child scope builds its own executors for the same graph, observation wrapper included.
        var child = root.CreateChildScope();
        var rebound = child.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(observed, problem);
        rebound.ShouldNotBeSameAs(original);
        var childInitial = CreateState(10);
        var childStates = new List<PopulationState<int>>();
        await foreach (var state in rebound.RunStreamingAsync(RandomNumberGenerator.Create(2), childInitial, TestContext.Current.CancellationToken))
            childStates.Add(state);

        (await paused.MoveNextAsync()).ShouldBeTrue();
        var parentSecond = paused.Current;
        (await paused.MoveNextAsync()).ShouldBeFalse();

        // Each call numbers its own iterations, so the paused call continues at 2 after the rebound call.
        parentObservations.Select(observation => observation.Iteration).ShouldBe([1L, 1L, 2L, 2L]);
        parentObservations.Select(observation => observation.State).ShouldBe([parentFirst, childStates[0], childStates[1], parentSecond]);
        parentObservations.Select(observation => observation.PreviousState).ShouldBe([parentInitial, childInitial, childStates[0], parentFirst]);
        parentObservations.ShouldAllBe(observation => ReferenceEquals(observation.Algorithm, algorithm));
        parentObservations.ShouldAllBe(observation => ReferenceEquals(observation.SearchSpace, problem.SearchSpace) && ReferenceEquals(observation.Problem, problem));

        // A separate root selects a separate execution for the same configuration.
        var independent = ResolutionScope.Create();
        var independentStates = new List<PopulationState<int>>();
        await foreach (var state in independent.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(observed, problem)
            .RunStreamingAsync(RandomNumberGenerator.Create(3), ct: TestContext.Current.CancellationToken))
            independentStates.Add(state);
        independentStates.Select(state => state.Population.EvaluatedCandidates.Single().Candidate).ShouldBe([1, 2]);
        parentObservations.Select(observation => observation.Iteration).ShouldBe([1L, 1L, 2L, 2L, 1L, 2L]);
    }

    [Fact]
    public void MutatorObservation_PreservesSourceAndResult()
    {
        var events = new List<string>();
        IOperator<Mutation, int> source = new RecordingMutator(events.Add);
        IReadOnlyList<int> parents = [1, 2];
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Mutator.ShouldBeSameAs(source);
                observation.Parents.ShouldBe(parents.ToValueArray());
                observation.Offspring.ShouldBe(parents.ToValueArray());
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(observed, problem),
            executor => executor.Mutate(parents, RandomNumberGenerator.Create(1)).ShouldBeSameAs(parents));
    }

    [Fact]
    public void CrossoverObservation_PreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<int> offspring = [3];
        IOperator<Crossover, int> source = new RecordingCrossover(events.Add, () => offspring);
        IReadOnlyList<Parents<int>> parents = [new(1, 2)];
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Crossover.ShouldBeSameAs(source);
                observation.Parents.ShouldBe(parents.ToValueArray());
                observation.Offspring.ShouldBe(offspring.ToValueArray());
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(observed, problem),
            executor => executor.Cross(parents, RandomNumberGenerator.Create(1)).ShouldBeSameAs(offspring));
    }

    [Fact]
    public void EvaluatorObservation_PreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<ObjectiveVector> objectiveVectors = [new(3.0)];
        IOperator<Evaluation, int> source = new RecordingEvaluator(events.Add, () => objectiveVectors);
        IReadOnlyList<int> candidates = [3];
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Evaluator.ShouldBeSameAs(source);
                observation.Candidates.ShouldBe(candidates.ToValueArray());
                observation.ObjectiveVectors.ShouldBe(objectiveVectors.ToValueArray());
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(observed, problem),
            executor => executor.Evaluate(candidates, RandomNumberGenerator.Create(1)).ShouldBeSameAs(objectiveVectors));
    }

    [Fact]
    public void InterceptorObservation_PreservesSourceAndStates()
    {
        var events = new List<string>();
        var transformed = CreateState(3);
        IOperator<Interception<PopulationState<int>>, int> source = new RecordingInterceptor(events.Add, transformed);
        var current = CreateState(2);
        var previous = CreateState(1);
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Interceptor.ShouldBeSameAs(source);
                observation.State.ShouldBeSameAs(transformed);
                observation.UntransformedState.ShouldBeSameAs(current);
                observation.PreviousState.ShouldBeSameAs(previous);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(observed, problem),
            executor => executor.Transform(current, previous, RandomNumberGenerator.Create(1)).ShouldBeSameAs(transformed));
    }

    [Fact]
    public void CreatorObservation_PreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<int> created = [4, 5];
        IOperator<Creation, int> source = new RecordingCreator(events.Add, () => created);
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Creator.ShouldBeSameAs(source);
                observation.Candidates.ShouldBe(created.ToValueArray());
                observation.Count.ShouldBe(2);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(observed, problem),
            executor => executor.Create(2, RandomNumberGenerator.Create(1)).ShouldBeSameAs(created));
    }

    [Fact]
    public void CreatorObservation_KeepsTheBatchWhenTheCreatorReusesItsList()
    {
        var batch = new List<int> { 0 };
        IOperator<Creation, int> creator = new BatchCreator(count =>
        {
            batch[0] = count;
            return batch;
        });
        var observations = new List<CreationObservation<int>>();
        var observed = creator.Substitute(creator.Invocations.Observing(observations.Add));
        var executor = ResolutionScope.Create().Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(observed, CreateProblem());
        var random = RandomNumberGenerator.Create(1);

        executor.Create(1, random).ShouldBeSameAs(batch);
        executor.Create(2, random).ShouldBeSameAs(batch);

        observations[0].Candidates.ShouldBe([1]);
        observations[1].Candidates.ShouldBe([2]);
    }

    [Fact]
    public void SelectorObservation_PreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<EvaluatedCandidate<int>> population = [EvaluatedCandidate.From(1, 1), EvaluatedCandidate.From(2, 2)];
        IReadOnlyList<EvaluatedCandidate<int>> selected = [population[1]];
        IOperator<Selection, int> source = new RecordingSelector(events.Add, () => selected);
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Selector.ShouldBeSameAs(source);
                observation.Selected.ShouldBe(selected.ToValueArray());
                observation.Population.ShouldBe(population.ToValueArray());
                observation.Count.ShouldBe(1);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(observed, problem),
            executor => executor.Select(population, 1, RandomNumberGenerator.Create(1))
                .ShouldBeSameAs(selected));
    }

    [Fact]
    public void RefinerObservation_PreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<int> candidates = [1, 2];
        IReadOnlyList<int> refined = [3, 4];
        IOperator<Refinement, int> source = new RecordingRefiner(events.Add, () => refined);
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Refiner.ShouldBeSameAs(source);
                observation.Refined.ShouldBe(refined.ToValueArray());
                observation.Candidates.ShouldBe(candidates.ToValueArray());
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(observed, problem),
            executor => executor.Refine(candidates, RandomNumberGenerator.Create(1)).ShouldBeSameAs(refined));
    }

    [Fact]
    public void ReplacerObservation_PreservesSourceAndResult()
    {
        var events = new List<string>();
        IReadOnlyList<EvaluatedCandidate<int>> previous = [EvaluatedCandidate.From(1, 1)];
        IReadOnlyList<EvaluatedCandidate<int>> offspring = [EvaluatedCandidate.From(2, 2)];
        IReadOnlyList<EvaluatedCandidate<int>> replaced = [EvaluatedCandidate.From(3, 3)];
        IOperator<Replacement, int> source = new RecordingReplacer(events.Add, () => replaced);
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Replacer.ShouldBeSameAs(source);
                observation.Replaced.ShouldBe(replaced.ToValueArray());
                observation.PreviousPopulation.ShouldBe(previous.ToValueArray());
                observation.OffspringPopulation.ShouldBe(offspring.ToValueArray());
                observation.Count.ShouldBe(1);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(observed, problem),
            executor => executor.Replace(previous, offspring, 1, RandomNumberGenerator.Create(1))
                .ShouldBeSameAs(replaced));
    }

    [Fact]
    public void TerminatorObservation_PreservesStateAndDecision()
    {
        var events = new List<string>();
        IOperator<Termination, int> source = new RecordingTerminator(events.Add);
        var state = CreateState(1);
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Terminator.ShouldBeSameAs(source);
                observation.State.ShouldBeSameAs(state);
                observation.IsTerminal.ShouldBeTrue();
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.Resolve<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>, PopulationState<int>>(observed, problem),
            executor => executor.IsTerminalState(state, RandomNumberGenerator.Create(1)).ShouldBeTrue());
    }

    /// <summary>
    /// An operation defined outside the library is observed like the library's own once its author declares its
    /// observation and source.
    /// </summary>
    [Fact]
    public void ExternalOperationObservation_PreservesSourceAndResult()
    {
        var events = new List<string>();
        IOperator<Checksum, int> source = new RecordingChecksum(events.Add, 17);
        var problem = CreateProblem();
        AssertObservation(events, source,
            observe => source.Invocations.Observing(observation =>
            {
                observation.Checksum.ShouldBeSameAs(source);
                observation.Result.ShouldBe(17);
                observation.SearchSpace.ShouldBeSameAs(problem.SearchSpace);
                observation.Problem.ShouldBeSameAs(problem);
                observe();
            }),
            (scope, observed) => scope.For<int, DummySearchSpace<int>, FuncProblem<int, DummySearchSpace<int>>>(problem).Resolve(observed),
            executor => executor.Read(RandomNumberGenerator.Create(1)).ShouldBe(17));
    }

    // The observation reports after the operation returned, from the wrapper the analyzer placed around the source.
    private static void AssertObservation<TNode, TExecutor>(List<string> events, TNode source,
        Func<Action, ISubstitution> observing, Func<ResolutionScope, TNode, TExecutor> resolve, Action<TExecutor> invoke)
        where TNode : class, IConfigurationNode
        where TExecutor : class, IExecutor
    {
        var executor = resolve(ResolutionScope.Create(), source.Substitute(observing(() => events.Add("observed"))));
        events.ShouldBeEmpty();
        invoke(executor);
        events.ShouldBe(["operation", "observed"]);
    }

    private static FuncProblem<int, DummySearchSpace<int>> CreateProblem() =>
        FuncProblem.Create(static (int candidate) => candidate, DummySearchSpace<int>.Instance, SingleObjective.Minimize);

    private static PopulationState<int> CreateState(int candidate) => Population.From([EvaluatedCandidate.From(candidate, candidate)]).ToPopulationState();

    private sealed record RecordingTerminator(Action<string> RecordEvent) : StatelessTerminator<int, PopulationState<int>>
    {
        public override bool IsTerminalState(PopulationState<int> state, IRandomNumberGenerator random)
        {
            RecordEvent("operation");
            return true;
        }
    }

    private sealed record RecordingChecksum(Action<string> RecordEvent, int Value) : IOperator<Checksum, int>
    {
        IExecutor IOperator<int>.CreateExecutor<TSearchSpace, TProblem, TSearchState>(
            ResolutionScope<int, TSearchSpace, TProblem, TSearchState> scope) => new Executor(this);

        private sealed class Executor(RecordingChecksum source) : IChecksumExecutor
        {
            public int Read(IRandomNumberGenerator random)
            {
                source.RecordEvent("operation");
                return source.Value;
            }
        }
    }

    private sealed record RecordingMutator(Action<string> RecordEvent) : StatelessMutator<int>
    {
        public override IReadOnlyList<int> Mutate(IReadOnlyList<int> parents, IRandomNumberGenerator random)
        {
            RecordEvent("operation");
            return parents;
        }
    }

    private sealed record RecordingCrossover(Action<string> RecordEvent, Func<IReadOnlyList<int>> Offspring) : StatelessCrossover<int>
    {
        public override IReadOnlyList<int> Cross(IReadOnlyList<Parents<int>> parents, IRandomNumberGenerator random)
        {
            RecordEvent("operation");
            return Offspring();
        }
    }

    private sealed record RecordingEvaluator(Action<string> RecordEvent, Func<IReadOnlyList<ObjectiveVector>> ObjectiveVectors)
        : StatelessEvaluator<int>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
        {
            RecordEvent("operation");
            return ObjectiveVectors();
        }
    }

    private sealed record RecordingInterceptor(Action<string> RecordEvent, PopulationState<int> Transformed) : StatelessInterceptor<int, PopulationState<int>>
    {
        public override PopulationState<int> Transform(PopulationState<int> currentState, PopulationState<int>? previousState, IRandomNumberGenerator random)
        {
            RecordEvent("operation");
            return Transformed;
        }
    }

    private sealed record BatchCreator(Func<int, IReadOnlyList<int>> CreateBatch) : StatelessCreator<int>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random) => CreateBatch(count);
    }

    private sealed record RecordingCreator(Action<string> RecordEvent, Func<IReadOnlyList<int>> Created) : StatelessCreator<int>
    {
        public override IReadOnlyList<int> Create(int count, IRandomNumberGenerator random)
        {
            RecordEvent("operation");
            return Created();
        }
    }

    private sealed record RecordingSelector(Action<string> RecordEvent, Func<IReadOnlyList<EvaluatedCandidate<int>>> Selected) : StatelessSelector<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Select(IReadOnlyList<EvaluatedCandidate<int>> population,
            ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            RecordEvent("operation");
            return Selected();
        }
    }

    private sealed record RecordingRefiner(Action<string> RecordEvent, Func<IReadOnlyList<int>> Refined) : StatelessRefiner<int>
    {
        public override IReadOnlyList<int> Refine(IReadOnlyList<int> candidates, IRandomNumberGenerator random)
        {
            RecordEvent("operation");
            return Refined();
        }
    }

    private sealed record RecordingReplacer(Action<string> RecordEvent, Func<IReadOnlyList<EvaluatedCandidate<int>>> Replaced) : StatelessReplacer<int>
    {
        public override IReadOnlyList<EvaluatedCandidate<int>> Replace(IReadOnlyList<EvaluatedCandidate<int>> previousPopulation,
            IReadOnlyList<EvaluatedCandidate<int>> offspringPopulation, ObjectiveDirections objective, int count, IRandomNumberGenerator random)
        {
            RecordEvent("operation");
            return Replaced();
        }
    }

    private sealed record TwoStepAlgorithm : Algorithm<TwoStepAlgorithm, int, PopulationState<int>>
    {
        public IOperator<Evaluation, int> Evaluator { get; } = new ProblemEvaluator<int>();

        protected override IAlgorithmExecutor<PopulationState<int>> CreateExecutor<TSearchSpace, TProblem>(
            ResolutionScope<int, TSearchSpace, TProblem, PopulationState<int>> scope) =>
            new Executor(scope.Resolve(Evaluator));

        private sealed class Executor(IEvaluationExecutor<int> evaluator)
            : AlgorithmExecutor<PopulationState<int>>
        {
            public override async IAsyncEnumerable<PopulationState<int>> RunStreamingAsync(IRandomNumberGenerator random, PopulationState<int>? initialState = null, [EnumeratorCancellation] CancellationToken ct = default)
            {
                var start = initialState?.Population.EvaluatedCandidates.Single().Candidate ?? 0;
                for (var step = 1; step <= 2; step++)
                {
                    ct.ThrowIfCancellationRequested();
                    var candidate = start + step;
                    var objective = evaluator.Evaluate([candidate], random).Single();
                    yield return Population.From([candidate.ToEvaluated(objective)]).ToPopulationState();
                }
                await Task.CompletedTask;
            }
        }
    }
}
