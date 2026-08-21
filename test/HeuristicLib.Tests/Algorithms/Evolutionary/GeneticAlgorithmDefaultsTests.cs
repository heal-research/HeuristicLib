using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Tests.Algorithms.Evolutionary;

public class GeneticAlgorithmDefaultsTests
{
    [Fact]
    public void Record_TakesSharedDefaults_WhenOnlyOperatorsAreConfigured()
    {
        var algorithm = CreateMinimallyConfiguredAlgorithm();

        algorithm.PopulationSize.ShouldBe(GeneticAlgorithmDefaults.PopulationSize);
        algorithm.MutationRate.ShouldBe(GeneticAlgorithmDefaults.MutationRate);
        algorithm.Elites.ShouldBe(GeneticAlgorithmDefaults.Elites);
        algorithm.Selector.ShouldBeOfType<TournamentSelector<RealVector>>()
                 .TournamentSize.ShouldBe(GeneticAlgorithmDefaults.TournamentSize);
        algorithm.Evaluator.ShouldNotBeNull();
    }

    private static GeneticAlgorithm<RealVector, RealVectorSearchSpace> CreateMinimallyConfiguredAlgorithm() =>
        new()
        {
            Creator = new UniformDistributedCreator { Maximum = 3.0 },
            Crossover = new SinglePointCrossover(),
            Mutator = new GaussianMutator(0.1, 0.1)
        };
}
