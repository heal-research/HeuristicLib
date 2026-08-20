using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.DataAnalysis.Regression;
using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionMutators;
using HEAL.HeuristicLib.Operators.Refiners;
using HEAL.HeuristicLib.Operators.Refiners.SymbolicRegressionRefiners;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;
using HEAL.HeuristicLib.SearchSpaces.SymbolicExpressions;

namespace HEAL.HeuristicLib.PythonInterop;

public sealed record EquationScoringEvaluator(
    Func<ExpressionTree[], ObjectiveVector[], double[][]> PythonCallback)
    : StatelessEvaluator<ExpressionTree, ExpressionTreeSearchSpace, ExtendedSymbolicRegressionProblem>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<ExpressionTree> candidates, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, ExtendedSymbolicRegressionProblem problem)
    {
        var normalObjectives = problem.Evaluate(candidates, random);
        var callbackObjectives = PythonCallback(candidates.ToArray(), normalObjectives.ToArray());

        return callbackObjectives.Select(objectives => (ObjectiveVector)objectives).ToArray();
    }
}

/// <summary>
/// Symbolic regression problem whose individual and population objectives can be augmented from Python.
/// </summary>
public class ExtendedSymbolicRegressionProblem(
    ObjectiveDirections objective,
    ExpressionTreeSearchSpace searchSpace,
    Func<ExpressionTree, ObjectiveVector, double[]> individualPythonCallback)
    : SingleSolutionProblem<ExpressionTree, ExpressionTreeSearchSpace>(objective, searchSpace)
{
    private sealed record InnerProblemParameterFittingRefiner(NumericParameterFittingRefiner ChildRefiner)
        : SingleCandidateRefiner<ExpressionTree, ExpressionTreeSearchSpace, ExtendedSymbolicRegressionProblem>
    {
        public override ExpressionTree RefineCandidate(ExpressionTree candidate, IRandomNumberGenerator random, ExpressionTreeSearchSpace searchSpace, ExtendedSymbolicRegressionProblem problem) =>
            ChildRefiner.RefineCandidate(candidate, random, searchSpace, problem.InnerProblem);
    }

    public required SymbolicRegressionProblem InnerProblem { get; init; }

    public override ObjectiveVector Evaluate(ExpressionTree solution, IRandomNumberGenerator random) =>
        individualPythonCallback(solution, InnerProblem.Evaluate(solution));

    public static Population<ExpressionTree> RunDefault(
        string file,
        int trainingRowCount,
        Func<ExpressionTree, ObjectiveVector, double[]>? individualPythonCallback,
        Func<ExpressionTree[], ObjectiveVector[], double[][]> populationwidePythonCallback,
        int seed = 42,
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

        // The number of objectives, these directions, and the objective vectors returned by
        // the Python callbacks must remain in sync.
        var directions = new ObjectiveDirection[]
        {
            ObjectiveDirection.Maximize, // combined score supplied by Python
            ObjectiveDirection.Maximize, // Pearson R2, retained for final comparison
            ObjectiveDirection.Maximize, // dimensional consistency
            ObjectiveDirection.Maximize, // limits and trends
            ObjectiveDirection.Maximize  // symmetry
        };

        // Python may pass None for the individual callback. Keep the objective count stable by
        // using Pearson R2 as the combined score and supplying neutral values for the remaining objectives.
        individualPythonCallback ??= static (_, objective) => [objective[0], 0.0, 0.0, 0.0, 0.0];
        var problem = new ExtendedSymbolicRegressionProblem(
            new ObjectiveDirections(directions, new LexicographicComparer(directions)),
            searchSpace,
            individualPythonCallback)
        {
            InnerProblem = innerProblem
        };
        var algorithm = new GeneticAlgorithm<ExpressionTree, ExpressionTreeSearchSpace, ExtendedSymbolicRegressionProblem>
        {
            Creator = new ProbabilisticTreeCreator(),
            Crossover = new SubtreeCrossover { InternalNodeProbability = 0.9 },
            Mutator = new ChooseOneMutator<ExpressionTree, ExpressionTreeSearchSpace, ExtendedSymbolicRegressionProblem>(
                [.. SymbolicExpressionMutators.Default]),
            MutationRate = 0.1,
            Selector = new TournamentSelector<ExpressionTree>(4),
            PopulationSize = 300,
            Evaluator = new EquationScoringEvaluator(populationwidePythonCallback),
            MaximumGenerations = 200,
            Refiner = new InnerProblemParameterFittingRefiner(
                new NumericParameterFittingRefiner { MaximumIterations = parameterOptimizationIterations })
        };

        return algorithm
            .Complete(problem, RandomNumberGenerator.Create(seed), ct: CancellationToken.None)
            .Population;
    }
}
