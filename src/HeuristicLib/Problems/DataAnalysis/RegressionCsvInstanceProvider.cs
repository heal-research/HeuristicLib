#region License Information
/* HeuristicLab
 * Copyright (C) Heuristic and Evolutionary Algorithms Laboratory (HEAL)
 *
 * This file is part of HeuristicLab.
 *
 * HeuristicLab is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * HeuristicLab is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with HeuristicLab. If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public static class RegressionCsvInstanceProvider {
  public static RegressionProblemData ImportData(string path) {
    TableFileParser csvFileParser = new TableFileParser();
    csvFileParser.Parse(path, csvFileParser.AreColumnNamesInFirstLine(path));

    Dataset dataset = new ModifiableDataset(csvFileParser.VariableNames, csvFileParser.Values);
    string targetVar = dataset.DoubleVariables.Last();

    // turn off input variables that are constant in the training partition
    var trainingEnd = (csvFileParser.Rows * 2) / 3;
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
