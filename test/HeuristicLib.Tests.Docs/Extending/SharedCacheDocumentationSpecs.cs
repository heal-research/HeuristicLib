#region shared-cache-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
#endregion shared-cache-imports

namespace HEAL.HeuristicLib.Tests.Docs.Extending;

public sealed class SharedCacheDocumentationSpecs
{
    [Fact]
    public async Task TheAlgorithmAndItsImprovementCheckUseOneCache()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        #region shared-cache
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        IReadOnlyList<RealVector> candidates = [new RealVector(1.0), new RealVector(2.0), new RealVector(3.0), new RealVector(4.0)];

        var problemEvaluator = new ProblemEvaluator<RealVector>();
        var cache = problemEvaluator.Cached();
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            Creator = PredefinedCandidatesCreator.Create(candidates, new UniformDistributedCreator()),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            Evaluator = cache,
            Refiner = new NoChangeRefiner<RealVector>().CheckedForImprovement(cache),
            PopulationSize = 4,
            MaximumGenerations = 1
        };

        var requests = new Counter();
        var solves = new Counter();
        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42),
            [cache.Invocations.CountingCandidates(requests), problemEvaluator.Invocations.CountingCandidates(solves)]);
        await run.CompleteAsync(cancellationToken: cancellationToken);

        var evaluationRequests = run.Result(requests).CurrentCount;
        var problemEvaluations = run.Result(solves).CurrentCount;
        #endregion shared-cache

        // The improvement check evaluates the four candidates before and after refinement, and the algorithm evaluates them again.
        evaluationRequests.ShouldBe(12);
        problemEvaluations.ShouldBe(4);
    }

    [Fact]
    public async Task TwoCachesLetEveryDistinctCandidateReachTheProblemTwice()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(1, -10.0, 10.0);
        var problem = FuncProblem.Create((RealVector candidate) => candidate[0] * candidate[0], searchSpace, SingleObjective.Minimize);
        IReadOnlyList<RealVector> candidates = [new RealVector(1.0), new RealVector(2.0), new RealVector(3.0), new RealVector(4.0)];
        var problemEvaluator = new ProblemEvaluator<RealVector>();
        var cache = problemEvaluator.Cached();
        var cacheOfTheCheck = problemEvaluator.Cached();
        var algorithm = new GeneticAlgorithm<RealVector>
        {
            Creator = PredefinedCandidatesCreator.Create(candidates, new UniformDistributedCreator()),
            Crossover = SelectFirstParentCrossover<RealVector>.Instance,
            Mutator = new NoChangeMutator<RealVector>(),
            Evaluator = cache,
            Refiner = new NoChangeRefiner<RealVector>().CheckedForImprovement(cacheOfTheCheck),
            PopulationSize = 4,
            MaximumGenerations = 1
        };
        var requests = new Counter();
        var solves = new Counter();

        var run = algorithm.CreateRun(problem, RandomNumberGenerator.Create(seed: 42),
            [cache.Invocations.CountingCandidates(requests), problemEvaluator.Invocations.CountingCandidates(solves)]);
        await run.CompleteAsync(cancellationToken: TestContext.Current.CancellationToken);

        // The algorithm's cache receives four requests, and the check's cache four distinct candidates of its own.
        run.Result(requests).CurrentCount.ShouldBe(4);
        run.Result(solves).CurrentCount.ShouldBe(8);
    }
}
