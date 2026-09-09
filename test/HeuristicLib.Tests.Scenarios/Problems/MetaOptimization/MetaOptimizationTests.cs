using HEAL.HeuristicLib.Encodings.IntegerVectors;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems.MetaOptimization;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.Tests.Scenarios.Problems.MetaOptimization;

public class MetaOptimizationTests
{
    [Fact]
    public void TestGAwithMutators()
    {
        //setup
        var problem = new TestFunctionProblem(new AckleyFunction(20));
        var ga = GeneticAlgorithm.Create(
          new UniformDistributedCreator(),
          new SimulatedBinaryCrossover(),
          new GaussianMutator(0.5, 0.5),
          selector: TournamentSelector.For(problem, tournamentSize: 2),
          evaluator: new ProblemEvaluator<RealVector>(),
          populationSize: 100,
          mutationRate: 0.25);

        //build meta problem (test some mutators
        var b = new MetaOptimizationProblemExamples.MetaOptimizationSearchSpaceBuilder();
        var mutatorExtractor = b.AddChoiceParameter(
            new Mutator<RealVector, BoundedRealVectorSearchSpace>[] {
                new GaussianMutator(0.5, 0.5),
                new GaussianMutator(0.5, 1),
                new PolynomialMutator(),
                new PolynomialMutator { AtLeastOnce = true }
            });
        var metaSpace = b.Build();
        var metaProblem = problem.AsMetaProblem<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, PopulationState<RealVector>>(metaSpace, x =>
        {
            var alg = ga with { Mutator = mutatorExtractor(x) };
            return alg with { MaximumGenerations = 1000 / alg.PopulationSize }; // now with fancy cross dependent parameters
        });

        //build meta alg
        var hc = HillClimber.Create(
          creator: metaSpace.CombineCreators(
            new UniformDistributedCreator(),
            new Encodings.IntegerVectors.UniformDistributedCreator()), //operator name clash ...
          mutator: metaSpace.CombineMutator(
            new PolynomialMutator(),
            new UniformOnePositionMutator()),
          batchSize: 4);
        hc = hc with
        {
            Evaluator = hc.Evaluator
                .AsRepeated(11, ObjectiveVectorAggregation.Median)
                .WithCache()
        };

        //run meta alg
        var finalState = hc
            .WithMaxIterations(5)
            .Complete(metaProblem, RandomNumberGenerator.Create(42), ct: TestContext.Current.CancellationToken);

        metaProblem.SearchSpace.Contains(finalState.EvaluatedCandidate.Candidate).ShouldBeTrue();
        finalState.EvaluatedCandidate.ObjectiveVector.Count.ShouldBe(1);
        double.IsFinite(finalState.EvaluatedCandidate.ObjectiveVector[0]).ShouldBeTrue();
    }
}
