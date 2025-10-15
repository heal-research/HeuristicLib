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

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Classification {
  public class ClassificationProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, ICollection<IClassificationEvaluator> objective, IComparer<ObjectiveVector> a, TEncoding encoding)
    : DataAnalysisProblem<TProblemData, TSolution, TEncoding>(problemData, new Objective(objective.Select(x => x.Direction).ToArray(), a), encoding)
    where TProblemData : ClassificationProblemData
    where TEncoding : class, IEncoding<TSolution>
    where TSolution : IRegressionModel {
    public List<IClassificationEvaluator> Evaluators { get; set; } = objective.ToList();

    private double[]? trainingTargetCache;

    public override ObjectiveVector Evaluate(TSolution solution) {
      trainingTargetCache ??= ProblemData.TargetVariableValues(DataAnalysisProblemData.PartitionType.Training).ToArray();
      var predictions = solution.Predict(ProblemData.Dataset, ProblemData.Partitions[DataAnalysisProblemData.PartitionType.Training].Enumerate());
      if (Evaluators.Count == 1)
        return new ObjectiveVector(Evaluators[0].Evaluate(trainingTargetCache, predictions));

      if (predictions is not ICollection<double> materialPredictions)
        materialPredictions = predictions.ToArray();
      return new ObjectiveVector(Evaluators.Select(x => x.Evaluate(trainingTargetCache, materialPredictions)).ToArray());
    }
  }
}
