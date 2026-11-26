#region compare-algorithms-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Experiments;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
#endregion compare-algorithms-imports
using System.Globalization;

namespace HEAL.HeuristicLib.Tests.Docs.Examples;

public sealed class CompareAlgorithmsDocumentationSpecs
{
    [Fact]
    public async Task AGeneticAlgorithmAndAHillClimberAreComparedAtTheSameBudget()
    {
        // The page shows the output of an invariant-culture console.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

#pragma warning disable xUnit1051 // The displayed program has no cancellation token.
        #region compare-algorithms-program
        const int budget = 20_000;
        (string Name, ITestFunction Function)[] functions =
        [
            ("Sphere", new SphereFunction(dimension: 10)),
            ("Rastrigin", new RastriginFunction(dimension: 10))
        ];

        var rows = new List<string>();
        foreach (var (name, function) in functions)
        {
            var problem = new TestFunctionProblem(function);
            var creator = new UniformDistributedCreator();

            var geneticAlgorithm = GeneticAlgorithm.Create(
                creator,
                new AlphaBetaBlendCrossover { Alpha = 0.7 },
                new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
                populationSize: 100,
                maximumGenerations: null,
                mutationRate: 0.2);
            var hillClimber = HillClimber.Create(
                creator,
                new GaussianMutator(mutationRate: 1.0, mutationStrength: 0.1));

            rows.Add(await Summarize(problem, $"{name}, GA",
                geneticAlgorithm.LimitedToCandidates(geneticAlgorithm.Evaluator, budget)));
            rows.Add(await Summarize(problem, $"{name}, hill climber",
                hillClimber.LimitedToCandidates(hillClimber.Evaluator, budget)));
        }

        Console.WriteLine("configuration                best     median      worst  evaluations");
        foreach (var row in rows)
            Console.WriteLine(row);

        async Task<string> Summarize<TSearchState>(
            TestFunctionProblem problem, string label, IAlgorithm<RealVector, TSearchState> algorithm)
            where TSearchState : class, ISearchState
        {
            var best = Analyzer.BestSoFar<RealVector>();
            var run = algorithm.Repeat(20).CreateRun(problem, RandomNumberGenerator.Create(seed: 999), [best]);
            await run.CompleteAsync();

            var qualities = run.Trials
                .Select(trial => trial.Run.Result(best).Last(point => point.Evaluations <= budget).Value[0])
                .Order()
                .ToArray();
            var median = (qualities[9] + qualities[10]) / 2;
            var evaluations = run.Trials.Average(trial => trial.Run.Result(best.Evaluations).CurrentCount);

            return $"{label,-24}{qualities[0],10:G3}{median,11:G3}{qualities[^1],11:G3}{evaluations,13:F0}";
        }
        #endregion compare-algorithms-program
#pragma warning restore xUnit1051

        rows.ShouldBe(Expected);
    }

    // The page quotes these rows.
    private static readonly string[] Expected =
    [
        "Sphere, GA                9.11E-07   8.97E-06   3.29E-05        20000",
        "Sphere, hill climber       0.00165    0.00299      0.006        14561",
        "Rastrigin, GA              0.00199       1.99       4.98        20000",
        "Rastrigin, hill climber       34.3       99.3        163         1931"
    ];
}
