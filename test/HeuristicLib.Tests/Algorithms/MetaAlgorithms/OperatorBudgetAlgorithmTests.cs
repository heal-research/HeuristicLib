using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.Tests.Algorithms.MetaAlgorithms;

public class OperatorBudgetAlgorithmTests
{
    [Fact]
    public void WithMaxEvaluatorCalls_StopsAfterObservedEvaluatorCallUsage()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxEvaluatorCalls(1).RunStreaming(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(1);
        results.Single().Population.Solutions.Length.ShouldBe(5);
    }

    [Fact]
    public void WithMaxEvaluatorCalls_StopsAfterObservedEvaluatorCallCount()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxEvaluatorCalls(2).RunStreaming(
            problem,
            RandomNumberGenerator.Create(42),
            ct: TestContext.Current.CancellationToken).ToList();

        results.Count.ShouldBe(2);
    }

    [Fact]
    public void WithMaxOperatorCalls_CanObserveExplicitOperator()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem) with
        {
            MaximumGenerations = 5
        };

        var results = algorithm.WithMaxOperatorCalls(
            algorithm.Evaluator,
            maximumCalls: 1,
            countedOperatorFactory: static (observedOperator, counter) =>
                observedOperator.ObserveWith((_, _) => counter.IncrementBy(1)))
            .RunStreaming(
                problem,
                RandomNumberGenerator.Create(42),
                ct: TestContext.Current.CancellationToken)
            .ToList();

        results.Count.ShouldBe(1);
    }

    [Fact]
    public void Constructor_Throws_WhenMaximumCallsIsNotPositive()
    {
        var problem = CreateProblem();
        var algorithm = CreateAlgorithm(problem);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            new OperatorBudgetAlgorithm<
                RealVector,
                RealVectorSearchSpace,
                TestFunctionProblem,
                PopulationState<RealVector>,
                IEvaluator<RealVector, RealVectorSearchSpace, TestFunctionProblem>,
                IEvaluatorInstance<RealVector, RealVectorSearchSpace, TestFunctionProblem>>
            {
                Algorithm = algorithm,
                ObservedOperator = algorithm.Evaluator,
                MaximumCalls = 0,
                CountedOperatorFactory = static (observedOperator, counter) =>
                    observedOperator.ObserveWith((_, _) => counter.IncrementBy(1))
            });
    }

    private static TestFunctionProblem CreateProblem()
    {
        return new TestFunctionProblem(new SphereFunction(dimension: 3));
    }

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem> CreateAlgorithm(
        TestFunctionProblem problem)
    {
        return new GeneticAlgorithm<RealVector, RealVectorSearchSpace, TestFunctionProblem>
        {
            PopulationSize = 5,
            Creator = new UniformDistributedCreator(problem.SearchSpace),
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1),
            MutationRate = 0.5,
            Selector = new RandomSelector<RealVector>(),
            Elites = 0
        };
    }
}
