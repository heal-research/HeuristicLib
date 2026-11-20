using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Experiments;

public class ExperimentSpecs
{
    [Fact]
    public async Task RepetitionUsesIndependentAlgorithmRuns()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleHillClimber(problem).TerminatedAfterIterations(6);
        var experiment = algorithm.Repeat(3);

        var results = await experiment.RunToCompletionAsync(problem, RandomNumberGenerator.Create(999), cancellationToken: TestContext.Current.CancellationToken);

        results.Length.ShouldBe(3);
        results.Select(result => result.Trial.Repetition).ShouldBe([0, 1, 2]);
        results.All(result => problem.SearchSpace.Contains(result.State.EvaluatedCandidate.Candidate)).ShouldBeTrue();
        results.Select(result => result.Trial.Run).Distinct(ReferenceEqualityComparer.Instance).Count().ShouldBe(3);
    }

    [Fact]
    public async Task ExperimentRunsGridsOverConcreteProblemsAndKeepsPerTrialResults()
    {
        var rastrigin10 = CreateRastriginProblem(dimension: 10);
        var rastrigin20 = CreateRastriginProblem(dimension: 20);
        var ga = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            MaximumGenerations = 2
        };
        var tournament2 = new TournamentSelector<RealVector>(2);
        var tournament4 = new TournamentSelector<RealVector>(4);

        var best = Analyzer.BestSoFar<RealVector>();
        var gridA = ga.AsParameterGrid()
            .VaryBy(GeneticAlgorithm<RealVector>.PopulationSizeParameter, [50, 100])
            .VaryBy("MutationRate", [0.1, 0.3])
            .VaryBy([0.1], (g, eliteFraction) => g with { Elites = (int)(g.PopulationSize * eliteFraction) });
        var gridB = ga.AsParameterGrid().VaryBy(GeneticAlgorithm<RealVector>.SelectorParameter, [tournament2, tournament4]);
        var experiment = Experiment.Create([gridA, gridB], repetitions: 5);

        var gridRun = Experiment.Create(gridA).CreateRun([rastrigin10, rastrigin20], RandomNumberGenerator.Create(42), [best]);
        gridRun.Trials.Length.ShouldBe(8);

        var run = experiment.CreateRun([rastrigin10, rastrigin20], RandomNumberGenerator.Create(42), [best]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        foreach (var trial in run.Trials)
        {
            TestFunctionProblem problem = trial.Problem;
            Console.WriteLine($"Dimension {problem.TestFunction.Dimension}, parameter candidate {trial.ParameterCandidateIndex}, repetition {trial.Repetition}");
            foreach (var (parameter, value) in trial.Settings)
                Console.WriteLine($"{parameter.Name} = {value}");
            trial.Run.Result(best).Count.ShouldBeGreaterThan(0);
        }

        var byParameterCandidate = run.Trials.GroupBy(trial => (trial.ProblemPosition, trial.ParameterCandidateIndex)).ToArray();

        byParameterCandidate.Length.ShouldBe(12);
        byParameterCandidate.ShouldAllBe(group => group.Count() == 5);
        run.Trials[0].Settings[0].Value.ShouldBe(50);
        run.Trials[20].Settings.Single().Value.ShouldBeSameAs(tournament2);
    }

    [Fact]
    public void ScriptedConfigurationsSupportMixedProblemsAndIndependentStreams()
    {
        var rastrigin = CreateRastriginProblem(dimension: 4);
        var sphere = FuncProblem.Create((RealVector point) => point.Sum(x => x * x), rastrigin.SearchSpace, SingleObjective.Minimize);
        IReadOnlyList<IProblem<RealVector, BoundedRealVectorSearchSpace>> problems = [rastrigin, sphere];
        var ga = new GeneticAlgorithm<RealVector>
        {
            Creator = new UniformDistributedCreator(),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            MaximumGenerations = 2
        };
        var generated = new[] { ga with { PopulationSize = 20 }, ga with { PopulationSize = 40 } };

        var experiment = Experiment.Create([.. generated.Select(configuration => configuration.AsParameterGrid())]);
        var independent = experiment with { ShareRandomStreamsAcrossParameterCandidates = false };
        var run = independent.CreateRun(problems, RandomNumberGenerator.Create(42));

        run.Trials.Select(trial => (trial.ProblemPosition, trial.ParameterCandidateIndex, trial.Repetition))
            .ShouldBe([(0, 0, 0), (0, 1, 0), (1, 0, 0), (1, 1, 0)]);
        run.Trials[2].Problem.ShouldBeSameAs(sphere);
        run.Trials.ShouldAllBe(trial => trial.Settings.IsEmpty);
    }

    [Fact]
    public async Task ConfigurationsWithAnalysisOnTheirOwnEvaluatorKeepTheirTraceDefinitionForReading()
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
        var experiment = Experiment.Create([.. cases.Select(value => value.Algorithm.AsParameterGrid())], repetitions: 2);
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
        var completedRepetitions = new List<int>();

        await foreach (var completedTask in Task.WhenEach(trialTasks))
        {
            var (trial, state) = await completedTask;
            completedRepetitions.Add(trial.Repetition);
            problem.SearchSpace.Contains(state.EvaluatedCandidate.Candidate).ShouldBeTrue();
        }

        completedRepetitions.Order().ShouldBe([0, 1, 2]);
    }

    [Fact]
    public void DirectExperimentExecutionExtensions_ExposeRunStreamingAndRunToCompletion()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var algorithm = CreateSimpleHillClimber(problem).TerminatedAfterIterations(2);

        var streamedEntries = algorithm.Repeat(2)
            .RunStreaming(problem, RandomNumberGenerator.Create(234), ExecutionConcurrency.Sequential(), cancellationToken: TestContext.Current.CancellationToken)
            .ToList();
        var experiment = Experiment.Create(algorithm.AsParameterGrid(), repetitions: 2);
        var completedTrials = experiment.RunToCompletion(problem, RandomNumberGenerator.Create(234), ExecutionConcurrency.Sequential(), cancellationToken: TestContext.Current.CancellationToken);
        var repeated = algorithm.Repeat(3).RunToCompletion(problem, RandomNumberGenerator.Create(234), cancellationToken: TestContext.Current.CancellationToken);

        var grid = CreateSimpleHillClimber(problem).AsParameterGrid()
            .VaryBy([4, 8], (configuration, neighbors) => configuration with { MaxNeighbors = neighbors });
        var once = Experiment.Create(grid).RunToCompletion(problem, RandomNumberGenerator.Create(234), cancellationToken: TestContext.Current.CancellationToken);

        once.Select(result => (result.Trial.ParameterCandidateIndex, result.Trial.Repetition)).ShouldBe([(0, 0), (1, 0)]);
        streamedEntries.Select(entry => entry.Trial.Repetition).Distinct().ShouldBe([0, 1]);
        completedTrials.Select(result => result.Trial.Repetition).ShouldBe([0, 1]);
        repeated.Select(result => result.Trial.Repetition).ShouldBe([0, 1, 2]);
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
