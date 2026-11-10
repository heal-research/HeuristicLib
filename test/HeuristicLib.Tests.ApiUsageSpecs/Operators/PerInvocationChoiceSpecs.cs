using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Analysis;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Operators;

public sealed class PerInvocationChoiceSpecs
{
    [Fact]
    public void AWeightedChoiceMutatesEachOffspringBatchWithOneOfItsMutators()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));
        var fineTuning = new GaussianMutator(mutationRate: 0.3, mutationStrength: 0.1);
        var exploring = new GaussianMutator(mutationRate: 1.0, mutationStrength: 2.0);
        var mutator = ChoosePerInvocation.Create([fineTuning, exploring], weights: [3.0, 1.0]);
        var algorithm = GeneticAlgorithm.For(problem,
            creator: new UniformDistributedCreator(problem.SearchSpace),
            crossover: new AlphaBetaBlendCrossover(),
            mutator: mutator,
            populationSize: 10,
            maximumGenerations: 5,
            mutationRate: 1.0);
        var fineTuningCalls = fineTuning.Invocations.Count();
        var exploringCalls = exploring.Invocations.Count();
        var choiceCalls = mutator.Invocations.Count();

        var run = algorithm.AnalyzedBy(fineTuningCalls, exploringCalls, choiceCalls).CreateRun(problem, RandomNumberGenerator.Create(7));
        var states = run.Stream(cancellationToken: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(5);
        mutator.Weights.ShouldBe([3.0, 1.0]);
        run.Result(choiceCalls).CurrentCount.ShouldBePositive();
        (run.Result(fineTuningCalls).CurrentCount + run.Result(exploringCalls).CurrentCount).ShouldBe(run.Result(choiceCalls).CurrentCount);
    }
}
