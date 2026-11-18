#region observability-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Terminators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;
#endregion observability-imports

namespace HEAL.HeuristicLib.Tests.Docs.Execution;

#region observability-summary-state
public sealed class CandidateSummary<TCandidate>
{
    private readonly List<TCandidate> candidates = [];

    // Read the live collection once execution has stopped.
    public IReadOnlyList<TCandidate> Candidates => candidates;

    public void Add(IReadOnlyList<TCandidate> batch)
    {
        candidates.AddRange(batch);
    }
}
#endregion observability-summary-state


public sealed class ObservabilityDocumentationSpecs
{
    private static readonly BoundedRealVectorSearchSpace SearchSpace = new(1, -10.0, 10.0);

    private static readonly FuncProblem<RealVector, BoundedRealVectorSearchSpace> Problem = FuncProblem.Create(
        (RealVector candidate) => candidate[0] * candidate[0],
        SearchSpace,
        SingleObjective.Minimize);

    [Fact]
    public void BestSoFarRecordsImprovements()
    {
        var ga = CreateAlgorithm(maximumGenerations: 3);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(42);
        #region observability-simple-best
        var best = Analyzer.BestSoFar<RealVector>();
        var run = ga.CreateRun(problem, random, [best]);
        run.Complete(cancellationToken: TestContext.Current.CancellationToken);
        foreach (var point in run.Result(best))
            Console.WriteLine($"{point.Evaluations}: {point.Value}");
        #endregion observability-simple-best
        run.Result(best).Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task EachTrialHasItsOwnTrace()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(42);
        var cancellationToken = TestContext.Current.CancellationToken;

        #region observability-experiment-setup
        var evaluations = new Counter();
        var quality = algorithm.Iterations.TracePopulationQuality(
            clocks: [evaluations.Clock]);
        IAlgorithm<RealVector, PopulationState<RealVector>> receiver = algorithm;
        var run = receiver.Substitute(quality, algorithm.Evaluator.Invocations.CountingCandidates(evaluations)).Repeat(2).CreateRun(problem, random);
        await run.CompleteAsync(cancellationToken: cancellationToken);
        var analyses = run.Trials.Select(trial => trial.Run.Result(quality)).ToList();
        #endregion observability-experiment-setup

        analyses.Count.ShouldBe(2);
        analyses.ShouldAllBe(analysis => analysis.SampleCount == 3);
        analyses[0].ShouldNotBeSameAs(analyses[1]);
    }

    [Fact]
    public async Task ARunTracesItsPopulation()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(seed: 42);
        var cancellationToken = TestContext.Current.CancellationToken;

        #region observability-quick-start
        var quality = algorithm.Iterations.TracePopulationCandidates();
        var run = algorithm.CreateRun(problem, random, [quality]);
        await run.CompleteAsync(cancellationToken: cancellationToken);

        var best = run.Result(quality).RequireLatestValue().Best;
        #endregion observability-quick-start

