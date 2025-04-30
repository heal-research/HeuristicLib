using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.NSGA2;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;

namespace HEAL.HeuristicLib.Tests;

public class NSGA2Tests {
  [Fact]
  public Task NSGA2_SolveTestFunction() {
    var creator = new UniformDistributedCreator();
    var crossover = new SinglePointCrossover();
    var mutator = new GaussianMutator(0.1, 0.1);
    var replacement = new ElitismReplacer(0);
    var terminator = Terminator.OnGeneration<NSGA2Result<RealVector>>(5);

    var nsga = new NSGA2<RealVector, RealVectorSearchSpace>(
      populationSize: 5, 
      creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
      replacer: replacement,
      randomSeed: 42, terminator: terminator
    );
    var problem = new MultiObjectiveTestFunctionProblem(new ZDT1(dimension: 3));

    var result = nsga.SolvePareto(problem);
    
    return Verify(result)
      .IgnoreMembersWithType<TimeSpan>();
  }
}
