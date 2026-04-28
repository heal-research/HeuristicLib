using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.LocalSearch;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.IntegerVectorMutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.MetaOptimization;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.Extensions.Tests.MetaOptimization;

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
      new GaussianMutator(0.5, 0.5), 0.25,
      new TournamentSelector<RealVector>(2),
      100,
      new DirectEvaluator<RealVector>());

    //build meta problem (test some mutators
    var b = new MetaOptimizationProblemExamples.MetaOptimizationSearchSpaceBuilder();
    var mutatorExtractor = b.AddChoiceParameter(
      new StatelessMutator<RealVector, RealVectorSearchSpace>[] {//TODO c# can not infer array type 
      new GaussianMutator(0.5, 0.5),
      new GaussianMutator(0.5, 1),
      new PolynomialMutator(),
      new PolynomialMutator(atLeastOnce: true)
    });
    var metaSpace = b.Build();
    var metaProblem = problem.AsMetaProblem(metaSpace, x => {
      var alg = ga with { Mutator = mutatorExtractor(x) };
      return alg.WithMaxIterations(1000 / alg.PopulationSize); // now with fancy cross dependent parameters
    });

    //build meta alg
    var hc = HillClimber.GetBuilder(
      creator: metaSpace.CombineCreators(
        new UniformDistributedCreator(),
        new Operators.Creators.IntegerVectorCreators.UniformDistributedCreator()), //operator name clash ...
      mutator: metaSpace.CombineMutator(
        new PolynomialMutator(),
        new UniformOnePositionManipulator()));
    hc.BatchSize = 4;
    hc.Evaluator = hc.Evaluator
                     .AsRepeated(11, objectives => objectives.Median(problem.Objective))
                     .WithCache();

    //run meta alg
    hc.Build()
      .WithMaxIterations(5)
      .RunToCompletion(metaProblem, RandomNumberGenerator.Create(42), ct: CancellationToken.None);
  }
}
