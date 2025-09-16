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

using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Clustering {
  public class ClusteringProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, ICollection<IClusteringEvaluator> objective, IComparer<ObjectiveVector> a, TEncoding encoding)
    : DataAnalysisProblem<TProblemData, TSolution, TEncoding>(problemData, new(objective.Select(x => x.Direction).ToArray(), a), encoding)
    where TProblemData : ClusteringProblemData
    where TEncoding : class, IEncoding<TSolution>
    where TSolution : IClusteringModel {
    public List<IClusteringEvaluator> Evaluators { get; set; } = objective.ToList();

    public override ObjectiveVector Evaluate(TSolution solution) {
      var predictions = solution.GetClusterValues(ProblemData.Dataset, ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate());
      if (Evaluators.Count == 1)
        return new(Evaluators[0].Evaluate(ProblemData, DataAnalysisProblemData.PartitionType.Training, predictions));

      if (predictions is not ICollection<int> materialPredictions)
        materialPredictions = predictions.ToArray();
      return new(Evaluators.Select(x => x.Evaluate(ProblemData, DataAnalysisProblemData.PartitionType.Training, materialPredictions)).ToArray());
    }
  }
}
