using System.Collections;
using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public sealed class RegressionVariableImpactsCalculator(
  RegressionVariableImpactsCalculator.ReplacementMethodType replacementMethod,
  RegressionVariableImpactsCalculator.FactorReplacementMethodType factorReplacementMethod,
  DataAnalysisProblemData.PartitionType dataPartition)
{
  #region Parameters/Properties

  public enum ReplacementMethodType
  {
    Median = 0,
    Average = 1,
    Shuffle = 2,
    Noise = 3
  }

  public enum FactorReplacementMethodType
  {
    Best = 0,
    Mode = 1,
    Shuffle = 2
  }

  public ReplacementMethodType ReplacementMethod { get; set; } = replacementMethod;
  public FactorReplacementMethodType FactorReplacementMethod { get; set; } = factorReplacementMethod;
  public DataAnalysisProblemData.PartitionType DataPartition { get; set; } = dataPartition;

  #endregion

  public ICollection<(string, double)> Calculate(IRegressionModel solution, RegressionProblemData data) => CalculateImpacts(data, solution, ReplacementMethod, FactorReplacementMethod, DataPartition);

  public static ICollection<(string, double)> CalculateImpacts(
    RegressionProblemData data,
    IRegressionModel solution,
    ReplacementMethodType replacementMethod = ReplacementMethodType.Shuffle,
    FactorReplacementMethodType factorReplacementMethod = FactorReplacementMethodType.Best,
    DataAnalysisProblemData.PartitionType dataPartition = DataAnalysisProblemData.PartitionType.Training)
  {
    var rows = data.Partitions[dataPartition].Enumerate().ToArray();
    var estimatedValues = solution.Predict(data.Dataset, rows).ToArray();
    return CalculateImpacts(solution, data, estimatedValues, rows, replacementMethod, factorReplacementMethod);
  }

  public static ICollection<(string, double)> CalculateImpacts(
    IRegressionModel solution,
    RegressionProblemData problemData,
    IReadOnlyList<double> estimatedValues,
    IReadOnlyList<int> rows,
    ReplacementMethodType replacementMethod = ReplacementMethodType.Shuffle,
    FactorReplacementMethodType factorReplacementMethod = FactorReplacementMethodType.Best)
  {
    var targetValues = problemData.Dataset.GetDoubleValues(problemData.TargetVariable, rows).ToArray();
    var originalQuality = CalculateQuality(targetValues, estimatedValues);

    var modifiableDataset = problemData.Dataset.ToModifiable();

    return problemData.InputVariables
      .Select(x => (x,
        CalculateImpact(x,
          solution,
          problemData,
          modifiableDataset,
          rows,
          replacementMethod,
          factorReplacementMethod,
          targetValues, originalQuality)))
      .ToArray();
  }

  public static double CalculateImpact(string variableName,
    IRegressionModel solution,
    RegressionProblemData problemData,
    ModifiableDataset modifiableDataset,
    IReadOnlyList<int> rows,
    ReplacementMethodType replacementMethod = ReplacementMethodType.Shuffle,
    FactorReplacementMethodType factorReplacementMethod = FactorReplacementMethodType.Best,
    IReadOnlyList<double>? targetValues = null,
    double quality = double.NaN)
  {
    if (!problemData.InputVariables.Contains(variableName)) {
      throw new InvalidOperationException($"Can not calculate variable impact, because the Solution uses inputs missing in the dataset ({variableName})");
    }

    targetValues ??= problemData.Dataset.GetDoubleValues(problemData.TargetVariable, rows).ToArray();

    if (double.IsNaN(quality)) {
      quality = CalculateQuality(solution.Predict(modifiableDataset, rows), targetValues);
    }

    var replacementValues = GetReplacementValues(modifiableDataset, variableName, solution, rows, targetValues, out var originalValues, replacementMethod, factorReplacementMethod);
    var newValue = CalculateQualityForReplacement(solution, modifiableDataset, variableName, originalValues, rows, replacementValues, targetValues);
    return quality - newValue;
  }

  private static IList GetReplacementValues(ModifiableDataset modifiableDataset,
    string variableName,
    IRegressionModel solution,
    IReadOnlyList<int> rows,
    IReadOnlyList<double> targetValues,
    out IList originalValues,
    ReplacementMethodType replacementMethod = ReplacementMethodType.Shuffle,
    FactorReplacementMethodType factorReplacementMethod = FactorReplacementMethodType.Best)
  {
    IList replacementValues;
    if (modifiableDataset.VariableHasType<double>(variableName)) {
      originalValues = modifiableDataset.GetDoubleValues(variableName).ToList();
      replacementValues = GetReplacementValuesForDouble(modifiableDataset, rows, (List<double>)originalValues, replacementMethod);
    } else if (modifiableDataset.VariableHasType<string>(variableName)) {
      originalValues = modifiableDataset.GetDoubleValues(variableName).ToList();
      replacementValues = GetReplacementValuesForString(solution, modifiableDataset, variableName, rows, (List<string>)originalValues, targetValues, factorReplacementMethod);
    } else {
      throw new NotSupportedException("Variable not supported");
    }

    return replacementValues;
  }

  private static List<double> GetReplacementValuesForDouble(ModifiableDataset modifiableDataset,
    IReadOnlyList<int> rows,
    List<double> originalValues,
    ReplacementMethodType replacementMethod = ReplacementMethodType.Shuffle)
  {
    var random = RandomNumberGenerator.Create(31475, RandomProfile.SystemRandom);
    var r2 = RandomNumberGenerator.Create(31475, RandomProfile.SystemRandom);
    List<double> replacementValues;
    double replacementValue;

    switch (replacementMethod) {
      case ReplacementMethodType.Median:
        replacementValue = rows.Select(r => originalValues[r]).Median();
        replacementValues = Enumerable.Repeat(replacementValue, modifiableDataset.Rows).ToList();
        break;
      case ReplacementMethodType.Average:
        replacementValue = rows.Select(r => originalValues[r]).Average();
        replacementValues = Enumerable.Repeat(replacementValue, modifiableDataset.Rows).ToList();
        break;
      case ReplacementMethodType.Shuffle:
        // new var has same empirical distribution but the relation to y is broken
        // prepare a complete column for the dataset
        replacementValues = Enumerable.Repeat(double.NaN, modifiableDataset.Rows).ToList();
        // shuffle only the selected rows
        var shuffledValues = rows.Select(r => originalValues[r]).Shuffle(random).ToList();
        var i = 0;
        // update column values 
        foreach (var r in rows) {
          replacementValues[r] = shuffledValues[i++];
        }

        break;
      case ReplacementMethodType.Noise:
        var avg = rows.Select(r => originalValues[r]).Average();
        var stdDev = rows.Select(r => originalValues[r]).StandardDeviation();
        // prepare a complete column for the dataset
        replacementValues = Enumerable.Repeat(double.NaN, modifiableDataset.Rows).ToList();
        // update column values 
        foreach (var r in rows) {
          replacementValues[r] = NormalDistribution.NextDouble(r2, avg, stdDev);
        }

        break;

      default:
        throw new ArgumentException($"ReplacementMethod {replacementMethod} cannot be handled.");
    }

    return replacementValues;
  }

  private static List<string> GetReplacementValuesForString(IRegressionModel solution,
    ModifiableDataset modifiableDataset,
    string variableName,
    IReadOnlyList<int> rows,
    List<string> originalValues,
    IReadOnlyList<double> targetValues,
    FactorReplacementMethodType factorReplacementMethod = FactorReplacementMethodType.Shuffle)
  {
    List<string> replacementValues = [];
    var random = RandomNumberGenerator.Create(31415, RandomProfile.SystemRandom);

    switch (factorReplacementMethod) {
      case FactorReplacementMethodType.Best:
        // try replacing with all possible values and find the best replacement value
        var bestQuality = double.NegativeInfinity;
        foreach (var repl in modifiableDataset.GetStringValues(variableName, rows).Distinct()) {
          var curReplacementValues = Enumerable.Repeat(repl, modifiableDataset.Rows).ToList();
          //fholzing: this result could be used later on (theoretically), but is neglected for better readability/method consistency 
          var newValue = CalculateQualityForReplacement(solution, modifiableDataset, variableName, originalValues, rows, curReplacementValues, targetValues);

          if (newValue <= bestQuality) {
            continue;
          }

          bestQuality = newValue;
          replacementValues = curReplacementValues;
        }

        break;
      case FactorReplacementMethodType.Mode:
        var mostCommonValue = rows.Select(r => originalValues[r])
          .GroupBy(v => v)
          .OrderByDescending(g => g.Count())
          .First().Key;
        replacementValues = Enumerable.Repeat(mostCommonValue, modifiableDataset.Rows).ToList();
        break;
      case FactorReplacementMethodType.Shuffle:
        // new var has same empirical distribution but the relation to y is broken
        // prepare a complete column for the dataset
        replacementValues = Enumerable.Repeat(string.Empty, modifiableDataset.Rows).ToList();
        // shuffle only the selected rows
        var shuffledValues = rows.Select(r => originalValues[r]).Shuffle(random).ToList();
        var i = 0;
        // update column values 
        foreach (var r in rows) {
          replacementValues[r] = shuffledValues[i++];
        }

        break;
      default:
        throw new ArgumentException($"FactorReplacementMethod {factorReplacementMethod} cannot be handled.");
    }

    return replacementValues;
  }

  private static double CalculateQualityForReplacement(
    IRegressionModel solution,
    ModifiableDataset modifiableDataset,
    string variableName,
    IList originalValues,
    IEnumerable<int> rows,
    IList replacementValues,
    IEnumerable<double> targetValues)
  {
    modifiableDataset.ReplaceVariable(variableName, replacementValues);
    //mkommend: ToList is used on purpose to avoid lazy evaluation that could result in wrong estimates due to variable replacements
    var ret = CalculateQuality(targetValues, solution.Predict(modifiableDataset, rows));
    modifiableDataset.ReplaceVariable(variableName, originalValues);
    return ret;
  }

  public static double CalculateQuality(IEnumerable<double> targetValues, IEnumerable<double> estimatedValues)
  {
    var ret = OnlinePearsonsRCalculator.Calculate(targetValues, estimatedValues, out var errorState);
    if (errorState != OnlineCalculatorError.None) {
      throw new InvalidOperationException("Error during calculation with replaced inputs.");
    }

    return ret * ret;
  }
}
