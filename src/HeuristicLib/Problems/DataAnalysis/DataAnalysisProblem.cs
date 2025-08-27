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

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public abstract class DataAnalysisProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, Objective objective, TEncoding encoding)
  : Problem<TSolution, TEncoding>(objective, encoding)
  where TProblemData : DataAnalysisProblemData
  where TEncoding : class, IEncoding<TSolution> {
  public TProblemData ProblemData {
    get;
    set;
  } = problemData;
}

public abstract class RegressionProblem<TProblemData, TSolution, TEncoding>(TProblemData problemData, ICollection<IRegressionEvaluator> objective, IComparer<ObjectiveVector> a, TEncoding encoding)
  : DataAnalysisProblem<TProblemData, TSolution, TEncoding>(problemData, new(objective.Select(x => x.Direction).ToArray(), a), encoding)
  where TProblemData : DataAnalysisProblemData
  where TEncoding : class, IEncoding<TSolution>
  where TSolution : IRegressionModel {
  public List<IRegressionEvaluator> Evaluators { get; set; } = objective.ToList();
  public override ObjectiveVector Evaluate(TSolution solution) => throw new NotImplementedException();
}

public interface IRegressionEvaluator {
  public ObjectiveDirection Direction { get; }
  public double Evaluate(IEnumerable<double> trueValues, IEnumerable<double> predictedValues);
}

public interface IRegressionModel {
  IEnumerable<double> Predict(Dataset data, IEnumerable<int> rows);

  double Predict(Dataset data, int rows) => Predict(data, [rows]).First();
}
