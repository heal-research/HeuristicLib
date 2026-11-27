using System.Text.Json.Nodes;
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.MachineLearning;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Tests.Docs.Domains;

public sealed class SymbolicRegressionDocumentationSpecs
{
    private const int SampleCount = 31;

    [Fact]
    public async Task TheFigureRunFindsAModelThatFollowsTheData()
    {
        // Noisy samples of 0.5 x² - x + 1 on [-3, 3]. The figure shows the best expression and its predictions.
        var noise = RandomNumberGenerator.Create(seed: 3);
        var inputs = new double[SampleCount, 1];
        var target = new double[SampleCount];
        for (var row = 0; row < SampleCount; row++)
        {
            var x = -3.0 + 6.0 * row / (SampleCount - 1);
            inputs[row, 0] = x;
            target[row] = 0.5 * x * x - x + 1 + noise.NextNormal(mu: 0.0, sigma: 0.3);
        }

        var data = new RegressionData(
            DataFrame.FromMatrix(["x"], inputs),
            Series<double>.FromOwnedArray("y", target));
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 4,
            operations: Symbols.MinimalOperations,
            variables: ["x"]);
        var problem = new SymbolicRegressionProblem(data, Metrics.RMSE, searchSpace);
        var algorithm = GeneticAlgorithm.Create(
            new RampedHalfAndHalfTreeCreator(),
            new SubtreeCrossover(),
            ChoosePerItem.Create(
                new NodeReplacementMutator(),
                new SubtreeMutator(),
                new LocalPerturbationMutator()),
            selector: TournamentSelector.For(problem, tournamentSize: 3),
            populationSize: 100,
            maximumGenerations: 30,
            mutationRate: 0.2) with
        {
            Refiner = new NumericParameterFittingRefiner { MaximumIterations = 10 }.AppliedAtRate(0.25)
        };

        var finalState = await algorithm.RunToCompletionAsync(
            problem, RandomNumberGenerator.Create(seed: 123), ct: TestContext.Current.CancellationToken);

        var best = finalState.Population.EvaluatedCandidates.MinBy(candidate => candidate.ObjectiveVector[0])!;
        var predictions = best.Candidate.Evaluate(data.Inputs);

        // The noise has a standard deviation of 0.3, so a good model reaches an error near it.
        best.ObjectiveVector[0].ShouldBeLessThan(0.5);

        FigureData.Write("symbolic-regression", new JsonObject
        {
            ["x"] = FigureData.Numbers(Enumerable.Range(0, SampleCount).Select(row => inputs[row, 0])),
            ["target"] = FigureData.Numbers(target),
            ["prediction"] = FigureData.Numbers(predictions),
            ["rmse"] = Math.Round(best.ObjectiveVector[0], 3),
            ["infix"] = best.Candidate.ToInfixString(),
            ["tree"] = Node(best.Candidate.Root)
        });
    }

    // A node as its label and children. Constants show their value with two decimals.
    private static JsonObject Node(ExpressionNode node)
    {
        var label = node switch
        {
            VariableExpressionNode variable => variable.VariableName,
            NumericConstantExpressionNode constant => constant.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
            _ => node.Name
        };
        var kind = node switch
        {
            VariableExpressionNode => "variable",
            NumericConstantExpressionNode => "constant",
            _ => "operation"
        };
        return new JsonObject
        {
            ["label"] = label,
            ["kind"] = kind,
            ["children"] = new JsonArray([.. Enumerable.Range(0, node.Arity).Select(index => (JsonNode?)Node(node.GetChild(index)))])
        };
    }
}
