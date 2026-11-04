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

#region observability-accumulating-analyzer
public sealed class CandidateSummaryAnalyzer<TCandidate> : AccumulatingAnalyzer
{
    private readonly List<TCandidate> candidates = [];

    public CandidateSummaryAnalyzer(IOperator<Evaluation, TCandidate> evaluator)
    {
        Advices = [.. Aspect.Observe(evaluator.Invocations, Record)];
    }

    public IReadOnlyList<TCandidate> Candidates => candidates;

    private void Record(EvaluationObservation<TCandidate> observation)
    {
        lock (Sync)
        {
            candidates.AddRange(observation.Candidates);
        }
    }
}
#endregion observability-accumulating-analyzer

#region observability-point-type
public sealed record CoolingStep(int Step, double Temperature);

public interface ICoolingScheduleExecutor : IExecutor
{
    double Temperature(int step);
}

public sealed record GeometricCooling(double StartTemperature, double Factor) : IConfigurationNode<ICoolingScheduleExecutor>
{
    public ObservationPoint<CoolingStep> Steps => ObservationPoint.Of<CoolingStep>(this, nameof(Steps));

    public ICoolingScheduleExecutor CreateExecutor(ResolutionScope scope)
    {
        var steps = scope.Resolve(Steps);
        return new Executor(StartTemperature, Factor, steps);
    }

    private sealed class Executor(double startTemperature, double factor, IObservationPointExecutor<CoolingStep> steps) : ICoolingScheduleExecutor
    {
        public double Temperature(int step)
        {
            var temperature = startTemperature * Math.Pow(factor, step);
            steps.Report(new CoolingStep(step, temperature));
            return temperature;
        }
    }
}
#endregion observability-point-type

#region observability-custom-pointcut
public sealed class SmallMutations : Pointcut<IOperator<Mutation, RealVector>>
{
    public override bool Matches(IOperator<Mutation, RealVector> mutator) =>
        mutator is GaussianMutator { MutationStrength: < 1.0 };
}
#endregion observability-custom-pointcut

public sealed class ObservabilityDocumentationSpecs
{
    private static readonly BoundedRealVectorSearchSpace SearchSpace = new(1, -10.0, 10.0);

    private static readonly FuncProblem<RealVector, BoundedRealVectorSearchSpace> Problem = FuncProblem.Create(
        (RealVector candidate) => candidate[0] * candidate[0],
        SearchSpace,
        SingleObjective.Minimize);

    [Fact]
    public async Task EachTrialHasItsOwnTrace()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(42);
        var cancellationToken = TestContext.Current.CancellationToken;

        #region observability-experiment-setup
        var quality = algorithm.Iterations.TracePopulationQuality(
            clocks: [algorithm.Evaluator.Invocations.CountCandidates()]);
        var run = algorithm.Advised(quality).Repeat(2).CreateRun(problem, random);
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
        var run = algorithm.Advised(quality).CreateRun(problem, random);
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
        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var best = algorithm.Evaluator.Invocations.TraceBestSoFar(clocks: [evaluations]);
        var population = algorithm.Iterations.TracePopulationQuality(clocks: [evaluations]);
        var run = algorithm.Advised(best, population).CreateRun(problem, random);
        await run.CompleteAsync(cancellationToken: cancellationToken);
        #endregion observability-boundary-and-clock

        #region observability-snapshot
        var snapshot = run.Result(population).Snapshot();
        var byEvaluations = snapshot.By(evaluations);
        #endregion observability-snapshot

        byEvaluations.Select(point => point.Time).ShouldBe([4L, 8L, 12L]);
        run.Result(best).SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void NamedConfigurationsAreSelectedByName()
    {
        IOperator<Mutation, RealVector> mutator = new NoChangeMutator<RealVector>();

        #region observability-named-pointcuts
        var exploration = mutator.Named("exploration");
        var selected = Pointcut.Named<IOperator<Mutation, RealVector>>("exploration");
        var excluded = !Pointcut.Reference<IOperator<Mutation, RealVector>>(exploration);
        var otherExploration = selected & excluded;
        #endregion observability-named-pointcuts

        selected.Matches(exploration).ShouldBeTrue();
        otherExploration.Matches(exploration).ShouldBeFalse();
        otherExploration.Matches(mutator.Named("exploration")).ShouldBeTrue();
    }

