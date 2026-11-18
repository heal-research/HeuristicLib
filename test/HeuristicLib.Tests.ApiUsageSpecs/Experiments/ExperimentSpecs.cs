using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Experiments;

public class ExperimentSpecs
{
    [Fact]
    public async Task RepeatedExperiment_UsesIndependentAlgorithmRuns()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleHillClimber(problem).TerminatedAfterIterations(6);
        var experiment = algorithm.Repeat(3);

        var results = await experiment.CompleteAsync(problem, RandomNumberGenerator.Create(999), cancellationToken: TestContext.Current.CancellationToken);

        results.Length.ShouldBe(3);
        results.Select(result => result.Trial.Key).ShouldBe([0, 1, 2]);
        results.All(result => problem.SearchSpace.Contains(result.State.EvaluatedCandidate.Candidate)).ShouldBeTrue();
        results.Select(result => result.Trial.Run).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(3);
    }

    [Fact]
    public void GridAndRepetition_ComposeWithTypedDeterministicKeys()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var experiment = CreateSimpleHillClimber(problem)
            .AsGrid()
            .VaryBy([2, 4], (algorithm, batchSize) => algorithm with { BatchSize = batchSize })
            .VaryBy([8, 12], (algorithm, maximumNeighbors) => algorithm with { MaxNeighbors = maximumNeighbors })
            .Repeat(2);

        var run = experiment.CreateRun(problem, RandomNumberGenerator.Create(123));

        run.Trials.Length.ShouldBe(8);
        run.Trials.Select(trial => (trial.Key.Inner.BatchSize, trial.Key.Inner.MaxNeighbors, trial.Key.Repetition)).ShouldBe([
            (2, 8, 0), (2, 8, 1), (2, 12, 0), (2, 12, 1),
            (4, 8, 0), (4, 8, 1), (4, 12, 0), (4, 12, 1)
        ]);
    }

    // A trace attached to each case's algorithm gives every trial its own result, read from the trial's run.
    [Fact]
    public async Task TrialTraces_GiveEachConcreteAlgorithmItsOwnResult()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleHillClimber(problem);
        var best = Analyzer.BestSoFar(algorithm.Evaluator);
        var experiment = algorithm.Substitute(best)
            .AsGrid()
            .VaryBy([4, 8], (_, maximumNeighbors) => (algorithm with { MaxNeighbors = maximumNeighbors }).Substitute(best));
        var run = experiment.CreateRun(problem, RandomNumberGenerator.Create(456));

        _ = await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        run.Trials.Length.ShouldBe(2);
        run.Trials.ShouldAllBe(trial => trial.Run.Result(best).Latest != null);
        run.Trials[0].Run.Result(best).ShouldNotBeSameAs(run.Trials[1].Run.Result(best));
    }

    [Fact]
    public async Task AdvisedGridCasesCanReplaceTheEvaluatorAndKeepItsTraceDefinitionForReading()
    {
        var problem = CreateRastriginProblem(dimension: 2);
        var algorithm = CreateSimpleHillClimber(problem);
        var cases = Enumerable.Range(0, 2).Select(_ =>
        {
            var evaluator = new ProblemEvaluator<RealVector>();
            var observations = evaluator.Invocations.Trace(observation => observation.Evaluator);
            var configured = (algorithm with { Evaluator = evaluator }).Substitute(observations);
            return (Algorithm: configured, Trace: observations, Evaluator: evaluator);
        }).ToArray();
        var experiment = cases[0].Algorithm.AsGrid().VaryBy(cases, (_, value) => value.Algorithm).Repeat(2);
        var run = experiment.CreateRun(problem, RandomNumberGenerator.Create(17));

        await run.CompleteAsync(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);

        run.Trials.Length.ShouldBe(4);
        foreach (var trial in run.Trials)
        {
            var selected = cases.Single(value => ReferenceEquals(value.Algorithm, trial.Algorithm));
            var observations = trial.Run.Result(selected.Trace).Snapshot().Values;
            observations.ShouldNotBeEmpty();
            observations.ShouldAllBe(evaluator => ReferenceEquals(evaluator, selected.Evaluator));
            var other = cases.Single(value => !ReferenceEquals(value.Algorithm, trial.Algorithm));
            Should.Throw<InvalidOperationException>(() => trial.Run.Result(other.Trace)).Message.ShouldContain("does not hold");
        }
    }

    [Fact]
    public async Task StartTrials_AllowsProcessingTrialsAsTheyComplete()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var experiment = CreateSimpleHillClimber(problem).TerminatedAfterIterations(6).Repeat(3);
        var run = experiment.CreateRun(problem, RandomNumberGenerator.Create(789));
        var trialTasks = run.StartTrials(ExecutionConcurrency.Concurrent(2), cancellationToken: TestContext.Current.CancellationToken);
        var completedKeys = new List<int>();

        await foreach (var completedTask in Task.WhenEach(trialTasks))
        {
            var (trial, state) = await completedTask;
            completedKeys.Add(trial.Key);
            problem.SearchSpace.Contains(state.EvaluatedCandidate.Candidate).ShouldBeTrue();
        }

        completedKeys.Order().ShouldBe([0, 1, 2]);
    }

    [Fact]
    public void DirectExperimentExecutionExtensions_ExposeStreamAndComplete()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleHillClimber(problem).TerminatedAfterIterations(2);

        var streamedEntries = algorithm.Repeat(2)
            .Stream(problem, RandomNumberGenerator.Create(234), ExecutionConcurrency.Sequential(), cancellationToken: TestContext.Current.CancellationToken)
            .ToList();
        var completedTrials = algorithm.Repeat(2)
            .Complete(problem, RandomNumberGenerator.Create(234), ExecutionConcurrency.Sequential(), cancellationToken: TestContext.Current.CancellationToken);

        streamedEntries.Select(entry => entry.Trial.Key).Distinct().ShouldBe([0, 1]);
        completedTrials.Select(result => result.Trial.Key).ShouldBe([0, 1]);
    }

    private static TestFunctionProblem CreateRastriginProblem(int dimension) => new(new RastriginFunction(dimension));

    private static HillClimber<RealVector> CreateSimpleHillClimber(TestFunctionProblem problem) => new()
    {
        Creator = new UniformDistributedCreator(problem.SearchSpace),
        Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
        Direction = LocalSearchDirection.FirstImprovement,
        BatchSize = 4,
        MaxNeighbors = 12
    };
}
