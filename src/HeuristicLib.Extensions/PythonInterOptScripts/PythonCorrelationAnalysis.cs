using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators.Creators.RealVectorCreators;
using HEAL.HeuristicLib.Operators.Crossovers;
using HEAL.HeuristicLib.Operators.Crossovers.RealVectorCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.RealVectorMutators;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.States;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public static class PythonCorrelationAnalysis
{
  public delegate void GenerationCallback(PopulationState<RealVector> current, RealVectorProblem problem);

  public static double[] GetPseudoCorrelations(IReadOnlyList<RealVector> solutions, MultiObjectiveTestFunctionProblem problem)
  {
    var gradcal = (IMultiObjectiveGradientTestFunction)problem.TestFunction;
    var res = new double[solutions.Count];
    for (var i = 0; i < solutions.Count; i++) {
      var grads = gradcal.EvaluateGradient(solutions[i]);
      res[i] = 0.5 - Math.Abs(grads[0].Angle(grads[1])) / Math.PI;
    }

    return res;
  }

  public static double[] GetCorrelations(IReadOnlyList<RealVector> solutions, RealVectorProblem problem, double[] delta, int count, int seed = 0)
  {
    var random = RandomNumberGenerator.Create(seed);
    var evaluator = new DirectEvaluator<RealVector>();
    var res = new double[solutions.Count];
    Parallel.ForEach(solutions, (vector, state, i) => {
      var r = random.Fork((int)i);
      var n = Enumerable.Range(0, count).Select(_ => NextSphere(r, vector, delta, vector.Count, false)).ToArray();
      var objectives = evaluator.Evaluate(n, r, problem.SearchSpace, problem);
      var d = OnlinePearsonsRCalculator.Calculate(
        objectives.Select(x => x[0]),
        objectives.Select(x => x[1]), out _);
      res[i] = d;
    });

    return res;
  }

  public static RealVector NextSphere(IRandomNumberGenerator uniformRandom, RealVector mu, RealVector sigma, int dim, bool surface = true)
  {
    var d = new RealVector(Enumerable.Range(0, dim).Select(_ => uniformRandom.NextGaussian()));
    if (surface) {
      d /= d.Norm();
    }

    d *= sigma;
    d += mu;
    return d.ToArray();
  }

  public static ObjectiveVector[] GetQualities(IReadOnlyList<RealVector> solutions, RealVectorProblem problem)
  {
    var random = RandomNumberGenerator.Create(42);
    var evaluator = new DirectEvaluator<RealVector>();

    return evaluator.Evaluate(solutions, random, problem.SearchSpace, problem).ToArray();
  }

  public static ExperimentResult<RealVector> RunCorrelationNsga2(GenerationCallback? callback, int generations, int populationSize, RealVectorProblem problem, int seed = 0)
  {
    var res = PythonGenealogyAnalysis.RunAlgorithmConfigurable(problem, callback is null ? null : r => callback(r, problem),
      new TestFunctionExperimentParameters {
        AlgorithmName = "nsga2",
        Creator = new UniformDistributedCreator(),
        Crossover = new SelfAdaptiveSimulatedBinaryCrossover { Eta = 15 }.WithRate(0.9),
        Mutator = new PolynomialMutator().WithRate(0.9),
        Iterations = generations,
        PopulationSize = populationSize,
        MutationRate = 1,
        Seed = seed,
        TrackPopulations = true
      });

    return res;
  }
}
