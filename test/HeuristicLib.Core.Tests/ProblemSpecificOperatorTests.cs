// using HEAL.HeuristicLib.Algorithms.GeneticAlgorithm;
// using HEAL.HeuristicLib.Encodings;
// using HEAL.HeuristicLib.Genotypes;
// using HEAL.HeuristicLib.Operators;
// using HEAL.HeuristicLib.Operators.PermutationOperators;
// using HEAL.HeuristicLib.Problems.TravelingSalesman;
//
// namespace HEAL.HeuristicLib.Core.Tests;
//
// public class ProblemSpecificOperatorTests {
//
//   [Fact]
//   public void TestOperatorCompatability() {
//     var problemAgnosticCreator = new RandomPermutationCreator();
//     var problemSpecificCreator = new TspCreator();
//     
//     ProblemAgnosticMethod(problemAgnosticCreator);
//     //ProblemAgnosticMethod(problemSpecificCreator); // should not compile
//     
//     ProblemSpecificMethod(problemAgnosticCreator); // should do with implicit conversion
//     ProblemSpecificMethod(new ProblemSpecificCreator<Permutation, PermutationEncoding, TravelingSalesmanProblem>(problemAgnosticCreator)); // should do with implicit conversion
//     ProblemSpecificMethod(problemSpecificCreator);
//   }
//   private static void ProblemAgnosticMethod(Creator<Permutation, PermutationEncoding> creator) {}
//   private static void ProblemSpecificMethod(Creator<Permutation, PermutationEncoding, TravelingSalesmanProblem> creator) {}
//   
//   [Fact]
//   public Task GeneticAlgorithm_ExecuteWithTspSample() {
//     var ga = new GeneticAlgorithm<Permutation, PermutationEncoding, TravelingSalesmanProblem>(
//       populationSize: 5, 
//       creator: new TspCreator(), 
//       crossover: new OrderCrossover(), mutator: new InversionMutator(), mutationRate: 0.5,
//       selector: new RandomSelector(), replacer: new ElitismReplacer<Permutation, PermutationEncoding>(0),
//       randomSeed: 42, terminator: Terminator.OnGeneration<Permutation, PermutationEncoding, PopulationResult<Permutation>>(5)
//     );
//
//     var problem = TravelingSalesmanProblem.CreateDefault();
//     
//     var finalState = ga.CreateExecution(problem).Execute();
//     
//     return Verify(finalState)
//       .IgnoreMembersWithType<TimeSpan>();
//   }
// }

