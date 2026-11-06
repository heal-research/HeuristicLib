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
    public void OneAccumulator_MergesEveryObservedSource()
    {
        var first = new SketchEvaluator();
        var second = new SketchEvaluator();
        var analyzer = new ParetoFrontAnalyzer<int>(
            TwoMinimized, new ObjectiveVector(100, 100), first, second);
        var scope = Install(analyzer);

        scope.Results.Result(analyzer).Count.ShouldBe(0);
        Evaluate(scope, first, 1, 9);
        Evaluate(scope, second, 3, 7);

        scope.Results.Result(analyzer).Points.Select(evaluated => evaluated.Candidate).ShouldBe([1, 3, 7, 9], ignoreOrder: true);

        // The front keeps accumulating, so a later observation shows up in the same front.
        Evaluate(scope, first, 5);
        scope.Results.Result(analyzer).Count.ShouldBe(5);
    }

    [Fact]
    public void OneAccumulator_MergesEverySourceItsSubstitutionsPlace()
    {
        var first = new SketchEvaluator();
        var second = new SketchEvaluator();
        var analyzer = new ParetoFrontAnalyzer<int>(TwoMinimized, new ObjectiveVector(100, 100), first, second);
        var scope = ResolutionScope.Create();

        var placed = new TwoEvaluators(first, second).Substitute(analyzer.Substitutions);
        Evaluate(scope, placed.First, 1, 9);
        Evaluate(scope, placed.Second, 3, 7);

        scope.Results.Result(analyzer).Points.Select(evaluated => evaluated.Candidate).ShouldBe([1, 3, 7, 9], ignoreOrder: true);
    }

    [Fact]
    public async Task ConcurrentRunsOfOneFrontDefinitionHaveIndependentAccumulators()
    {
        var evaluator = new SketchEvaluator();
        var analyzer = new ParetoFrontAnalyzer<int>(TwoMinimized, new ObjectiveVector(100, 100), evaluator);
        var first = Install(analyzer);
        var second = Install(analyzer);

        await Task.WhenAll(
            Task.Run(() => Evaluate(first, evaluator, 1, 2), TestContext.Current.CancellationToken),
            Task.Run(() => Evaluate(second, evaluator, 8, 9), TestContext.Current.CancellationToken));

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
        RankAnalyzer<RealVector> ranks = Analyzer.Rank(algorithm.Crossover, algorithm.Mutator, algorithm);
        var configured = algorithm.Advised(genealogy, ranks);
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

    // Hooks on operators and on the algorithm holding them, one of which must see the graph its siblings built.
    [Fact]
    public async Task GenealogyAndRank_RecordTheSameThroughTheirSubstitutionsAsThroughAdvice()
    {
        var problem = new TestFunctionProblem(new SphereFunction(1));
        IAlgorithm<RealVector, PopulationState<RealVector>> algorithm = GeneticAlgorithm.For(problem, creator: new UniformDistributedCreator(problem.SearchSpace),
            crossover: new AlphaBetaBlendCrossover(), mutator: new GaussianMutator(1, 0.1),
            populationSize: 4, maximumGenerations: 3, mutationRate: 1);
        var ga = (GeneticAlgorithm<RealVector>)algorithm;
        var genealogy = Analyzer.Genealogy(ga.Crossover, ga.Mutator, algorithm);
        var ranks = Analyzer.Rank(ga.Crossover, ga.Mutator, algorithm);

        var attached = algorithm.Advised(genealogy, ranks).CreateRun(problem, RandomNumberGenerator.Create(1));
        await attached.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);
        var placed = algorithm.Substitute([.. genealogy.Substitutions, .. ranks.Substitutions]).CreateRun(problem, RandomNumberGenerator.Create(1));
        await placed.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        placed.Result(genealogy).GenerationCount.ShouldBe(attached.Result(genealogy).GenerationCount);
        placed.Result(ranks).Graph.GenerationCount.ShouldBe(4);
        placed.Result(ranks).Ranks.Count.ShouldBe(2);
        placed.Result(ranks).Ranks.Select(row => row.ToArray()).ShouldBe(attached.Result(ranks).Ranks.Select(row => row.ToArray()));
    }

    private static ResolutionScope Install(params Instrument[] analyzers)
    {
        var scope = ResolutionScope.Create();
        foreach (var analyzer in analyzers)
            scope.AddAspect(scope.Results.Aspect(analyzer));
        return scope;
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
        scope.Resolve<int, SketchSearchSpace, SketchProblem>(evaluator)
             .Evaluate(candidates, RandomNumberGenerator.Create(1), SketchSearchSpace.Instance, SketchProblem.Instance);

    /// <summary>Scores a candidate against two conflicting objectives, so every candidate stays on the front.</summary>
    private sealed record SketchEvaluator : Evaluator<int, SketchSearchSpace, SketchProblem>
    {
        protected override IEvaluationExecutor<int, SketchSearchSpace, SketchProblem> CreateExecutor(ResolutionScope scope) => new Executor();

        private sealed class Executor : IEvaluationExecutor<int, SketchSearchSpace, SketchProblem>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<int> candidates, IRandomNumberGenerator random,
                SketchSearchSpace searchSpace, SketchProblem problem) =>
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
