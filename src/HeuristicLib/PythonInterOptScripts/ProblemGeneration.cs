using System.Collections.Concurrent;
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
using RastriginFunction = HEAL.HeuristicLib.Problems.TestFunctions.RastriginFunction;
using SphereFunction = HEAL.HeuristicLib.Problems.TestFunctions.SphereFunction;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class ProblemGeneration {
  private static readonly ConcurrentDictionary<string, ITravelingSalesmanProblemData> TSPCache = [];

  public static TravelingSalesmanProblem CreateTestTravellingSalesmanProblem(string file) {
    var cdata = TSPCache.GetOrAdd(file, s => {
      var t = TsplibTspInstanceProvider.LoadData(s);
      return t.Distances != null || (t.Coordinates?.Length ?? 0) <= 1000 ? t.ToDistanceMatrixData() : t.ToCoordinatesData();
    });
    return new TravelingSalesmanProblem(cdata);
  }

  private static readonly ConcurrentDictionary<(string, double), RegressionProblemData> SymRegCache = [];

  public static SymbolicRegressionProblem CreateTestSymbolicRegressionProblem(string file, SymRegExperimentParameters parameters) {
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
}
