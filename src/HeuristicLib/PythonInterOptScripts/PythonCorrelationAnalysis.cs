using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.NSGA2;
using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Encodings.RealVector.Creators;
using HEAL.HeuristicLib.Encodings.RealVector.Crossovers;
using HEAL.HeuristicLib.Encodings.RealVector.Mutators;
using HEAL.HeuristicLib.Operators.Analyzer;
using HEAL.HeuristicLib.Operators.Crossover;
using HEAL.HeuristicLib.Operators.Evaluator;
using HEAL.HeuristicLib.Operators.Mutator;
using HEAL.HeuristicLib.Operators.Terminator;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public static class PythonCorrelationAnalysis {
  public static double[] GetPseudoCorrelations(IReadOnlyList<RealVector> solutions, MultiObjectiveTestFunctionProblem problem, double[]? delta = null, int count = 0, int seed = 0) {
    var gradcal = (IMultiObjectiveGradientTestFunction)problem.TestFunction;
    var res = new double[solutions.Count];
    for (var i = 0; i < solutions.Count; i++) {
      var grads = gradcal.EvaluateGradient(solutions[i]);
      res[i] = 0.5 - (Math.Abs(grads[0].Angle(grads[1])) / Math.PI);
    }

    return res;
  }

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

  public static void RunCorrelationNsga2(GenerationCallback callback, int generations, int populationSize, MultiObjectiveTestFunctionProblem problem, int seed = 0) {
    //var prob = SphereRastriginProblem(dimensions, min, max);

    var proto = Nsga2.GetBuilder(
      new UniformDistributedCreator(),
      new SelfAdaptiveSimulatedBinaryCrossover { Eta = 15 }.WithProbability(0.9),
      new PolynomialMutator().WithRate(0.9));

    proto.Terminator = new AfterIterationsTerminator<RealVector>(generations);
    proto.RandomSeed = seed;
    proto.PopulationSize = populationSize;
    proto.MutationRate = 1;

    _ = FuncAnalysis.Create(proto, (_, iterationResult) => callback(iterationResult, problem));
    proto.Execute(problem, problem.SearchSpace, new SystemRandomNumberGenerator(seed));
  }

  public static MultiObjectiveTestFunctionProblem SphereRastriginProblem(int dimensions, double min, double max) {
    var testFunction = new CombinedGradientTestFunction(
      new ShiftedGradientTestFunction(-0.5, new SphereFunction(dimensions)),
      new RastriginFunction(dimensions));
    var encoding = new RealVectorEncoding(dimensions, min, max);
    var prob = new MultiObjectiveTestFunctionProblem(testFunction, encoding);
    return prob;
  }
}
