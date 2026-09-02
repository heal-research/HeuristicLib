using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Numerics;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Random;
using UniformDistributedCreator = HEAL.HeuristicLib.Encodings.RealVectors.UniformDistributedCreator;

namespace HEAL.HeuristicLib.PythonInterop;

public static class PythonCorrelationAnalysis
{
    public delegate void GenerationCallback(PopulationState<RealVector> current, IProblem<RealVector, BoundedRealVectorSearchSpace> problem);

    public static double[] GetPseudoCorrelations(IReadOnlyList<RealVector> candidates, MultiObjectiveTestFunctionProblem problem)
    {
        var gradcal = (IMultiObjectiveGradientTestFunction)problem.TestFunction;
        var res = new double[candidates.Count];
        for (var i = 0; i < candidates.Count; i++)
        {
            var grads = gradcal.EvaluateGradient(candidates[i]);
            res[i] = 0.5 - Math.Abs(grads[0].Angle(grads[1])) / Math.PI;
        }

        return res;
    }

    public static double[] GetCorrelations(IReadOnlyList<RealVector> candidates, IProblem<RealVector, BoundedRealVectorSearchSpace> problem, double[] delta, int count, int seed = 0)
    {
        var random = RandomNumberGenerator.Create(seed);
        var evaluator = new ProblemEvaluator<RealVector>();
        var res = new double[candidates.Count];
        var sigma = RealVector.Create(delta);
        Parallel.ForEach(candidates, (vector, state, i) =>
        {
            var r = random.Fork((int)i);
            var n = Enumerable.Range(0, count).Select(_ => NextSphere(r, vector, sigma, vector.Count, false)).ToArray();
            var objectives = evaluator.Evaluate(n, r, problem.SearchSpace, problem).ToArray();
            var d = DescriptiveStatistics.Covariance(
              objectives.Select(x => x[0]).ToArray(),
              objectives.Select(x => x[1]).ToArray()).Correlation;
            res[i] = d;
        });

        return res;
    }

    public static RealVector NextSphere(IRandomNumberGenerator uniformRandom, RealVector mu, RealVector sigma, int dim, bool surface = true)
    {
        var d = new RealVector(Enumerable.Range(0, dim).Select(_ => uniformRandom.NextNormal()));
        if (surface)
        {
            d /= d.Norm();
        }

        d *= sigma;
        d += mu;
        return d;
    }

    public static ObjectiveVector[] GetQualities(IReadOnlyList<RealVector> candidates, IProblem<RealVector, BoundedRealVectorSearchSpace> problem)
    {
        var random = RandomNumberGenerator.Create(42);
        var evaluator = new ProblemEvaluator<RealVector>();

        return evaluator.Evaluate(candidates, random, problem.SearchSpace, problem).ToArray();
    }

    public static ExperimentResult<RealVector> RunCorrelationNsga2(GenerationCallback? callback, int generations, int populationSize, IProblem<RealVector, BoundedRealVectorSearchSpace> problem, int seed = 0)
    {
        var res = PythonGenealogyAnalysis.RunAlgorithmConfigurable(problem, callback is null ? null : r => callback(r, problem),
          new TestFunctionExperimentParameters
          {
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
