using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Problems.Dynamic;
using HEAL.HeuristicLib.SearchSpaces;
using MetaOptimizationGenotype = HEAL.HeuristicLib.Encodings.Composite.CompositeGenotype<HEAL.HeuristicLib.Encodings.RealVectors.RealVector, HEAL.HeuristicLib.Encodings.IntegerVectors.IntegerVector>;

namespace HEAL.HeuristicLib.Tests.Scenarios.Papers.AutoEC;

public partial class AutoEcPaperScenarioTests
{
    [Theory]
    [InlineData("moving-peaks")]
    [InlineData("interpolated-qap")]
    [InlineData("activated-tsp")]
    [InlineData("sliding-window-symreg")]
    public async Task OperatorSelection_EveryCombinationRunsOnItsProblemFamily(string family)
    {
        foreach (var kind in MatrixScaffoldAlgorithmVariants)
        {
            switch (family)
            {
                case "moving-peaks":
                    await AssertOperatorCombinations(new MovingPeaksAutoEcScenario(kind, targetEpochChanges: 4, epochLength: 100));
                    break;
                case "interpolated-qap":
                    await AssertOperatorCombinations(new InterpolatedQapAutoEcScenario(kind, targetEpochChanges: 4, epochLength: 100));
                    break;
                case "activated-tsp":
                    await AssertOperatorCombinations(new ActivatedTspAutoEcScenario(
                        Path.Combine(AppContext.BaseDirectory, "TestData", "berlin52.tsp"), null,
                        targetEpochChanges: 4, epochLength: 100, childAlgorithmKind: kind));
                    break;
                case "sliding-window-symreg":
                    await AssertOperatorCombinations(new SlidingWindowSymRegAutoEcScenario(kind, targetEpochChanges: 4, epochLength: 100));
                    break;
            }
        }
    }

    private static async Task AssertOperatorCombinations<TCandidate, TSearchSpace, TProblem>(
        AutoEcPaperScenario<TCandidate, TSearchSpace, TProblem> scenario)
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : DynamicProblem<TProblem, TCandidate, TSearchSpace>
    {
        using var problem = scenario.CreateProblem();
        var evaluator = scenario.CreateEvaluator(problem);
        var racing = scenario.CreateRacingAlgorithm(problem, evaluator);
        var ranges = scenario.HyperParameterRanges;
        var crossoverRange = ranges.Single(range => range.Name == "crossover");
        var mutatorRange = ranges.Single(range => range.Name == "mutator");
        crossoverRange.Choices.ShouldNotBeNull();
        mutatorRange.Choices.ShouldNotBeNull();
        AssertCatalogCoversEncoding<TCandidate, ICrossover<TCandidate>>(crossoverRange, nameof(ChooseOneCrossover));
        AssertCatalogCoversEncoding<TCandidate, IMutator<TCandidate>>(mutatorRange, nameof(ChooseOneMutator));
        var crossovers = new Dictionary<int, ICrossover<TCandidate>>();
        var mutators = new Dictionary<int, IMutator<TCandidate>>();

        for (var crossover = 0; crossover < crossoverRange.Choices.Count; crossover++)
        {
            for (var mutator = 0; mutator < mutatorRange.Choices.Count; mutator++)
            {
                var configuration = new MetaOptimizationGenotype(
                    RealVector.Create(ranges.Single(range => range.Name == "mutationRate").Maximum),
                    IntegerVector.Create((int)ranges.Single(range => range.Name == "populationSize").Minimum, crossover, mutator));
                var algorithm = racing.AlgBuilder(configuration, null);
                if (scenario.ChildAlgorithmKind == AutoEcChildAlgorithmKind.GeneticAlgorithm)
                {
                    var ga = algorithm.ShouldBeOfType<GeneticAlgorithm<TCandidate>>();
                    crossovers.TryAdd(crossover, ga.Crossover);
                    mutators.TryAdd(mutator, ga.Mutator);
                }
                var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(crossover * mutatorRange.Choices.Count + mutator))
                    .AddAspect(problem.CreateIterationUpdateAspect(algorithm));
                await using var stream = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
                    .GetAsyncEnumerator(TestContext.Current.CancellationToken);

                for (var generation = 0; generation < 4; generation++)
                {
                    (await stream.MoveNextAsync()).ShouldBeTrue();
                    stream.Current.Population.EvaluatedCandidates.ShouldAllBe(candidate => problem.SearchSpace.Contains(candidate.Candidate));
                }
            }
        }

        if (scenario.ChildAlgorithmKind == AutoEcChildAlgorithmKind.GeneticAlgorithm)
        {
            var chooseCrossover = crossovers[crossoverRange.Choices.Count - 1].ShouldBeOfType<ChooseOneCrossover<TCandidate>>();
            chooseCrossover.Weights.ShouldBeEmpty();
            chooseCrossover.ChildCrossovers.ShouldBe(Enumerable.Range(0, crossovers.Count - 1).Select(index => crossovers[index]));
            var chooseMutator = mutators[mutatorRange.Choices.Count - 1].ShouldBeOfType<ChooseOneMutator<TCandidate>>();
            chooseMutator.Weights.ShouldBeEmpty();
            chooseMutator.ChildMutators.ShouldBe(Enumerable.Range(0, mutators.Count - 1).Select(index => mutators[index]));
        }

        var result = await RunScenario(scenario);
        result.Output.RacingQualityTrace.ShouldAllBe(point =>
            point.Configuration.RealValues.Count == 1 && point.Configuration.IntegerValues.Count == 3 &&
            point.Configuration.IntegerValues[1] >= 0 && point.Configuration.IntegerValues[1] < crossoverRange.Choices.Count &&
            point.Configuration.IntegerValues[2] >= 0 && point.Configuration.IntegerValues[2] < mutatorRange.Choices.Count);
    }

    private static void AssertCatalogCoversEncoding<TCandidate, TOperator>(AutoEcParameterRange range, string compositeName)
    {
        var leaves = typeof(TCandidate).Assembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters &&
                type.Namespace == typeof(TCandidate).Namespace && typeof(TOperator).IsAssignableFrom(type))
            .Select(type => type.Name);
        // A boundary-repair wrapper and fixed parameter values do not make another leaf option.
        var names = range.Choices!.Select(name => name.Split(['(', '+'], StringSplitOptions.None)[0]);
        names.Order(StringComparer.Ordinal).ShouldBe(leaves.Append(compositeName).Order(StringComparer.Ordinal));
    }
}
