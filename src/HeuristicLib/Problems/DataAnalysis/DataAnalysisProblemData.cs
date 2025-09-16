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

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblemData {
  public Dataset Dataset { get; set; }
  public List<string> InputVariables { get; }
  public Dictionary<PartitionType, Range> Partitions { get; }

  protected DataAnalysisProblemData(Dataset dataset, IEnumerable<string> inputs) {
    Dataset = dataset;
    InputVariables = inputs.Where(variable => dataset.VariableHasType<double>(variable) || dataset.VariableHasType<string>(variable)).ToList();
    Partitions = new() { [PartitionType.Training] = new(0, dataset.Rows / 2), [PartitionType.Test] = new(dataset.Rows / 2, dataset.Rows), [PartitionType.Validation] = new(0, 0), [PartitionType.All] = new(0, dataset.Rows) };
  }

  public enum PartitionType {
    Training,
    Test,
    Validation,
    All
  }
}
