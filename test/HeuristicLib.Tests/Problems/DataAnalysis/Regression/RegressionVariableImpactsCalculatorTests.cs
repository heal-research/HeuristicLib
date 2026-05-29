using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

namespace HEAL.HeuristicLib.Tests.Problems.DataAnalysis.Regression;

public sealed class RegressionVariableImpactsCalculatorTests
{
    [Fact]
    public void CalculateImpacts_ReturnsOneImpactPerInputVariable()
    {
        var problemData = CreateNumericProblemData();
        var model = new WeightedSumRegressionModel();

        var impacts = RegressionVariableImpactsCalculator.CalculateImpacts(
          problemData,
          model,
          RegressionVariableImpactsCalculator.ReplacementMethodType.Average);

        var impactDict = impacts.ToDictionary(x => x.Item1, x => x.Item2);

        impactDict.Count.ShouldBe(2);
        impactDict.Keys.ShouldContain("x1");
        impactDict.Keys.ShouldContain("x2");
    }

    [Fact]
    public void CalculateImpact_UnknownVariable_ThrowsInvalidOperationException()
    {
        var problemData = CreateNumericProblemData();
        var model = new WeightedSumRegressionModel();
        var rows = Enumerable.Range(0, problemData.Dataset.Rows).ToArray();
        var modifiableDataset = problemData.Dataset.ToModifiable();

        var ex = Should.Throw<InvalidOperationException>(() =>
          RegressionVariableImpactsCalculator.CalculateImpact(
            "does_not_exist",
            model,
            problemData,
            modifiableDataset,
            rows,
            RegressionVariableImpactsCalculator.ReplacementMethodType.Average));

        ex.Message.ShouldContain("does_not_exist");
    }

    [Fact]
    public void CalculateImpact_ReplacesVariableOnlyTemporarily()
    {
        var problemData = CreateNumericProblemData();
        var model = new WeightedSumRegressionModel();
        var rows = Enumerable.Range(0, problemData.Dataset.Rows).ToArray();
        var modifiableDataset = problemData.Dataset.ToModifiable();
        var originalValues = modifiableDataset.GetDoubleValues("x1").ToArray();

        _ = RegressionVariableImpactsCalculator.CalculateImpact(
          "x1",
          model,
          problemData,
          modifiableDataset,
          rows,
          RegressionVariableImpactsCalculator.ReplacementMethodType.Average);

        var valuesAfterCall = modifiableDataset.GetDoubleValues("x1").ToArray();
        valuesAfterCall.ShouldBe(originalValues);
    }

    [Fact]
    public void CalculateImpact_ImportantNumericVariable_HasPositiveImpact()
    {
        var problemData = CreateNumericProblemData();
        var model = new WeightedSumRegressionModel();
        var rows = Enumerable.Range(0, problemData.Dataset.Rows).ToArray();
        var modifiableDataset = problemData.Dataset.ToModifiable();

        var impact = RegressionVariableImpactsCalculator.CalculateImpact(
          "x1",
          model,
          problemData,
          modifiableDataset,
          rows,
          RegressionVariableImpactsCalculator.ReplacementMethodType.Average);

        (impact > 0.5).ShouldBeTrue($"Expected x1 to have a clearly positive impact, but got {impact}.");
    }

    [Fact]
    public void CalculateImpacts_Overloads_ReturnSameResults()
    {
        var problemData = CreateNumericProblemData();
        var model = new WeightedSumRegressionModel();
        var rows = Enumerable.Range(0, problemData.Dataset.Rows).ToArray();
        var estimatedValues = model.Predict(problemData.Dataset, rows).ToArray();

        var viaConvenienceOverload = RegressionVariableImpactsCalculator.CalculateImpacts(
                                                                          problemData,
                                                                          model,
                                                                          RegressionVariableImpactsCalculator.ReplacementMethodType.Average,
                                                                          dataPartition: DataAnalysisProblemData.PartitionType.All)
                                                                        .OrderBy(x => x.Item1)
                                                                        .ToArray();

        var viaExplicitOverload = RegressionVariableImpactsCalculator.CalculateImpacts(
                                                                       model,
                                                                       problemData,
                                                                       estimatedValues,
                                                                       rows,
                                                                       RegressionVariableImpactsCalculator.ReplacementMethodType.Average)
                                                                     .OrderBy(x => x.Item1)
                                                                     .ToArray();

        viaExplicitOverload.Length.ShouldBe(viaConvenienceOverload.Length);

        for (var i = 0; i < viaConvenienceOverload.Length; i++)
        {
            viaExplicitOverload[i].Item1.ShouldBe(viaConvenienceOverload[i].Item1);
            viaExplicitOverload[i].Item2.ShouldBe(viaConvenienceOverload[i].Item2, 1e-10);
        }
    }

