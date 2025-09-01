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

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

/// <summary>
/// Represents a regression data analysis model
/// </summary>
public class CachedRegressionModel(IRegressionModel model) : IRegressionModel {
  protected readonly Dictionary<int, double> EvaluationCache = new();
  protected readonly IRegressionModel Model = model;

  public IEnumerable<double> GetEstimatedValues(Dataset data, IEnumerable<int> rows) {
    var rowsP = rows as ICollection<int> ?? rows.ToArray();
    var rowsToEvaluate = rowsP.Where(row => !EvaluationCache.ContainsKey(row)).ToList();
    var rowsEnumerator = rowsToEvaluate.GetEnumerator();
    using var valuesEnumerator = Model.Predict(data, rowsToEvaluate).GetEnumerator();
    while (rowsEnumerator.MoveNext() & valuesEnumerator.MoveNext()) {
      EvaluationCache.Add(rowsEnumerator.Current, valuesEnumerator.Current);
    }

    return rowsP.Select(row => EvaluationCache[row]);
  }

  public IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows) => GetEstimatedValues(data, rows);
}
