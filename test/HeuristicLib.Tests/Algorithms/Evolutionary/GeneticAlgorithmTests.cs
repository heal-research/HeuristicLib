using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests;

public class GeneticAlgorithmTests
{
    [Fact]
    public void GeneticAlgorithm_ConfigurationRetainsConfiguredValues()
    {
        var creator = new UniformDistributedCreator { Maximum = 3.0 };
        var crossover = new SinglePointCrossover();
        var mutator = new GaussianMutator(0.1, 0.1);
        var evaluator = new DummyEvaluator<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>();
        var selector = new RandomSelector<RealVector>();

        var algorithm = new GeneticAlgorithm<RealVector, RealVectorSearchSpace>
        {
            PopulationSize = 250,
            Creator = creator,
            Crossover = crossover,
            Mutator = mutator,
            MutationRate = 0.15,
            Evaluator = evaluator,
            Selector = selector,
            Elites = 1
        };

        algorithm.PopulationSize.ShouldBe(250);
        algorithm.Creator.ShouldBeSameAs(creator);
        algorithm.Crossover.ShouldBeSameAs(crossover);
        algorithm.Mutator.ShouldBeSameAs(mutator);
        algorithm.MutationRate.ShouldBe(0.15);
        algorithm.Evaluator.ShouldBeSameAs(evaluator);
        algorithm.Selector.ShouldBeSameAs(selector);
        algorithm.Elites.ShouldBe(1);
        algorithm.Terminator.ShouldBeNull();
        algorithm.MaximumGenerations.ShouldBeNull();
        algorithm.Interceptor.ShouldBeNull();
    }
}
