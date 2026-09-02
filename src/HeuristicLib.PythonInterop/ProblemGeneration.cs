using System.Collections.Concurrent;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MachineLearning;
using HEAL.HeuristicLib.Problems.TestFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.BBoB;
using HEAL.HeuristicLib.Problems.TestFunctions.MetaFunctions;
using HEAL.HeuristicLib.Problems.TestFunctions.ZDT;
using HEAL.HeuristicLib.Problems.TravelingSalesman;
using HEAL.HeuristicLib.Random;
using RastriginFunction = HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives.RastriginFunction;
using SphereFunction = HEAL.HeuristicLib.Problems.TestFunctions.SingleObjectives.SphereFunction;

namespace HEAL.HeuristicLib.PythonInterop;

public class ProblemGeneration
{
    public delegate double[] CustomFunc(RealVector solution);

    private static readonly ConcurrentDictionary<string, ITravelingSalesmanProblemData> TSPCache = [];

    private static readonly ConcurrentDictionary<(string, double), RegressionData> SymRegCache = [];

    public static TravelingSalesmanProblem CreateTravellingSalesmanProblem(string file)
    {
        var cdata = TSPCache.GetOrAdd(file, valueFactory: s =>
        {
            var t = TsplibTspInstanceProvider.LoadData(s);

            return t.Distances != null || (t.Coordinates?.Length ?? 0) <= 1000 ? t.ToDistanceMatrixData() : t.ToCoordinatesData();
        });

        return new TravelingSalesmanProblem(cdata);
    }

    public static SymbolicRegressionProblem CreateSymbolicRegressionProblem(string file, SymRegExperimentParameters parameters)
    {
        var data = SymRegCache.GetOrAdd((file, parameters.TrainingSplit), static key => PythonRegressionData.ReadCsv(key.Item1, key.Item2));
        var operations = new OperationSymbol[]
        {
            Symbols.Addition,
            Symbols.Subtraction,
            Symbols.Multiplication,
            Symbols.Division,
            Symbols.SquareRoot,
            Symbols.Logarithm
        };
        var constant = new EvolvableConstantSymbol(
            new UniformDoubleDistribution(-20.0, 20.0),
            new ChooseNumericPerturbation(
            [
                (new AdditiveNumericPerturbation(new NormalDoubleDistribution(0.0, 1.0)), 0.5),
                (new MultiplicativeNumericPerturbation(new NormalDoubleDistribution(0.0, 0.03)), 0.5)
            ]));
        var searchSpace = new ExpressionTreeSearchSpace(
            parameters.TreeLength,
            parameters.TreeDepth,
            operations,
            data.Inputs.Columns.Select(column => column.Name),
            [constant]);

        return new SymbolicRegressionProblem(
            data,
            Metrics.RMSE.ToFinite(),
            ExpressionMetrics.Length,
            searchSpace,
            parameters.UseLinearScaling);
    }

    public static TestFunctionProblem CreateTestFunctionProblem(int function, int dimension, int instance) => new(BBoBSuite.GetProblem(function, dimension, instance));

    public static MultiObjectiveTestFunctionProblem SphereRastriginProblem(int dimensions, double min, double max, double shift)
    {
        var testFunction = new CombinedGradientTestFunction(
            new ShiftedGradientTestFunction(-shift, new SphereFunction(dimensions)),
            new RastriginFunction(dimensions));
        var encoding = new BoundedRealVectorSearchSpace(dimensions, min, max);
        var prob = new MultiObjectiveTestFunctionProblem(testFunction, encoding);

        return prob;
    }

    public static MultiObjectiveTravellingSalesmanProblem CreateMultiObjectiveRealVectorTravellingSalesmanProblem(params string[] files) => new(files.Select(CreateTravellingSalesmanProblem).ToArray());

    public class PythonProblem(CustomFunc cfunc, int dimensions, double min, double max, bool[] maximization)
        : RealVectorProblem<PythonProblem>(MultiObjective.Create(maximization), new BoundedRealVectorSearchSpace(dimensions, min, max))
    {
        public override ObjectiveVector Evaluate(RealVector solution, IRandomNumberGenerator random) => cfunc(solution);
    }

    public static MultiObjectiveTestFunctionProblem CreateZdt(int number, int dim)
    {
        return (number switch
        {
            1 => new Zdt1(dim).AsProblem(),
            2 => new Zdt2(dim).AsProblem(),
            3 => new Zdt3(dim).AsProblem(),
            4 => new Zdt4(dim).AsProblem(),
            6 => new Zdt6(dim).AsProblem(),
            _ => throw new ArgumentException($"Invalid ZDT function number: {number}")
        });
    }

    public class MultiObjectiveTravellingSalesmanProblem(TravelingSalesmanProblem[] tsps) :
        RealVectorProblem<MultiObjectiveTravellingSalesmanProblem>(MultiObjective.Create(tsps.Select(_ => false).ToArray()),
            new BoundedRealVectorSearchSpace(
                tsps.Max(x => x.ProblemData.NumberOfCities),
                [0.0], [1.0]))
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
