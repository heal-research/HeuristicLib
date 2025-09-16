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

namespace HEAL.HeuristicLib.Problems.DataAnalysis {
  public static class DatasetExtensions {
    public static double[,] ToArray(this Dataset dataset, IEnumerable<string> variables, IEnumerable<int> rows) {
      var variablesArr = variables.ToArray();
      var rowsArr = rows.ToArray();

      var matrix = new double[rowsArr.Length, variablesArr.Length];

      for (var i = 0; i < variablesArr.Length; i++) {
        var origValues = dataset.GetDoubleValues(variablesArr[i], rowsArr);
        var row = 0;
        foreach (var value in origValues) {
          matrix[row, i] = value;
          row++;
        }
      }

      return matrix;
    }

    /// <summary>
    /// Prepares a binary data matrix from a number of factors and specified factor values
    /// </summary>
    /// <param name="dataset">A dataset that contains the variable values</param>
    /// <param name="factorVariables">An enumerable of categorical variables (factors). For each variable an enumerable of values must be specified.</param>
    /// <param name="rows">An enumerable of row indices for the dataset</param>
    /// <returns></returns>
    /// <remarks>Factor variables (categorical variables) are split up into multiple binary variables one for each specified value.</remarks>
    public static double[,] ToArray(
      this Dataset dataset,
      IEnumerable<KeyValuePair<string, IEnumerable<string>>> factorVariables,
      IEnumerable<int> rows) {
      // check input variables. Only string variables are allowed.
      var keyValuePairs = factorVariables as KeyValuePair<string, IEnumerable<string>>[] ?? factorVariables.ToArray();
      var numBinaryColumns = keyValuePairs.Sum(kvp => kvp.Value.Count());
      var rowsList = rows.ToList();
      var matrix = new double[rowsList.Count, numBinaryColumns];

      var col = 0;
      foreach (var (varName, cats) in keyValuePairs) {
        foreach (var cat in cats) {
          var values = dataset.GetStringValues(varName, rowsList);
          var row = 0;
          foreach (var value in values) {
            matrix[row, col] = value == cat ? 1 : 0;
            row++;
          }

          col++;
        }
      }

      return matrix;
    }

    //public static IntervalCollection GetVariableRanges(this Dataset dataset, bool ignoreNaNs = true) {
    //  var variableRanges = new IntervalCollection();
    //  foreach (var variable in dataset.DoubleVariables) { // ranges can only be calculated for double variables
    //    var values = dataset.GetDoubleValues(variable);

    //    if (ignoreNaNs) {
    //      values = values.Where(v => !double.IsNaN(v));

    //      if (!values.Any()) { //handle values with only NaNs explicitly
    //        var emptyInterval = new Interval(double.NaN, double.NaN);
    //        variableRanges.AddInterval(variable, emptyInterval);
    //        continue;
    //      }
    //    }

    //    var interval = Interval.GetInterval(values);
    //    variableRanges.AddInterval(variable, interval);
    //  }

    //  return variableRanges;
    //}

    public static IEnumerable<KeyValuePair<string, IEnumerable<string>>> GetFactorVariableValues(
      this Dataset ds, IEnumerable<string> factorVariables, IEnumerable<int> rows) {
      return from factor in factorVariables
             let distinctValues = ds.GetStringValues(factor, rows).Distinct().ToArray()
             // 1 distinct value => skip (constant)
             // 2 distinct values => only take one of the two values
             // >=3 distinct values => create a binary value for each value
             let reducedValues = distinctValues.Length <= 2
               ? distinctValues.Take(distinctValues.Length - 1)
               : distinctValues
             select new KeyValuePair<string, IEnumerable<string>>(factor, reducedValues);
    }
  }
}
