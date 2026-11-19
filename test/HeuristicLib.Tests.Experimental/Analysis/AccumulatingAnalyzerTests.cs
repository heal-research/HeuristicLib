using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Analysis.GenealogyAnalysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Analysis;

public class AccumulatingAnalyzerTests
{
    private static readonly ObjectiveDirections TwoMinimized =
        new([ObjectiveDirection.Minimize, ObjectiveDirection.Minimize], NoTotalOrderComparer.Instance);

    [Fact]
    public void ParetoAnalyzerKeepsEvaluatorReferencesOnce()
    {
        var evaluator = new ProblemEvaluator<int>();
        var equalEvaluator = new ProblemEvaluator<int>();
        var analyzer = new ParetoFrontAnalyzer<int>(SingleObjective.Minimize, new ObjectiveVector(100),
            evaluator, evaluator, equalEvaluator);
        analyzer.Expand().Count.ShouldBe(2);
    }

    [Fact]
    public void OneAccumulator_MergesEveryObservedSource()
    {
        var first = new SketchEvaluator();
        var second = new SketchEvaluator();
        var analyzer = new ParetoFrontAnalyzer<int>(
            TwoMinimized, new ObjectiveVector(100, 100), first, second);
        var scope = ResolutionScope.Create();
        var placed = new TwoEvaluators(first, second).Substitute(analyzer.Expand());

        scope.Results.Result(analyzer).Count.ShouldBe(0);
        Evaluate(scope, placed.First, 1, 9);
        Evaluate(scope, placed.Second, 3, 7);

        scope.Results.Result(analyzer).Points.Select(evaluated => evaluated.Candidate).ShouldBe([1, 3, 7, 9], ignoreOrder: true);

        // The front keeps accumulating, so a later observation shows up in the same front.
        Evaluate(scope, placed.First, 5);
        scope.Results.Result(analyzer).Count.ShouldBe(5);
    }

    [Fact]
    public async Task ConcurrentRunsOfOneFrontDefinitionHaveIndependentAccumulators()
    {
        var evaluator = new SketchEvaluator();
        var analyzer = new ParetoFrontAnalyzer<int>(TwoMinimized, new ObjectiveVector(100, 100), evaluator);
        var placed = new TwoEvaluators(evaluator, evaluator).Substitute(analyzer.Expand()).First;
        var first = ResolutionScope.Create();
        var second = ResolutionScope.Create();

        await Task.WhenAll(
            Task.Run(() => Evaluate(first, placed, 1, 2), TestContext.Current.CancellationToken),
            Task.Run(() => Evaluate(second, placed, 8, 9), TestContext.Current.CancellationToken));

        first.Results.Result(analyzer).Points.Select(point => point.Candidate).ShouldBe([1, 2]);
        second.Results.Result(analyzer).Points.Select(point => point.Candidate).ShouldBe([8, 9]);
        first.Results.Result(analyzer).ShouldNotBeSameAs(second.Results.Result(analyzer));
        first.CreateChildScope().Results.Result(analyzer).ShouldBeSameAs(first.Results.Result(analyzer));
    }

    [Fact]
    public async Task ReusedGenealogyAndRankDefinitionsCreateIndependentGraphsAndRecordClosedGenerations()
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = GeneticAlgorithm.For(problem, creator: new UniformDistributedCreator(problem.SearchSpace),
            crossover: new AlphaBetaBlendCrossover(), mutator: new GaussianMutator(1, 0.1),
            populationSize: 4, maximumGenerations: 3, mutationRate: 1);
        var genealogy = Analyzer.Genealogy(algorithm.Crossover, algorithm.Mutator, algorithm);
        Analyzer<RankState<RealVector>> ranks = Analyzer.Rank(algorithm, algorithm.Crossover, algorithm.Mutator);
        IAlgorithm<RealVector, PopulationState<RealVector>> receiver = algorithm;
        var configured = receiver.Substitute(genealogy, ranks);
        var first = configured.CreateRun(problem, RandomNumberGenerator.Create(1));
        var second = configured.CreateRun(problem, RandomNumberGenerator.Create(2));

