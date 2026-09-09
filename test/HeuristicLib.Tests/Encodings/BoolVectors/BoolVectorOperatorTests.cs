using HEAL.HeuristicLib.Encodings.BoolVectors;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

// Deliberately not mirroring the folder: a HEAL.HeuristicLib.Tests.Encodings namespace would shadow
// HEAL.HeuristicLib.Encodings for every unqualified reference elsewhere in this assembly.
namespace HEAL.HeuristicLib.Tests.Operators.BoolVectors;

public class BoolVectorOperatorTests
{
    /// <summary>
    /// The encoding is only usable if a complete algorithm can be assembled from operators the library ships, so this
    /// runs one rather than checking the three in isolation.
    /// </summary>
    [Fact]
    public async Task GeneticAlgorithmOverBoolVectors_AssemblesFromShippedOperatorsAndImproves()
    {
        var searchSpace = new BoolVectorSearchSpace(24);
        var problem = FuncProblem.Create(
            evaluateFunc: (BoolVector candidate) => -candidate.TrueCount(),
            encoding: searchSpace,
            objective: SingleObjective.Minimize);

        var algorithm = GeneticAlgorithm.Create(
            new RandomBoolVectorCreator(),
            new BitUniformCrossover(),
            new BitFlipMutator(),
            populationSize: 40,
            maximumGenerations: 60);

        var finalState = await algorithm.CompleteAsync(
            problem,
            RandomNumberGenerator.Create(seed: 42),
            ct: TestContext.Current.CancellationToken);

        var best = finalState.Population.EvaluatedCandidates
            .MinBy(candidate => candidate.ObjectiveVector, problem.Objective.TotalOrderComparer)!;

        best.Candidate.Count.ShouldBe(24);
        searchSpace.Contains(best.Candidate).ShouldBeTrue();
        best.Candidate.TrueCount().ShouldBeGreaterThan(12);
    }

    [Fact]
    public void RandomBoolVectorCreator_ProducesTheSearchSpaceLength()
    {
        var searchSpace = new BoolVectorSearchSpace(16);
        var random = RandomNumberGenerator.Create(seed: 7);

        var candidate = RandomBoolVectorCreator.Create(random, searchSpace);

        candidate.Count.ShouldBe(16);
        searchSpace.Contains(candidate).ShouldBeTrue();
    }

    [Fact]
    public void BitFlipMutator_PreservesLengthAndCanChangeTheSetCount()
    {
        var random = RandomNumberGenerator.Create(seed: 11);
        var parent = new BoolVector(Enumerable.Repeat(false, 32));

        var mutant = BitFlipMutator.Mutate(parent, random, flipProbability: 0.5);

        mutant.Count.ShouldBe(32);
        mutant.TrueCount().ShouldBeGreaterThan(0);
    }

    /// <remarks>
    /// The two bool vector mutators differ in exactly this, which is why both exist and why the pre-flight check can
    /// tell them apart.
    /// </remarks>
    [Fact]
    public void TheTwoMutators_DisagreeOnCardinality()
    {
        ICandidateInvariant<BoolVector> cardinality = new BoolVectorCardinality(4);

        new BitSwapMutator().Ensures(cardinality).ShouldBe(true);
        new BitFlipMutator().Ensures(cardinality).ShouldBe(false);
    }
}
