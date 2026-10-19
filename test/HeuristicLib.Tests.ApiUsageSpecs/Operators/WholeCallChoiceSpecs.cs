using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Inspection;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Operators;

public sealed class WholeCallChoiceSpecs
{
    [Fact]
    public void AWeightedChoiceMutatesEachOffspringBatchWithOneOfItsMutators()
    {
        var problem = new TestFunctionProblem(new RastriginFunction(dimension: 3));
        var fineTuning = new GaussianMutator(mutationRate: 0.3, mutationStrength: 0.1);
        var exploring = new GaussianMutator(mutationRate: 1.0, mutationStrength: 2.0);
        var mutator = ChoosePerCall.Create([fineTuning, exploring], weights: [3.0, 1.0]);
        var algorithm = GeneticAlgorithm.For(problem,
            creator: new UniformDistributedCreator(problem.SearchSpace),
            crossover: new AlphaBetaBlendCrossover(),
            mutator: mutator,
            populationSize: 10,
            maximumGenerations: 5,
            mutationRate: 1.0);
        var fineTuningCalls = new CountAccumulator();
        var exploringCalls = new CountAccumulator();
        var choiceCalls = new CountAccumulator();

        var states = algorithm.CreateRun(problem, RandomNumberGenerator.Create(7))
            .AddAspect(Aspect.CountCalls(fineTuning, fineTuningCalls))
            .AddAspect(Aspect.CountCalls(exploring, exploringCalls))
            .AddAspect(Aspect.CountCalls(mutator, choiceCalls))
            .Stream(cancellationToken: TestContext.Current.CancellationToken)
            .ToList();

        states.Count.ShouldBe(5);
        mutator.Weights.ShouldBe([3.0, 1.0]);
        choiceCalls.CurrentCount.ShouldBePositive();
        (fineTuningCalls.CurrentCount + exploringCalls.CurrentCount).ShouldBe(choiceCalls.CurrentCount);
    }
}
