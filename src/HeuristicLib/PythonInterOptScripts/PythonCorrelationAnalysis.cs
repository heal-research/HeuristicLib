using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.NSGA2;
using HEAL.HeuristicLib.Encodings.Vectors;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Creator.RealVectors;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Crossover.RealVectors;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Mutator.RealVectors;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public static class PythonCorrelationAnalysis {
  public static double[] GetCorrelations(IReadOnlyList<RealVector> solutions, RealVectorProblem problem, double[] delta, int count, int seed = 0) {
    var random = new SystemRandomNumberGenerator(seed);
    var evaluator = new DirectEvaluator<RealVector>();
    return solutions.ParallelSelect(random, (i, solution, r) => {
      var n = Enumerable.Range(0, count).Select(x =>
        NormalDistributedRandomPolar.NextSphere(r, solution.ToArray(), delta, solution.Count, false)).ToArray();
      var objectives = evaluator.Evaluate(n, r, problem.SearchSpace, problem);
      var d = OnlinePearsonsRCalculator.Calculate(
        objectives.Select(x => x[0]),
        objectives.Select(x => x[1]),
        out _);
      return d;
    }).ToArray();
  }

  public static ObjectiveVector[] GetQualities(IReadOnlyList<RealVector> solutions, RealVectorProblem problem) {
    var random = new SystemRandomNumberGenerator();
    var evaluator = new DirectEvaluator<RealVector>();
    return evaluator.Evaluate(solutions, random, problem.SearchSpace, problem).ToArray();
  }

  public delegate void GenerationCallback(PopulationIterationResult<RealVector> current, RealVectorProblem problem);

  public static void RunCorrelationNsga2(GenerationCallback callback, int generations, int populationSize,
                                         int dimensions = 10, double min = -5, double max = 5, int seed = 0) {
    var prob = SphereRastriginProblem(dimensions, min, max);

    var proto = Nsga2.GetBuilder(
      new UniformDistributedCreator(),
      new SelfAdaptiveSimulatedBinaryCrossover { Eta = 15 }.WithProbability(0.9),
      new PolynomialMutator().WithRate(0.9));

    proto.Terminator = new AfterIterationsTerminator<RealVector>(generations);
    proto.RandomSeed = seed;
    proto.PopulationSize = populationSize;
    proto.MutationRate = 1;

    _ = FuncAnalysis.Create(proto, (_, iterationResult) => callback(iterationResult, prob));
    proto.Execute(prob, prob.SearchSpace, new SystemRandomNumberGenerator(seed));
  }

  public static MultiObjectiveTestFunctionProblem SphereRastriginProblem(int dimensions, double min, double max) {
    var testFunction = new CombinedTestFunction(
      new SphereFunction(dimensions).Shifted(-0.1),
      new RastriginFunction(dimensions));
    var encoding = new RealVectorEncoding(dimensions, min, max);
    var prob = new MultiObjectiveTestFunctionProblem(testFunction, encoding);
    return prob;
  }
}
