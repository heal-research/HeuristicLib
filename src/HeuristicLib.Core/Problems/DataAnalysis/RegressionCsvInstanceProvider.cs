using HEAL.HeuristicLib.Collections;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public static class RegressionCsvInstanceProvider {
  public static RegressionProblemData ImportData(string path, double trainingSplit = 0.66) {
    TableFileParser csvFileParser = new TableFileParser();
    csvFileParser.Parse(path, csvFileParser.AreColumnNamesInFirstLine(path));

    Dataset dataset = new ModifiableDataset(csvFileParser.VariableNames, csvFileParser.Values);
    string targetVar = dataset.DoubleVariables.Last();

    // turn off input variables that are constant in the training partition
    var trainingEnd = (int)(csvFileParser.Rows * trainingSplit);
    var allowedInputVars = csvFileParser.Rows >= 3 ? dataset.DoubleVariables.Where(variableName => dataset.GetDoubleValues(variableName, Enumerable.Range(0, trainingEnd)).Range() > 0 && variableName != targetVar).ToList() : dataset.DoubleVariables.Where(x => !x.Equals(targetVar)).ToList();

    var regressionData = new RegressionProblemData(dataset, allowedInputVars, targetVar) {
      Partitions = {
        [DataAnalysisProblemData.PartitionType.Training] = new Range(0, trainingEnd),
        [DataAnalysisProblemData.PartitionType.Test] = new Range(trainingEnd, csvFileParser.Rows)
      }
    };

    return regressionData;
  }
}