    [Fact]
    public void CalculateImpact_ForLessImportantVariable_IsSmallerThanForMoreImportantVariable()
    {
        var problemData = CreateNumericProblemData();
        var model = new WeightedSumRegressionModel();
        var rows = Enumerable.Range(0, problemData.Dataset.Rows).ToArray();
        var modifiableDataset = problemData.Dataset.ToModifiable();

        var x1Impact = RegressionVariableImpactsCalculator.CalculateImpact(
          "x1",
          model,
          problemData,
          modifiableDataset,
          rows,
          RegressionVariableImpactsCalculator.ReplacementMethodType.Average);

        var x2Impact = RegressionVariableImpactsCalculator.CalculateImpact(
          "x2",
          model,
          problemData,
          modifiableDataset,
          rows,
          RegressionVariableImpactsCalculator.ReplacementMethodType.Average);

        (x1Impact > x2Impact).ShouldBeTrue($"Expected x1 impact ({x1Impact}) to be larger than x2 impact ({x2Impact}).");
    }

    [Fact]
    public void CalculateImpact_ForCategoricalVariable()
    {
        var dataset = new ModifiableDataset(
          ["cat", "y"],
          [
            new List<string> { "A", "A", "B", "B", "A", "B" },
        new List<double> { 0, 0, 1, 1, 0, 1 }
          ]);

        var problemData = new RegressionProblemData(dataset, "y", ["cat"]);
        var model = new SimpleCategoricalRegressionModel();
        var rows = Enumerable.Range(0, dataset.Rows).ToArray();
        var modifiableDataset = problemData.Dataset.ToModifiable();

        var impact = RegressionVariableImpactsCalculator.CalculateImpact(
          "cat",
          model,
          problemData,
          modifiableDataset,
          rows,
          factorReplacementMethod: RegressionVariableImpactsCalculator.FactorReplacementMethodType.Mode);
        impact.ShouldBe(1);
    }

    private static RegressionProblemData CreateNumericProblemData()
    {
        // y = 2*x1 + x2
        // x1 is intentionally more important than x2
        var dataset = new ModifiableDataset(
          ["x1", "x2", "y"],
          [
            new List<double> { 1, 2, 3, 4, 5 },
        new List<double> { 5, 1, 4, 2, 3 },
        new List<double> { 7, 5, 10, 10, 13 }
          ]);

        return new RegressionProblemData(dataset, "y", ["x1", "x2"]);
    }

    private sealed class WeightedSumRegressionModel : IRegressionModel
    {
        public IEnumerable<double> Predict(Dataset dataset, IEnumerable<int> rows)
        {
            var rowArray = rows.ToArray();
            var x1 = dataset.GetDoubleValues("x1", rowArray).ToArray();
            var x2 = dataset.GetDoubleValues("x2", rowArray).ToArray();

            for (var i = 0; i < rowArray.Length; i++)
            {
                yield return 2.0 * x1[i] + x2[i];
            }
        }
    }

    private sealed class SimpleCategoricalRegressionModel : IRegressionModel
    {
        public IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows)
        {
            var rowArray = rows.ToArray();
            var cat = data.GetStringValues("cat", rowArray).ToArray();

            foreach (var value in cat)
            {
                yield return value == "B" ? 1.0 : 0.0;
            }
        }
    }
}