        await Task.WhenAll(
            Task.Run(() => first.Complete(cancellationToken: TestContext.Current.CancellationToken), TestContext.Current.CancellationToken),
            Task.Run(() => second.Complete(cancellationToken: TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));

        foreach (var run in new[] { first, second })
        {
            run.Result(genealogy).GenerationCount.ShouldBe(4);
            run.Result(ranks).Graph.GenerationCount.ShouldBe(4);
            run.Result(ranks).Ranks.Count.ShouldBe(2);
            run.Result(ranks).Ranks.ShouldAllBe(row => row.Length == 4);
            run.Result(ranks).Graph.ShouldNotBeSameAs(run.Result(genealogy));
        }
        first.Result(genealogy).ShouldNotBeSameAs(second.Result(genealogy));
        first.Result(ranks).ShouldNotBeSameAs(second.Result(ranks));
        first.Result(ranks).Graph.ShouldNotBeSameAs(second.Result(ranks).Graph);
    }

    [Fact]
    public void RankCompositionMatchesTheCombinedGenealogyAndRankRecording()
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = GeneticAlgorithm.For(problem, creator: new UniformDistributedCreator(problem.SearchSpace),
            crossover: new AlphaBetaBlendCrossover(), mutator: new GaussianMutator(1, 0.1),
            populationSize: 4, maximumGenerations: 3, mutationRate: 1);
        var ranks = Analyzer.Rank(algorithm, algorithm.Crossover, algorithm.Mutator);
        var graph = new GenealogyGraph<RealVector>(EqualityComparer<RealVector>.Default);
        var expectedRanks = new List<double[]>();
        var crossovers = algorithm.Crossover.Invocations.Observing(observation =>
        {
            foreach (var (parents, child) in observation.Parents.Zip(observation.Offspring))
                graph.AddConnection([parents.Parent1, parents.Parent2], child);
        });
        var mutations = algorithm.Mutator.Invocations.Observing(observation =>
        {
            foreach (var (parent, child) in observation.Parents.Zip(observation.Offspring))
                graph.AddConnection([parent], child);
        });
        var generations = algorithm.Iterations.Observing(observation =>
        {
            graph.SetAsNewGeneration(observation.State.Population.EvaluatedCandidates.OrderBy(candidate => candidate.ObjectiveVector,
                observation.Problem.Objective.RequireTotalOrder()).Select(candidate => candidate.Candidate));
            var row = graph.AverageDescendantRanksOfPreviousGeneration();
            if (!row.IsEmpty)
                expectedRanks.Add([.. row]);
        });
        var expected = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1), [crossovers, mutations, generations]);
        var actual = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1), [ranks]);

        expected.Complete(cancellationToken: TestContext.Current.CancellationToken);
        actual.Complete(cancellationToken: TestContext.Current.CancellationToken);

        actual.Result(ranks).Graph.ToGraphViz().ShouldBe(graph.ToGraphViz());
        actual.Result(ranks).Ranks.Count.ShouldBe(expectedRanks.Count);
        foreach (var (actualRow, expectedRow) in actual.Result(ranks).Ranks.Zip(expectedRanks))
            actualRow.ShouldBe(expectedRow);
    }

    [Fact]
    public void AnExplicitlyPlacedRankAnalyzerWithItsGenealogy_RecordsWhatAttachingItRecords()
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        var algorithm = GeneticAlgorithm.For(problem, creator: new UniformDistributedCreator(problem.SearchSpace),
            crossover: new AlphaBetaBlendCrossover(), mutator: new GaussianMutator(1, 0.1),
            populationSize: 4, maximumGenerations: 3, mutationRate: 1);
        var attachedRank = Analyzer.Rank(algorithm, algorithm.Crossover, algorithm.Mutator);
        var explicitRank = Analyzer.Rank(algorithm, algorithm.Crossover, algorithm.Mutator);
        IAlgorithm<RealVector, PopulationState<RealVector>> receiver = algorithm;
        var explicitlyPlaced = receiver.Substitute(explicitRank.Genealogy).Observed(explicitRank);
        var attachedRun = algorithm.CreateRun(problem, RandomNumberGenerator.Create(1), [attachedRank]);
        var explicitRun = explicitlyPlaced.CreateRun(problem, RandomNumberGenerator.Create(1));

        attachedRun.Complete(cancellationToken: TestContext.Current.CancellationToken);
        explicitRun.Complete(cancellationToken: TestContext.Current.CancellationToken);

        var attachedResult = attachedRun.Result(attachedRank);
        var explicitResult = explicitRun.Result(explicitRank);
        explicitResult.Graph.ToGraphViz().ShouldBe(attachedResult.Graph.ToGraphViz());
        explicitResult.Ranks.ShouldNotBeEmpty();
        explicitResult.Ranks.ShouldAllBe(row => !row.IsEmpty);
        explicitResult.Ranks.Count.ShouldBe(attachedResult.Ranks.Count);
        foreach (var (explicitRow, attachedRow) in explicitResult.Ranks.Zip(attachedResult.Ranks))
            explicitRow.ShouldBe(attachedRow);
    }

    [Fact]
    public void ARankAnalyzerWithoutAnAlgorithmIsRejected()
    {
        Should.Throw<InvalidOperationException>(() => new RankAnalyzer<RealVector, PopulationState<RealVector>>(
            algorithms: [], crossovers: [new AlphaBetaBlendCrossover()]));
    }

    private sealed record TwoEvaluators(IOperator<Evaluation, int> First, IOperator<Evaluation, int> Second) : IConfigurationNode
    {
        public static Parameter<TwoEvaluators, IOperator<Evaluation, int>> FirstParameter { get; } =
            new(nameof(First), static node => node.First, static (node, value) => node with { First = value });

        public static Parameter<TwoEvaluators, IOperator<Evaluation, int>> SecondParameter { get; } =
            new(nameof(Second), static node => node.Second, static (node, value) => node with { Second = value });

        public IReadOnlyList<IParameter> GetParameters() => [FirstParameter, SecondParameter];
    }

    private static void Evaluate(ResolutionScope scope, IOperator<Evaluation, int> evaluator, params int[] candidates) =>
        scope.Resolve<int, SketchSearchSpace, SketchProblem>(evaluator, SketchProblem.Instance)
             .Evaluate(candidates, RandomNumberGenerator.Create(1));

    /// <summary>Scores a candidate against two conflicting objectives, so every candidate stays on the front.</summary>
    private sealed record SketchEvaluator : Evaluator<int, SketchSearchSpace, SketchProblem>
    {
        protected override IEvaluationExecutor<int> CreateExecutor<TRunSearchState>(ResolutionScope<int, SketchSearchSpace, SketchProblem, TRunSearchState> scope) => new Executor();

        private sealed class Executor : EvaluationExecutor<int>
        {
            public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
                [.. candidates.Select(candidate => new ObjectiveVector(candidate, 10 - candidate))];
        }
    }

    private sealed class SketchSearchSpace : ISearchSpace<int>
    {
        public static readonly SketchSearchSpace Instance = new();
        private SketchSearchSpace() { }
        public bool Contains(int candidate) => true;
    }

    private sealed class SketchProblem : IProblem<int, SketchSearchSpace>
    {
        public static SketchProblem Instance { get; } = new();
        public ObjectiveDirections Objective => TwoMinimized;
        public SketchSearchSpace SearchSpace => SketchSearchSpace.Instance;
        public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random) =>
            [.. candidates.Select(candidate => new ObjectiveVector(candidate, 10 - candidate))];
    }
}
