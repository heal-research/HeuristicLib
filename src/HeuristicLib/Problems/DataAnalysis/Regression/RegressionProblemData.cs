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

using System.Collections.Concurrent;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

public class RegressionProblemData(Dataset dataset, IEnumerable<string> allowedInputVariables, string targetVariable)
  : DataAnalysisProblemData(dataset, allowedInputVariables) {
  public string TargetVariable { get; } = targetVariable;
  private readonly Dictionary<PartitionType, double[]> cachedTargets = [];

  public IReadOnlyList<double> TargetVariableValues(PartitionType partition) {
    if (cachedTargets.TryGetValue(partition, out var res)) return res;
    return cachedTargets[partition] = Dataset.GetDoubleValues(TargetVariable, Partitions[partition].Enumerate()).ToArray();
  }

  public IEnumerable<double> TargetVariableValues() => Dataset.GetDoubleValues(TargetVariable);
}
