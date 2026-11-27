#region symbolic-regression-imports
using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Data;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.MachineLearning;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.MachineLearning;
using HEAL.HeuristicLib.Random;
#endregion symbolic-regression-imports
using System.Globalization;

namespace HEAL.HeuristicLib.Tests.Docs.Examples;

public sealed class SymbolicRegressionExampleDocumentationSpecs
{
    [Fact]
    public async Task TheExampleRunRecoversTheLinearFunctionAndPrintsTheQuotedModel()
    {
        // The page shows the output of an invariant-culture console.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        #region symbolic-regression-data
        var x = Enumerable.Range(0, 40)
            .Select(index => (index - 20) * 0.25)
            .ToArray();
        var y = x.Select(value => 2.5 * value + 1.3).ToArray();

        var trainingData = new RegressionData(
            new DataFrame([Series<double>.FromOwnedArray("x", x)]),
            Series<double>.FromOwnedArray("y", y));
        #endregion symbolic-regression-data

        #region symbolic-regression-language
        var searchSpace = new ExpressionTreeSearchSpace(
            maximumLength: 15,
            maximumDepth: 5,
            operations: [Symbols.Addition, Symbols.Subtraction, Symbols.Multiplication],
            variables: ["x"],
            constants: [new EvolvableConstantSymbol()]);

        var problem = new SymbolicRegressionProblem(
            trainingData,
            Metrics.MSE,
            searchSpace);
        #endregion symbolic-regression-language

        #region symbolic-regression-algorithm
        var algorithm = GeneticAlgorithm.Create(
            new RampedHalfAndHalfTreeCreator(),
            new SubtreeCrossover(),
            ChoosePerItem.Create(
                new NodeReplacementMutator(),
                new LocalPerturbationMutator(),
                new SubtreeMutator()),
            refiner: new NumericParameterFittingRefiner
            {
                MaximumIterations = 10
            },
            selector: TournamentSelector.For(problem, tournamentSize: 3),
            populationSize: 100,
            maximumGenerations: 50,
            mutationRate: 0.25);
        #endregion symbolic-regression-algorithm

#pragma warning disable xUnit1051 // The displayed program has no cancellation token.
        #region symbolic-regression-run
        var finalState = await algorithm.RunToCompletionAsync(
            problem,
            RandomNumberGenerator.Create(seed: 42));

        var best = finalState.Population.EvaluatedCandidates
            .MinBy(candidate => candidate.ObjectiveVector[0])!;
        var compiledModel = best.Candidate.Compile(optimize: true);
        var predictions = compiledModel.Evaluate(trainingData.Inputs);

        Console.WriteLine($"Training MSE: {best.ObjectiveVector[0]:F6}");
        Console.WriteLine($"Model: {best.Candidate.ToInfixString()}");
        Console.WriteLine();
        Console.WriteLine("    x        y  prediction");

        foreach (var row in Enumerable.Range(0, 5))
        {
            Console.WriteLine($"{x[row],5:F2}  {y[row],7:F3}  {predictions[row],10:F3}");
        }
        #endregion symbolic-regression-run
#pragma warning restore xUnit1051

        #region symbolic-regression-compiled
        Console.WriteLine(compiledModel.ToInfixString());
        #endregion symbolic-regression-compiled

        // The search recovers 2.5x + 1.3 on every row.
        best.ObjectiveVector[0].ShouldBeLessThan(1e-20);
        foreach (var row in Enumerable.Range(0, x.Length))
        {
            predictions[row].ShouldBe(y[row], 1e-9);
        }

        // Which tree the search finds depends on the processor's vector width, so the page shows the two trees on
        // lines 1 and 9 as an example. Every other line the page quotes is the same on every processor.
        var lines = PrintedOutput.Lines();
        lines.Length.ShouldBe(10);
        lines[1].ShouldStartWith("Model: ");
        lines.Where((_, index) => index is not (1 or 9)).ShouldBe([
            "Training MSE: 0.000000",
            "",
            "    x        y  prediction",
            "-5.00  -11.200     -11.200",
            "-4.75  -10.575     -10.575",
            "-4.50   -9.950      -9.950",
            "-4.25   -9.325      -9.325",
            "-4.00   -8.700      -8.700"
        ]);
    }
}
