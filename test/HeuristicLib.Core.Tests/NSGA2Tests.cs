// using HEAL.HeuristicLib.Algorithms;
// using HEAL.HeuristicLib.Algorithms.NSGA2;
// using HEAL.HeuristicLib.Encodings;
// using HEAL.HeuristicLib.Genotypes;
// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Operators.RealVectorOperators;
// using HEAL.HeuristicLib.Optimization;
// using HEAL.HeuristicLib.Problems;
// using HEAL.HeuristicLib.Problems.TestFunctions;
//
// namespace HEAL.HeuristicLib.Tests;
//
// public class NSGA2Tests {
//   [Fact]
//   public Task NSGA2_SolveTestFunction() {
//     var creator = new UniformDistributedCreator();
//     var crossover = new SinglePointCrossover();
//     var mutator = new GaussianMutator(0.1, 0.1);
//     var replacement = new ElitismReplacer<RealVector, RealVectorEncoding>(0);
//     var terminator = Terminator.OnGeneration<RealVector, RealVectorEncoding, NSGA2Result<RealVector>>(5);
//
//     var nsga = new NSGA2<RealVector, RealVectorEncoding>(
//       populationSize: 5, 
//       creator: creator, crossover: crossover, mutator: mutator, mutationRate: 0.5,
//       replacer: replacement,
//       randomSeed: 42, terminator: terminator
//     );
//     var problem = new MultiObjectiveTestFunctionProblem(new ZDT1(dimension: 3));
//
//     var result = nsga.SolvePareto(problem);
//     
//     return Verify(result)
//       .IgnoreMembersWithType<TimeSpan>();
//   }
// }

