using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public static class RegressionCsvInstanceProvider
{
  public static RegressionProblemData ImportData(string path, int trainingRowCount)
  {
    var csvFileParser = ParseFile(path);
    return ImportFromParser(csvFileParser, trainingRowCount);
  }

  public static RegressionProblemData ImportData(string path, double trainingSplit = 0.66)
  {
    var csvFileParser = ParseFile(path);
    return ImportFromParser(csvFileParser, (int)(csvFileParser.Rows * trainingSplit));
  }

  private static TableFileParser ParseFile(string path)
  {
    var csvFileParser = new TableFileParser();
    csvFileParser.Parse(path, csvFileParser.AreColumnNamesInFirstLine(path));
    return csvFileParser;
  }

  private static RegressionProblemData ImportFromParser(TableFileParser csvFileParser, int trainingRowCount)
  {
    Dataset dataset = new ModifiableDataset(csvFileParser.VariableNames, csvFileParser.Values);
    var targetVar = dataset.DoubleVariables.Last();

    // turn off input variables that are constant in the training partition
    var allowedInputVars = csvFileParser.Rows >= 3
      ? dataset.DoubleVariables.Where(v => dataset.GetDoubleValues(v, Enumerable.Range(0, trainingRowCount)).Range() > 0 && v != targetVar).ToList()
      : dataset.DoubleVariables.Where(v => v != targetVar).ToList();

    return new RegressionProblemData(dataset, targetVar, allowedInputVars, ..trainingRowCount);
  }
}
