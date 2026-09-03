using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.ApiUsageSpecs.Usage;

/// <summary>
/// Pins which capabilities each of the two algorithm interfaces carries. The negative half — that the execution
/// surface is genuinely absent from the one-argument form — cannot be written here, because a spec that fails to
/// compile is not a spec; it lives in AlgorithmInterfaceCapabilityTests.
/// </summary>
public class AlgorithmInterfaceCapabilitySpecs
{
    /// <summary>
    /// The reason the second interface exists: a caller who did not build the algorithm, and therefore does not know
    /// its concrete type, can still run it and receives a typed final state.
    /// </summary>
    [Fact]
    public async Task AnAlgorithmHeldByItsInterface_StillRuns()
    {
        var problem = new TravelingSalesmanProblem();
        IAlgorithm<Permutation, PopulationState<Permutation>> algorithm =
            GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        var finalState = await algorithm.CompleteAsync(
            problem,
            RandomNumberGenerator.Create(seed: 42),
            ct: TestContext.Current.CancellationToken);

        finalState.Population.Count().ShouldBe(20);
    }

    /// <summary>
    /// What the one-argument form is for. A genetic algorithm yields a population and a hill climber a single
    /// solution, so no single state describes both; dropping the state is what lets them share a collection.
    /// </summary>
    [Fact]
    public void AlgorithmsThatYieldDifferentStates_ShareOneCollection()
    {
        var problem = new TravelingSalesmanProblem();
        var geneticAlgorithm = GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);
        var hillClimber = new HillClimber<Permutation>
        {
            Creator = new RandomPermutationCreator(),
            Mutator = new InversionMutator()
        };

        List<IAlgorithm<Permutation>> algorithms = [geneticAlgorithm, hillClimber];

        algorithms.Count.ShouldBe(2);
    }

    /// <summary>
    /// Resolution works from the one-argument form: naming the four run types at the call site supplies what the
    /// interface does not. This is why an algorithm can be held erased and still take part in a run.
    /// </summary>
    [Fact]
    public void AnAlgorithmHeldWithoutItsState_StillResolves()
    {
        var problem = new TravelingSalesmanProblem();
        IAlgorithm<Permutation> algorithm =
            GeneticAlgorithm.For(problem, populationSize: 20, maximumGenerations: 5);

        var resolved = new ExecutionInstanceRegistry()
            .Resolve<Permutation, PermutationSearchSpace, TravelingSalesmanProblem, PopulationState<Permutation>>(algorithm);

        resolved.ShouldNotBeNull();
    }
}
