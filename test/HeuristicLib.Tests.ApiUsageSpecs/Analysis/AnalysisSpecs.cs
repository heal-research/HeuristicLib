using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Analysis;

public class AnalysisSpecs
{
    [Fact]
    public async Task Analyzer_CurrentApi_AttachesAtRunCreation_AndIsReadFromRunStateWrapper()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var baseAlgorithm = CreateSimpleGeneticAlgorithm(problem, interceptor, maximumGenerations: 4);
        var analysis = Analyzer.BestMedianWorst<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(interceptor);

        var run = baseAlgorithm.CreateRun(problem, RandomNumberGenerator.Create(777)).WithAnalyzer(analysis);

        var finalState = await run.CompleteAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        var analysisResult = run.GetResult(analysis);

        analysisResult.Count.ShouldBe(4);
        finalState.Population.EvaluatedCandidates.Count.ShouldBe(16);
    }

    [Fact]
    public async Task Analyzer_CurrentApi_CanBeQueriedDuringExecution()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var baseAlgorithm = CreateSimpleGeneticAlgorithm(problem, interceptor, maximumGenerations: 3);
        var analysis = Analyzer.BestMedianWorst<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(interceptor);

        var run = baseAlgorithm.CreateRun(problem, RandomNumberGenerator.Create(888)).WithAnalyzer(analysis);

        await using var enumerator = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
                                        .GetAsyncEnumerator(TestContext.Current.CancellationToken);

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        run.GetResult(analysis).Count.ShouldBe(1);

        (await enumerator.MoveNextAsync()).ShouldBeTrue();
        run.GetResult(analysis).Count.ShouldBe(2);

        while (await enumerator.MoveNextAsync())
        {
            _ = enumerator.Current;
        }

        run.GetResult(analysis).Count.ShouldBe(3);
    }

    [Fact]
    public async Task Analyzer_CurrentApi_CanObserveBothEvaluatorAndInterceptor()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var baseAlgorithm = CreateSimpleGeneticAlgorithm(problem, interceptor, maximumGenerations: 3);
        var analysis = Analyzer.BestMedianWorstPerEvaluation<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(
            [baseAlgorithm.Evaluator],
            [interceptor]);

        var run = baseAlgorithm.CreateRun(problem, RandomNumberGenerator.Create(321)).WithAnalyzer(analysis);

        await run.CompleteAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        var analysisResult = run.GetResult(analysis);

        analysisResult.BestSolutions.Count.ShouldBe(3);
        analysisResult.BestSolutions.All(entry => entry.evaluations > 0).ShouldBeTrue();
    }

    [Fact]
    public async Task AnalyzerAttachment_OutOverloadProducesTypedResult()
    {
        var problem = CreateRastriginProblem(dimension: 4);
        var interceptor = new IdentityInterceptor<RealVector, PopulationState<RealVector>>();
        var baseAlgorithm = CreateSimpleGeneticAlgorithm(problem, interceptor, maximumGenerations: 4);

        var run = baseAlgorithm.CreateRun(problem, RandomNumberGenerator.Create(333))
            .WithAnalyzer(Analyzer.BestMedianWorst<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(interceptor), out var analysis);
        var finalState = await run.CompleteAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        var result = run.GetResult(analysis);

        result.Count.ShouldBe(4);
        finalState.Population.EvaluatedCandidates.Count.ShouldBe(16);
    }

    private static TestFunctionProblem CreateRastriginProblem(int dimension)
    {
        return new TestFunctionProblem(new RastriginFunction(dimension));
    }

    private static GeneticAlgorithm<RealVector>
        CreateSimpleGeneticAlgorithm(
            TestFunctionProblem problem,
            IdentityInterceptor<RealVector, PopulationState<RealVector>> interceptor,
            int maximumGenerations)
    {
        return new GeneticAlgorithm<RealVector>
        {
            PopulationSize = 16,
            MaximumGenerations = maximumGenerations,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new AlphaBetaBlendCrossover { Alpha = 0.7 },
            Mutator = new GaussianMutator(mutationRate: 0.2, mutationStrength: 0.15),
            Selector = TournamentSelector.For(problem, tournamentSize: 2),
            MutationRate = 0.2,
            Elites = 1,
            Interceptor = interceptor
        };
    }
}
