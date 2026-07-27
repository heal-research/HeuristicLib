using HEAL.HeuristicLib.PythonInterop;

namespace HEAL.HeuristicLib.Tests.Scenarios.PythonInterop;

public sealed class InteractiveSymbolicRegressionTests
{
    [Fact]
    public void Run_ThrowsWhenLinearScalingIsEnabled()
    {
        var callbackInvoked = false;
        var parameters = CreateParameters();
        parameters.UseLinearScaling = true;

        Should.Throw<NotImplementedException>(() =>
            InteractiveSymbolicRegression.Run(
                [0.0],
                [0.0],
                (_, _) =>
                {
                    callbackInvoked = true;
                    return [];
                },
                parameters));

        callbackInvoked.ShouldBeFalse();
    }

    [Fact]
    public void Run_ThrowsWhenParameterOptimizationIsEnabled()
    {
        var callbackInvoked = false;
        var parameters = CreateParameters();
        parameters.ParameterOptimizationIterations = 1;

        Should.Throw<NotImplementedException>(() =>
            InteractiveSymbolicRegression.Run(
                [0.0],
                [0.0],
                (_, _) =>
                {
                    callbackInvoked = true;
                    return [];
                },
                parameters));

        callbackInvoked.ShouldBeFalse();
    }

    [Fact]
    public void Run_ReturnsModernExpressionPopulation()
    {
        var callbackCount = 0;
        var parameters = CreateParameters();

        var population = InteractiveSymbolicRegression.Run(
            [-1.0, 0.0, 1.0, 2.0],
            [-1.0, 0.0, 1.0, 2.0],
            (trees, objectives) =>
            {
                trees.Length.ShouldBe(parameters.PopulationSize);
                callbackCount++;
                return objectives.Select(objective => objective.ToArray()).ToArray();
            },
            parameters,
            TestContext.Current.CancellationToken);

        population.EvaluatedCandidates.Length.ShouldBe(parameters.PopulationSize);
        callbackCount.ShouldBe(parameters.Generations);
    }

    private static InteractiveSymRegParameters CreateParameters()
    {
        return new InteractiveSymRegParameters
        {
            PopulationSize = 1,
            Generations = 1,
            TreeLength = 7,
            TreeDepth = 3,
            MutationRate = 0.1,
            TournamentSize = 2,
            Elites = 1,
            ParameterOptimizationIterations = 0,
            Seed = 42,
            AllowedSymbols = ["add", "mul", "constant", "variable"],
            UseLinearScaling = false
        };
    }
}
