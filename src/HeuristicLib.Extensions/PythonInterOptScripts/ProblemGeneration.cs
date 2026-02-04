using System.Collections.Concurrent;
using HEAL.HeuristicLib.Genotypes.Vectors;
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
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Vectors;
using RastriginFunction = HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives.RastriginFunction;
using SphereFunction = HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives.SphereFunction;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public class ProblemGeneration
{
  public delegate double[] CustomFunc(RealVector solution);

  private static readonly ConcurrentDictionary<string, ITravelingSalesmanProblemData> TSPCache = [];

  private static readonly ConcurrentDictionary<(string, double), RegressionProblemData> SymRegCache = [];

  public static TravelingSalesmanProblem CreateTravellingSalesmanProblem(string file)
  {
    var cdata = TSPCache.GetOrAdd(file, valueFactory: s => {
      var t = TsplibTspInstanceProvider.LoadData(s);

      return t.Distances != null || (t.Coordinates?.Length ?? 0) <= 1000 ? t.ToDistanceMatrixData() : t.ToCoordinatesData();
    });

    return new TravelingSalesmanProblem(cdata);
  }

  public static SymbolicRegressionProblem CreateSymbolicRegressionProblem(string file, SymRegExperimentParameters parameters)
  {
    var problemData = SymRegCache.GetOrAdd((file, parameters.TrainingSplit), valueFactory: key => RegressionCsvInstanceProvider.ImportData(key.Item1, key.Item2));
    var problem = new SymbolicRegressionProblem(problemData, new RootMeanSquaredErrorEvaluator(), new TreeLengthEvaluator()) {
      SearchSpace = {
        TreeDepth = parameters.TreeDepth,
        TreeLength = parameters.TreeLength
      },
      ParameterOptimizationIterations = parameters.ParameterOptimizationIterations
    };
    var root = problem.SearchSpace.Grammar.AddLinearScaling();
    var symbols = new Symbol[] { new Addition(), new Subtraction(), new Multiplication(), new Division(), new Number(), new SquareRoot(), new Logarithm(), new Variable { VariableNames = problemData.InputVariables } };
    problem.SearchSpace.Grammar.AddFullyConnectedSymbols(root, symbols);

    return problem;
  }

  public static TestFunctionProblem CreateTestFunctionProblem(int function, int dimension, int instance) => new(BBoBSuite.GetProblem(function, dimension, instance));

  public static MultiObjectiveTestFunctionProblem SphereRastriginProblem(int dimensions, double min, double max, double shift)
  {
    var testFunction = new CombinedGradientTestFunction(
      new ShiftedGradientTestFunction(-shift, new SphereFunction(dimensions)),
      new RastriginFunction(dimensions));
    var encoding = new RealVectorSearchSpace(dimensions, min, max);
    var prob = new MultiObjectiveTestFunctionProblem(testFunction, encoding);

    return prob;
  }

  public static MultiObjectiveTravellingSalesmanProblem CreateMultiObjectiveRealVectorTravellingSalesmanProblem(params string[] files) => new(files.Select(CreateTravellingSalesmanProblem).ToArray());

  public class PythonProblem(CustomFunc cfunc, int dimensions, double min, double max, bool[] maximization)
    : RealVectorProblem(MultiObjective.Create(maximization), new RealVectorSearchSpace(dimensions, min, max))
  {
    public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) => cfunc(solution);
  }

  public class MultiObjectiveTravellingSalesmanProblem(TravelingSalesmanProblem[] tsps) :
    RealVectorProblem(MultiObjective.Create(tsps.Select(_ => false).ToArray()),
      new RealVectorSearchSpace(
        tsps.Max(x => x.ProblemData.NumberOfCities),
        new RealVector(0.0), new RealVector(1.0)))
  {
    private readonly TravelingSalesmanProblem[] tsps = tsps.ToArray();

    public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) => tsps.Select(x => EvaluateSolutionFromRealVector(x, solution, random)[0]).ToArray();

    private static ObjectiveVector EvaluateSolutionFromRealVector(TravelingSalesmanProblem problem, RealVector solution, IRandomNumberGenerator random)
    {
      var p = new Permutation(Enumerable.Range(0, problem.ProblemData.NumberOfCities).OrderBy(x => solution[x]));

      return problem.Evaluate(p, random);
    }
  }
}