    [Fact]
    public async Task RetentionDecidesWhichObservationsAreStored()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 20);

        #region observability-retention
        var iterations = algorithm.Iterations.Count();
        var evaluations = algorithm.Evaluator.Invocations.CountCandidates();
        var quality = algorithm.Iterations.TracePopulationQuality(clocks: [iterations], retention: TraceRetention.EveryNth(10));
        var bestAtEveryEvaluation = algorithm.Evaluator.Invocations.TraceBestSoFar(clocks: [evaluations], retention: TraceRetention.EveryObservation());
        #endregion observability-retention

        var run = algorithm.Advised(quality, bestAtEveryEvaluation).CreateRun(Problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(quality).By(iterations).Select(point => point.Time).ShouldBe([10L, 20L]);
        run.Result(bestAtEveryEvaluation).SampleCount.ShouldBe(20);
    }

    [Fact]
    public async Task AnAccumulatingAnalyzerKeepsOneSummary()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3);
        var summary = new CandidateSummaryAnalyzer<RealVector>(algorithm.Evaluator);

        var run = algorithm.Advised(summary).CreateRun(Problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        summary.Candidates.Count.ShouldBe(12);
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
        var bestSoFar = evaluator.Invocations.Trace<EvaluationObservation<RealVector>, ObjectiveVector>(() =>
        {
            var best = new BestSoFar();
            return observation => best.Update(observation.ObjectiveVectors, observation.Problem.Objective);
        });
        #endregion observability-best-so-far

        var run = algorithm.Advised(size, offspring, quality, bestSoFar).CreateRun(Problem, RandomNumberGenerator.Create(seed: 42));
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
        var combined = sources.TraceBestSoFar();
        #endregion observability-combined-trace

        var run = firstAlgorithm.Then(secondAlgorithm).Advised(combined).CreateRun(Problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Result(combined).SampleCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void AnExplicitComparerRanksMultipleObjectives()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 2);
        var problem = Problem;

        #region observability-lexicographic
        var best = algorithm.Evaluator.Invocations.TraceBestSoFar(objectiveComparer: new LexicographicComparer(problem.Objective.Directions));
        #endregion observability-lexicographic

        ResolutionScope.Create().Results.Result(best).SampleCount.ShouldBe(0);
    }

    [Fact]
    public async Task ACycleReportsItsCyclesAndTurns()
    {
        var exploration = CreateAlgorithm(maximumGenerations: 3);
        var exploitation = CreateAlgorithm(maximumGenerations: 2);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(seed: 42);
        var cancellationToken = TestContext.Current.CancellationToken;

        #region observability-cycle
        var cycle = CycleAlgorithm.Create(exploration, exploitation) with { MaximumCycles = 2 };
        var cycles = Clock.FromCycles(cycle);
        var iterations = exploitation.Iterations.Count();
        var iteration = exploitation.Iterations.Trace(observation => observation.Iteration, [cycles, iterations]);
        var turns = new List<string>();
        var run = cycle
            .Advised(iteration)
            .Advised(Aspect.Observe(cycle.Turns, turn => turns.Add($"cycle {turn.Cycle}, turn {turn.Turn}: algorithm {turn.Index}")))
            .CreateRun(problem, random);
        await run.CompleteAsync(cancellationToken: cancellationToken);
        #endregion observability-cycle

        run.Result(iteration).By(cycles).Select(point => (point.Time, point.Value)).ShouldBe([(1L, 1L), (1L, 2L), (2L, 1L), (2L, 2L)]);
        run.Result(iteration).By(iterations).Select(point => (point.Time, point.Value)).ShouldBe([(1L, 1L), (2L, 2L), (3L, 1L), (4L, 2L)]);
        turns.ShouldBe(["cycle 1, turn 1: algorithm 0", "cycle 1, turn 2: algorithm 1", "cycle 2, turn 3: algorithm 0", "cycle 2, turn 4: algorithm 1"]);
    }

    [Fact]
    public void AConfigurationOfYourOwnPublishesItsObservations()
    {
        #region observability-point-use
        var schedule = new GeometricCooling(StartTemperature: 100, Factor: 0.5);
        var temperatures = schedule.Steps.Trace(static step => step.Temperature);
        var scope = ResolutionScope.Create();
        scope.AddAspect(scope.Results.Aspect(temperatures));
        var executor = scope.Resolve(schedule);

        executor.Temperature(step: 0);
        executor.Temperature(step: 1);
        #endregion observability-point-use

        scope.Results.Result(temperatures).Snapshot().Values.ShouldBe([100.0, 50.0]);
    }

    [Fact]
    public async Task AspectsCountTimeAndObserveAnOperator()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3);
        var problem = Problem;
        var random = RandomNumberGenerator.Create(seed: 42);
        var cancellationToken = TestContext.Current.CancellationToken;
        var offspring = new List<int>();

        #region observability-aspects
        var counter = new CountAccumulator();
        var counting = algorithm.Mutator.CountInvocations(counter);
        var duration = new DurationAccumulator();
        var timing = algorithm.Mutator.MeasureDuration(duration, TimeProvider.System);
        var observation = Aspect.Observe(algorithm.Mutator.Invocations,
            value => offspring.Add(value.Offspring.Count));
        var run = algorithm.Advised(counting, timing, observation).CreateRun(problem, random);
        await run.CompleteAsync(cancellationToken: cancellationToken);
        #endregion observability-aspects

        counter.CurrentCount.ShouldBe(2L);
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
        var mutations = algorithm.Mutator.Invocations.Count();
        var limited = algorithm.TerminatedBy(new AfterOperatorCountTerminator<RealVector>(mutations, 2));
        var run = limited.Advised(mutations).CreateRun(problem, random);

        await run.CompleteAsync(cancellationToken: cancellationToken);
        var measuredCount = run.Result(mutations).CurrentCount;
        #endregion observability-shared-accounting

        measuredCount.ShouldBe(2);
    }

    [Fact]
    public async Task RoleAdviceCountsEverySelectedConfiguration()
    {
        var algorithm = CreateAlgorithm(maximumGenerations: 3) with { Mutator = new GaussianMutator(mutationRate: 1.0, mutationStrength: 0.5) };
        var counter = new CountAccumulator();

        #region observability-role-advice
        var counting = Aspect.Create(Pointcut.OfType<IOperator<Mutation, RealVector>>(),
            new CountingOperatorAdvice<Mutation, RealVector>(counter, OperatorCountMetric.Candidates));
        #endregion observability-role-advice

        var run = algorithm.Advised(counting).CreateRun(Problem, RandomNumberGenerator.Create(seed: 42));
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        counter.CurrentCount.ShouldBe(8L);
        new SmallMutations().Matches(algorithm.Mutator).ShouldBeTrue();
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
