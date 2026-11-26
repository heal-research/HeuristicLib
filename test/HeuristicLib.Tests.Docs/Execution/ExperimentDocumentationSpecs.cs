using System.Text.Json.Nodes;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Docs.Execution;

public class ExperimentDocumentationSpecs
{
    [Fact]
    public async Task RepetitionShowsTheDistributionOfFinalQuality()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateAlgorithm();
        var cancellationToken = TestContext.Current.CancellationToken;

        #region experiment-repeat
        var experiment = algorithm.Repeat(20);
        var results = await experiment.RunToCompletionAsync(problem, RandomNumberGenerator.Create(seed: 999), cancellationToken: cancellationToken);
        #endregion experiment-repeat

        #region experiment-summary
        double BestOf(PopulationState<RealVector> state)
        {
            var best = state.Population.EvaluatedCandidates
                .MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!;
            return best.ObjectiveVector[0];
        }

        var qualities = results.Select(result => BestOf(result.State)).Order().ToArray();
        var median = (qualities[(qualities.Length - 1) / 2] + qualities[qualities.Length / 2]) / 2;

        Console.WriteLine($"trials:  {qualities.Length}");
        Console.WriteLine($"best:    {qualities[0]:F4}");
        Console.WriteLine($"median:  {median:F4}");
        Console.WriteLine($"worst:   {qualities[^1]:F4}");
        Console.WriteLine($"reached below 0.01: {qualities.Count(quality => quality < 0.01)} of {qualities.Length}");
        #endregion experiment-summary

        qualities.Length.ShouldBe(20);
        qualities.ShouldAllBe(quality => double.IsFinite(quality));
        qualities[0].ShouldBeLessThan(qualities[^1]);

        // The page quotes this output.
        Math.Round(qualities[0], 4).ShouldBe(0.0);
        Math.Round(median, 4).ShouldBe(0.0010);
        Math.Round(qualities[^1], 4).ShouldBe(0.9965);
        qualities.Count(quality => quality < 0.01).ShouldBe(13);
    }

    [Fact]
    public async Task GridSummaryGroupsTrialsByTheirTypedSettings()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var algorithm = CreateAlgorithm();
        var cancellationToken = TestContext.Current.CancellationToken;
        double BestOf(PopulationState<RealVector> state)
        {
            var best = state.Population.EvaluatedCandidates
                .MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!;
            return best.ObjectiveVector[0];
        }

        #region experiment-grid
        var populationSize = GeneticAlgorithm<RealVector>.PopulationSizeParameter;
        var mutationRate = GeneticAlgorithm<RealVector>.MutationRateParameter;
        var grid = algorithm.AsParameterGrid()
            .VaryBy(populationSize, [50, 100, 200])
            .VaryBy("MutationRate", [0.1, 0.3])
            .VaryBy([0.05], (ga, fraction) => ga with { Elites = (int)(ga.PopulationSize * fraction) });

        var experiment = Experiment.Create(grid, repetitions: 5);
        var gridResults = await experiment.RunToCompletionAsync(problem, RandomNumberGenerator.Create(seed: 999), cancellationToken: cancellationToken);
        #endregion experiment-grid

        #region experiment-grid-summary
        var bySettings = gridResults.GroupBy(result => (
            PopulationSize: populationSize.Get(result.Trial.Algorithm),
            MutationRate: mutationRate.Get(result.Trial.Algorithm)));

        Console.WriteLine("population  mutation   median      best");
        foreach (var group in bySettings.OrderBy(group => group.Key.PopulationSize).ThenBy(group => group.Key.MutationRate))
        {
            var values = group.Select(result => BestOf(result.State)).Order().ToArray();
            var median = (values[(values.Length - 1) / 2] + values[values.Length / 2]) / 2;
            Console.WriteLine($"{group.Key.PopulationSize,10}{group.Key.MutationRate,10:F1}{median,9:F4}{values[0],10:F4}");
        }
        #endregion experiment-grid-summary

        bySettings.Count().ShouldBe(6);
        bySettings.ShouldAllBe(group => group.Count() == 5);

        // The page quotes this output as (median, best) per population size and mutation rate.
        var quoted = new Dictionary<(int, double), (double Median, double Best)>
        {
            [(50, 0.1)] = (0.9953, 0.0060),
            [(50, 0.3)] = (1.9899, 0.9950),
            [(100, 0.1)] = (0.9955, 0.0013),
            [(100, 0.3)] = (0.9950, 0.0000),
            [(200, 0.1)] = (0.0000, 0.0000),
            [(200, 0.3)] = (0.0000, 0.0000)
        };
        foreach (var group in bySettings)
        {
            var values = group.Select(result => BestOf(result.State)).Order().ToArray();
            var median = (values[(values.Length - 1) / 2] + values[values.Length / 2]) / 2;
            var expected = quoted[(group.Key.PopulationSize, group.Key.MutationRate)];
            Math.Round(median, 4).ShouldBe(expected.Median);
            Math.Round(values[0], 4).ShouldBe(expected.Best);
        }

        FigureData.Verify("experiments-grid", new JsonObject
        {
            ["cells"] = new JsonArray([.. bySettings
                .OrderBy(group => group.Key.PopulationSize)
                .ThenBy(group => group.Key.MutationRate)
                .Select(group => (JsonNode?)new JsonObject
                {
                    ["populationSize"] = group.Key.PopulationSize,
                    ["mutationRate"] = group.Key.MutationRate,
                    ["trials"] = FigureData.Numbers(group.Select(result => BestOf(result.State)).Order())
                })])
        });
    }

    [Fact]
    public async Task ConcurrentTrialsCanBeProcessedAsTheyFinish()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 4));
        var experiment = CreateAlgorithm().Repeat(4);
        var cancellationToken = TestContext.Current.CancellationToken;

        #region experiment-concurrency
        var run = experiment.CreateRun(problem, RandomNumberGenerator.Create(seed: 789));
        var tasks = run.StartTrials(ExecutionConcurrency.Concurrent(4), cancellationToken: cancellationToken);

        await foreach (var completedTask in Task.WhenEach(tasks))
        {
            var (trial, _) = await completedTask;
            Console.WriteLine($"Finished problem {trial.ProblemPosition}, parameter candidate {trial.ParameterCandidateIndex}, repetition {trial.Repetition}");
        }
        #endregion experiment-concurrency

        tasks.Length.ShouldBe(4);
        tasks.ShouldAllBe(task => task.IsCompletedSuccessfully);
    }

    private static GeneticAlgorithm<RealVector> CreateAlgorithm()
    {
        return GeneticAlgorithm.Create(
            new UniformDistributedCreator(),
            new AlphaBetaBlendCrossover { Alpha = 0.7 },
            new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            populationSize: 100,
            maximumGenerations: 100,
            mutationRate: 0.2);
    }
}