        run.Result(quality).SampleCount.ShouldBe(3);
        best.ShouldBe(run.Result(quality).Snapshot().Values[^1].Best);
    }

    [Fact]
    public async Task TracesShareOneCandidateClock()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(seed: 42);
        var cancellationToken = TestContext.Current.CancellationToken;

        #region observability-boundary-and-clock
        var evaluations = new Counter();
        var best = Analyzer.BestSoFar(algorithm.Evaluator, clocks: [evaluations.Clock]);
        var population = algorithm.Iterations.TracePopulationQuality(clocks: [evaluations.Clock]);
        var run = algorithm.CreateRun(problem, random, [best, population, algorithm.Evaluator.Invocations.CountingCandidates(evaluations)]);
        await run.CompleteAsync(cancellationToken: cancellationToken);
        #endregion observability-boundary-and-clock

        #region observability-snapshot
        var snapshot = run.Result(population).Snapshot();
        var byEvaluations = snapshot.By(evaluations.Clock);
        #endregion observability-snapshot

        byEvaluations.Select(point => point.Time).ShouldBe([4L, 8L, 12L]);
        run.Result(best).Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void NamedConfigurationsAreSelectedByName()
    {
        IOperator<Mutation, RealVector> mutator = new NoChangeMutator<RealVector>();

        #region observability-named-selection
        var exploration = mutator.Named("exploration");
        Func<IOperator<Mutation, RealVector>, bool> selected = node => node is INamedConfiguration { Name: "exploration" };
        Func<IOperator<Mutation, RealVector>, bool> otherExploration = node => selected(node) && !ReferenceEquals(node, exploration);
        #endregion observability-named-selection

        selected(exploration).ShouldBeTrue();
        otherExploration(exploration).ShouldBeFalse();
        otherExploration(mutator.Named("exploration")).ShouldBeTrue();
    }

    [Fact]
    public async Task RetentionDecidesWhichObservationsAreStored()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 20);

        #region observability-retention
        var iterations = new Counter();
        var evaluations = new Counter();
        var quality = algorithm.Iterations.TracePopulationQuality(clocks: [iterations.Clock], retention: TraceRetention.EveryNth(10));
        var bestAtEveryEvaluation = Analyzer.BestSoFar(algorithm.Evaluator, clocks: [evaluations.Clock], retention: TraceRetention.EveryObservation());
        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(seed: 42),
            [quality, bestAtEveryEvaluation, algorithm.Iterations.Counting(iterations), algorithm.Evaluator.Invocations.CountingCandidates(evaluations)]);
        #endregion observability-retention

        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(quality).By(iterations.Clock).Select(point => point.Time).ShouldBe([10L, 20L]);
        run.Result(bestAtEveryEvaluation).Count.ShouldBe(20);
    }

    [Fact]
    public async Task AnAccumulatingAnalyzerKeepsOneSummary()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3);
        #region observability-accumulating-analyzer
        var summary = algorithm.Evaluator.Invocations.Aggregate(
            () => new CandidateSummary<RealVector>(), (state, observation) => state.Add(observation.Candidates));
        #endregion observability-accumulating-analyzer

        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(seed: 42), [summary]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(summary).Value.Candidates.Count.ShouldBe(12);
    }

    [Fact]
    public async Task CustomTracesReadOneValuePerObservation()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3);
        var crossover = algorithm.Crossover;
        var evaluator = algorithm.Evaluator;

        #region observability-custom-traces
        var size = algorithm.Iterations.Trace(
            observation => observation.State.Population.EvaluatedCandidates.Count);
        var offspring = crossover.Invocations.Trace(observation => observation.Offspring.Count);
        var quality = evaluator.Invocations.Trace(
            observation => observation.ObjectiveVectors.BestMedianWorst(observation.Problem.Objective));
        #endregion observability-custom-traces

        #region observability-best-so-far
        var bestSoFar = evaluator.Invocations.Trace(() => new BestSoFar(),
            (best, observation) => best.Update(observation.ObjectiveVectors, observation.Problem.Objective));
        #endregion observability-best-so-far

        var run = algorithm.CreateRun(Problem, RandomNumberGenerator.Create(seed: 42), [size, offspring, quality, bestSoFar]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(size).Snapshot().Values.ShouldBe([4, 4, 4]);
        run.Result(offspring).SampleCount.ShouldBe(2);
        run.Result(quality).SampleCount.ShouldBe(3);
        run.Result(bestSoFar).SampleCount.ShouldBe(3);
    }

    [Fact]
    public async Task ACombinedTraceAccumulatesSeveralEvaluators()
    {
        var firstAlgorithm = CreateAlgorithm(maximumGenerations: 2);
        var secondAlgorithm = CreateAlgorithm(maximumGenerations: 2);

        #region observability-combined-trace
        var sources = new[] { firstAlgorithm.Evaluator.Invocations, secondAlgorithm.Evaluator.Invocations };
        var combined = Analyzer.Trace(sources, () => new BestSoFar(),
            (best, observation) => best.Update(observation.ObjectiveVectors, observation.Problem.Objective),
            retention: TraceRetention.OnChange());
        #endregion observability-combined-trace

        var run = firstAlgorithm.Then(secondAlgorithm).CreateRun(Problem, RandomNumberGenerator.Create(seed: 42), [combined]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(combined).SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AnExplicitComparerRanksMultipleObjectives()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 2);
        var problem = Problem;

        #region observability-lexicographic
        var best = Analyzer.BestSoFar(algorithm.Evaluator, objectiveComparer: new LexicographicComparer(problem.Objective.Directions));
        #endregion observability-lexicographic

        ResolutionScope.Create().Results.Result(best).Count.ShouldBe(0);
    }

    [Fact]
    public async Task ACycleReportsItsInnerAlgorithmCalls()
    {
        var exploration = CreateAlgorithm(maximumGenerations: 3);
        var exploitation = CreateAlgorithm(maximumGenerations: 2);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(seed: 42);
        var cancellationToken = TestContext.Current.CancellationToken;

        #region observability-cycle
        var cycle = CycleAlgorithm.Create(exploration, exploitation) with { MaximumCycles = 2 };
        var calls = new Counter();
        var iteration = exploitation.Iterations.Trace(observation => observation.Iteration, [calls.Clock]);
        var turns = Analyzer.Trace(new[] { exploration.Calls, exploitation.Calls }, call => call.Event);
        var run = cycle.CreateRun(problem, random, [iteration, turns, exploitation.Calls.Counting(calls)]);
        await run.CompleteAsync(cancellationToken: cancellationToken);
        #endregion observability-cycle

        run.Result(iteration).By(calls.Clock).Select(point => (point.Time, point.Value)).ShouldBe([(1L, 1L), (1L, 2L), (2L, 1L), (2L, 2L)]);
        run.Result(turns).Snapshot().Values.ShouldBe([
            AlgorithmCallEvent.Started, AlgorithmCallEvent.Completed, AlgorithmCallEvent.Started, AlgorithmCallEvent.Completed,
            AlgorithmCallEvent.Started, AlgorithmCallEvent.Completed, AlgorithmCallEvent.Started, AlgorithmCallEvent.Completed]);
    }

    [Fact]
    public async Task AnalyzersCountTimeAndObserveAnOperator()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(seed: 42);
        var cancellationToken = TestContext.Current.CancellationToken;
        var offspring = new List<int>();

        #region observability-analyzers
        var counting = new Counter();
        var timing = new Duration();
        var observation = algorithm.Mutator.Invocations.Observing(
            value => offspring.Add(value.Offspring.Count));
        var run = algorithm.CreateRun(problem, random, [observation, algorithm.Mutator.Invocations.Counting(counting), algorithm.Mutator.Invocations.Timing(timing)]);
        await run.CompleteAsync(cancellationToken: cancellationToken);
        #endregion observability-analyzers

        run.Result(counting).CurrentCount.ShouldBe(2L);
        offspring.ShouldBe([4, 4]);
    }

    [Fact]
    public async Task ACountTerminatorReadsTheRunsCounter()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 100);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(seed: 42);
        var cancellationToken = TestContext.Current.CancellationToken;

        #region observability-shared-accounting
        var mutations = new Counter();
        var limited = algorithm.TerminatedBy(new AfterOperatorCountTerminator<RealVector>(mutations, 2));
        var run = limited.CreateRun(problem, random, [algorithm.Mutator.Invocations.Counting(mutations)]);

        await run.CompleteAsync(cancellationToken: cancellationToken);
        var measuredCount = run.Result(mutations).CurrentCount;
        #endregion observability-shared-accounting

        measuredCount.ShouldBe(2);
    }

    [Fact]
    public async Task ATypeSelectionCountsEverySelectedConfiguration()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3) with { Mutator = new GaussianMutator(mutationRate: 1.0, mutationStrength: 0.5) };
        var counter = new Counter();

        #region observability-type-selection
        var counting = Substitution.Of((IOperator<Mutation, RealVector> mutator) =>
            mutator.Counted(counter, OperatorCountMetric.Candidates));
        #endregion observability-type-selection

        var run = algorithm.Substitute(counting).CreateRun(Problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(counter).CurrentCount.ShouldBe(8L);

        #region observability-predicate-selection
        var smallMutations = Substitution.Of<IOperator<Mutation, RealVector>>(
            mutator => mutator is GaussianMutator { MutationStrength: < 1.0 },
            mutator => mutator.Counted(counter, OperatorCountMetric.Candidates));
        #endregion observability-predicate-selection

        algorithm.Substitute(smallMutations).Mutator.ShouldBeOfType<Counted<Mutation, RealVector>>();
    }

    private static GeneticAlgorithm<RealVector> CreateAlgorithm(int maximumGenerations) =>
        new()
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            PopulationSize = 4,
            MutationRate = 1.0,
            MaximumGenerations = maximumGenerations
        };
}
