using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MachineLearning;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.PythonInterop;

/// <summary>
/// Symbolic regression problem that lets a Python callback augment the normal objectives of each expression.
/// </summary>
public class PythonInterOptEquationScoring(
    ObjectiveDirections objective,
    ExpressionTreeSearchSpace searchSpace,
    Func<ExpressionTree, ObjectiveVector, double[]> score)
    : SingleSolutionProblem<ExpressionTree, ExpressionTreeSearchSpace>(objective, searchSpace)
{
    private sealed record InnerProblemParameterFittingRefiner(NumericParameterFittingRefiner ChildRefiner)
        : SingleCandidateRefiner<ExpressionTree, ExpressionTreeSearchSpace, PythonInterOptEquationScoring>
    {
        public override ExpressionTree RefineCandidate(ExpressionTree candidate, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, PythonInterOptEquationScoring problem) =>
            ChildRefiner.RefineCandidate(candidate, random, searchSpace, problem.InnerProblem);
    }

    public required SymbolicRegressionProblem InnerProblem { get; init; }
    public int ParameterOptimizationIterations { get; init; } = 5;

    public override ObjectiveVector Evaluate(ExpressionTree solution, IRandomNumberGenerator random) =>
        score(solution, InnerProblem.Evaluate(solution));

    public static PythonInterOptEquationScoring DefaultConf(
        string file,
        int trainingRowCount,
        Func<ExpressionTree, ObjectiveVector, double[]> score,
        bool useLinearScaling = true,
        int parameterOptimizationIterations = 5)
    {
        var data = PythonRegressionData.ReadCsv(file, trainingRowCount);
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
            40,
            20,
            operations,
            data.Inputs.Columns.Select(column => column.Name),
            [constant]);
        var innerProblem = new SymbolicRegressionProblem(
            data,
            Metrics.PearsonR2.ToFinite(),
            searchSpace,
            useLinearScaling);
        var directions = new ObjectiveDirection[]
        {
            ObjectiveDirection.Maximize, // combined score, overridden by score
            ObjectiveDirection.Maximize, // Pearson R2
            ObjectiveDirection.Maximize, // dimensional consistency
            ObjectiveDirection.Maximize, // limits and trends
            ObjectiveDirection.Maximize  // symmetry
        };

        return new PythonInterOptEquationScoring(
            new ObjectiveDirections(directions, new LexicographicComparer(directions)),
            searchSpace,
            score)
        {
            InnerProblem = innerProblem,
            ParameterOptimizationIterations = parameterOptimizationIterations
        };
    }

    public static Population<ExpressionTree> RunDefault(PythonInterOptEquationScoring problem, int seed = 42)
    {
        var algorithm = new GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, PythonInterOptEquationScoring>
        {
            Creator = new ProbabilisticTreeCreator(),
            Crossover = new SubtreeCrossover { InternalNodeProbability = 0.9 },
            Mutator = new ChooseOneMutator<ExpressionTree, ExpressionTreeSearchSpace, PythonInterOptEquationScoring>(
                [.. SymbolicExpressionMutators.Default]),
            MutationRate = 0.1,
            Selector = new TournamentSelector<ExpressionTree>(4),
            PopulationSize = 300,
            MaximumGenerations = 200,
            Refiner = new InnerProblemParameterFittingRefiner(
                new NumericParameterFittingRefiner { MaximumIterations = problem.ParameterOptimizationIterations })
        };

        return algorithm
            .Complete(problem, RandomNumberGenerator.Create(seed), ct: CancellationToken.None)
            .Population;
    }

    public static Population<ExpressionTree> RunDefault(
        string file,
        int trainingRowCount,
        Func<ExpressionTree, ObjectiveVector, double[]> score,
        int seed = 42,
        bool useLinearScaling = true,
        int parameterOptimizationIterations = 5) =>
        RunDefault(
            DefaultConf(file, trainingRowCount, score, useLinearScaling, parameterOptimizationIterations),
            seed);
}
