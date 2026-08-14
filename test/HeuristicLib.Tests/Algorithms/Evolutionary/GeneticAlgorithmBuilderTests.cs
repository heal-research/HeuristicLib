using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using HEAL.HeuristicLib.Tests.TestSupport.Mocks;

namespace HEAL.HeuristicLib.Tests.Algorithms.Evolutionary;

public class GeneticAlgorithmBuilderTests
{
    [Fact]
    public void Build_CopiesConfiguredComponentsAndParameters()
    {
        var creator = new UniformDistributedCreator { Maximum = 3.0 };
        var crossover = new SinglePointCrossover();
        var mutator = new GaussianMutator(0.1, 0.1);
        var selector = new RandomSelector<RealVector>();
        var evaluator = new DummyEvaluator<RealVector, RealVectorSearchSpace, IProblem<RealVector, RealVectorSearchSpace>>();

        var builder = GeneticAlgorithm.GetBuilder(
          creator,
          crossover,
          mutator);
        builder.PopulationSize = 23;
        builder.MutationRate = 0.17;
        builder.Elites = 3;
        builder.Selector = selector;
        builder.Evaluator = evaluator;

        var algorithm = builder.Build();

        algorithm.Creator.ShouldBeSameAs(creator);
        algorithm.Crossover.ShouldBeSameAs(crossover);
        algorithm.Mutator.ShouldBeSameAs(mutator);
        algorithm.Selector.ShouldBeSameAs(selector);
        algorithm.Evaluator.ShouldBeSameAs(evaluator);
        algorithm.PopulationSize.ShouldBe(23);
        algorithm.MutationRate.ShouldBe(0.17);
        algorithm.Elites.ShouldBe(3);
    }

    [Fact]
    public void GetBuilder_UsesCurrentDefaultParametersForUnconfiguredSettings()
    {
        var builder = GeneticAlgorithm.GetBuilder(
          new UniformDistributedCreator { Maximum = 3.0 },
          new SinglePointCrossover(),
          new GaussianMutator(0.1, 0.1));

        var algorithm = builder.Build();

        algorithm.PopulationSize.ShouldBe(100);
        algorithm.MutationRate.ShouldBe(0.05);
        algorithm.Elites.ShouldBe(1);
        algorithm.Selector.ShouldBeOfType<TournamentSelector<RealVector>>();
        algorithm.Evaluator.ShouldNotBeNull();
    }
}
