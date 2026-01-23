using System.Collections.Concurrent;
using HEAL.HeuristicLib.Encodings.Permutation;
using HEAL.HeuristicLib.Encodings.RealVector;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.BBoB;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Problems.TravelingSalesman.InstanceLoading;
using HEAL.HeuristicLib.Random;
using static HEAL.HeuristicLib.PythonInterOptScripts.PythonGenealogyAnalysis;
using RastriginFunction = HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives.RastriginFunction;
using SphereFunction = HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives.SphereFunction;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class ProblemGeneration {
  private static readonly ConcurrentDictionary<string, ITravelingSalesmanProblemData> TSPCache = [];

  public static TravelingSalesmanProblem CreateTravellingSalesmanProblem(string file) {
    var cdata = TSPCache.GetOrAdd(file, s => {
      var t = TsplibTspInstanceProvider.LoadData(s);
      return t.Distances != null || (t.Coordinates?.Length ?? 0) <= 1000 ? t.ToDistanceMatrixData() : t.ToCoordinatesData();
    });
    return new TravelingSalesmanProblem(cdata);
  }

  private static readonly ConcurrentDictionary<(string, double), RegressionProblemData> SymRegCache = [];

  public static SymbolicRegressionProblem CreateSymbolicRegressionProblem(string file, SymRegExperimentParameters parameters) {
    var problemData = SymRegCache.GetOrAdd((file, parameters.TrainingSplit), key => RegressionCsvInstanceProvider.ImportData(key.Item1, key.Item2));
    var problem = new SymbolicRegressionProblem(problemData, new RootMeanSquaredErrorEvaluator(), new TreeLengthEvaluator()) {
      SearchSpace = {
        TreeDepth = parameters.TreeDepth,
        TreeLength = parameters.TreeLength
      },
      ParameterOptimizationIterations = parameters.ParameterOptimizationIterations
    };
    var root = problem.SearchSpace.Grammar.AddLinearScaling();
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols(root, new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables });
    return problem;
  }

  public static TestFunctionProblem CreateTestFunctionProblem(int function, int dimension, int instance) => new(BBoBSuite.GetProblem(function, dimension, instance));

  public delegate double[] CustomFunc(RealVector solution);

  public class PythonProblem(CustomFunc cfunc, int dimensions, double min, double max, bool[] maximization)
    : RealVectorProblem(MultiObjective.Create(maximization), new RealVectorEncoding(dimensions, min, max)) {
    public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) => cfunc(solution);
  }

  public static MultiObjectiveTestFunctionProblem SphereRastriginProblem(int dimensions, double min, double max, double shift) {
    var testFunction = new CombinedGradientTestFunction(
      new ShiftedGradientTestFunction(-shift, new SphereFunction(dimensions)),
      new RastriginFunction(dimensions));
    var encoding = new RealVectorEncoding(dimensions, min, max);
    var prob = new MultiObjectiveTestFunctionProblem(testFunction, encoding);
    return prob;
  }

  public class MultiObjectiveTravellingSalesmanProblem(TravelingSalesmanProblem[] tsps) :
    RealVectorProblem(MultiObjective.Create(tsps.Select(_ => false).ToArray()),
      new RealVectorEncoding(
        tsps.Max(x => x.ProblemData.NumberOfCities),
        new RealVector(0.0), new RealVector(1.0))) {
    private readonly TravelingSalesmanProblem[] tsps = tsps.ToArray();

    public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) {
      return tsps.Select(x => EvaluateSolutionFromRealVector(x, solution, random)[0]).ToArray();
    }

    private static ObjectiveVector EvaluateSolutionFromRealVector(TravelingSalesmanProblem problem, RealVector solution, IRandomNumberGenerator random) {
      var p = new Permutation(Enumerable.Range(0, problem.ProblemData.NumberOfCities).OrderBy(x => solution[x]));
      return problem.Evaluate(p, random);
    }
  }

  public static MultiObjectiveTravellingSalesmanProblem CreateMultiObjectiveRealVectorTravellingSalesmanProblem(params string[] files) {
    return new MultiObjectiveTravellingSalesmanProblem(files.Select(CreateTravellingSalesmanProblem).ToArray());
  }
}
