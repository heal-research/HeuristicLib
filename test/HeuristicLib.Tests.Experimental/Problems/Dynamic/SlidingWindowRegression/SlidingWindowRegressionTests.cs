using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.Problems.Dynamic.SlidingWindowRegression;
using HEAL.HeuristicLib.Problems.MachineLearning;
using HEAL.HeuristicLib.Tests.TestSupport.Random;

namespace HEAL.HeuristicLib.Tests.Problems.Dynamic.SlidingWindowRegression;

public class SlidingWindowRegressionTests
{
    private static readonly double[,] Data =
    {
        { 1.0, 2.0, 3.0 },
        { 4.0, 5.0, 6.0 },
        { 7.0, 8.0, 9.0 },
        { 10.0, 11.0, 12.0 },
        { 13.0, 14.0, 15.0 },
        { 16.0, 17.0, 18.0 },
        { 19.0, 20.0, 21.0 },
        { 22.0, 23.0, 24.0 },
        { 25.0, 26.0, 27.0 },
        { 28.0, 29.0, 30.0 }
    };

    private static RegressionData CreateData()
    {
        var frame = DataFrame.FromMatrix(["x1", "x2", "y"], Data);
        return new RegressionData(
            new DataFrame([frame.Get<double>("x1"), frame.Get<double>("x2")]),
            frame.Get<double>("y"));
    }

    private static ExpressionTreeSearchSpace CreateSearchSpace()
    {
        return new ExpressionTreeSearchSpace(
            maximumLength: 40,
            maximumDepth: 40,
            operations: [],
            variables: ["x1", "x2"],
            constants: []);
    }

    private static ExpressionTree MakeVariableTree(
        ExpressionTreeSearchSpace searchSpace,
        string variableName)
    {
        return ExpressionDraft.Variable(variableName).Build(searchSpace);
    }

    [Fact]
    public void WindowRows_WithinTrainingData_NoWrap()
    {
        var data = CreateData();
        var spy = new SpyMetric();
        var inner = new SymbolicRegressionProblem(data, spy, CreateSearchSpace());
        var p = new SlidingWindowSymbolicRegressionProblem(inner, 2, 4, 1);
        var tree = MakeVariableTree(p.SearchSpace, "x1");
        _ = p.Evaluate(tree, TestRandoms.NoRandom)[0];
        spy.LastPredictions[0].ShouldBe([7.0, 10, 13.0, 16.0]);
        spy.LastTargets[0].ShouldBe([9.0, 12.0, 15.0, 18.0]);
    }

    [Fact]
    public void WindowRows_WrapsRoundRobin()
    {
        var data = CreateData();
        var spy = new SpyMetric();
        var inner = new SymbolicRegressionProblem(data, spy, CreateSearchSpace());
        var p = new SlidingWindowSymbolicRegressionProblem(inner, 8, 5, 1);
        var tree = MakeVariableTree(p.SearchSpace, "x1");
        _ = p.Evaluate(tree, TestRandoms.NoRandom)[0];
        spy.LastPredictions[0].ShouldBe([25.0, 28.0, 1.0, 4.0, 7.0]);
        spy.LastTargets[0].ShouldBe([27.0, 30.0, 3.0, 6.0, 9.0]);
    }

    [Fact]
    public void Update_AdvancesWindow_ByStepSize()
    {
        var data = CreateData();
        var spy = new SpyMetric();
        var inner = new SymbolicRegressionProblem(data, spy, CreateSearchSpace());
        var p = new SlidingWindowSymbolicRegressionProblem(inner, 1, 4, 3);
        var tree = MakeVariableTree(p.SearchSpace, "x1");
        _ = p.Evaluate(tree, TestRandoms.NoRandom)[0];
        spy.LastPredictions[0].ShouldBe([4.0, 7.0, 10.0, 13.0]);
        p.UpdateOnce();
        _ = p.Evaluate(tree, TestRandoms.NoRandom)[0];
        spy.LastPredictions[1].ShouldBe([13.0, 16.0, 19.0, 22.0]);
    }

    [Fact]
    public void WindowLength_IsRespected()
    {
        var data = CreateData();
        var spy = new SpyMetric();
        var inner = new SymbolicRegressionProblem(data, spy, CreateSearchSpace());
        var p = new SlidingWindowSymbolicRegressionProblem(inner, 9, 7, 1);
        var tree = MakeVariableTree(p.SearchSpace, "x1");
        _ = p.Evaluate(tree, TestRandoms.NoRandom)[0];
        spy.LastPredictions[0].Length.ShouldBe(7);
    }

    [Fact]
    public void WindowEvaluation_PreservesPredictionAndExpressionObjectives()
    {
        var data = CreateData();
        var spy = new SpyMetric();
        var searchSpace = CreateSearchSpace();
        var inner = new SymbolicRegressionProblem(
            data,
            spy,
            ExpressionMetrics.Length,
            searchSpace);
        var problem = new SlidingWindowSymbolicRegressionProblem(inner, 2, 4, 1);
        var tree = MakeVariableTree(searchSpace, "x1");

        var objective = problem.Evaluate(tree, TestRandoms.NoRandom);

        objective.Count.ShouldBe(2);
        objective[1].ShouldBe(1.0);
        spy.LastPredictions.Single().ShouldBe([7.0, 10.0, 13.0, 16.0]);
    }

    private sealed class SpyMetric : IRegressionMetric
    {
        public List<double[]> LastPredictions { get; } = [];
        public List<double[]> LastTargets { get; } = [];
        public ObjectiveDirection Direction => ObjectiveDirection.Minimize;

        public double Evaluate(
            ReadOnlySpan<double> predictedValues,
            ReadOnlySpan<double> targetValues)
        {
            LastPredictions.Add(predictedValues.ToArray());
            LastTargets.Add(targetValues.ToArray());
            var d = LastPredictions[^1];
            var res = d[0].GetHashCode();
            for (var i = 1; i < d.Length; i++)
            {
                res = HashCode.Combine(res, d[i]);
            }

            return res;
        }
    }
}
