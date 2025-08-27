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

namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class ConfusionMatrixCalculator {
  public static readonly double[,] Empty = new double[0, 0];

  //TODO this has heavy multi-enumeration issues and more importantly it is not online 
  public static double[,] Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) {
    var originals = originalValues as double[] ?? originalValues.ToArray();
    var estimated = estimatedValues as double[] ?? estimatedValues.ToArray();
    if (originals.Length == 0 || estimated.Length == 0) {
      errorState = OnlineCalculatorError.InsufficientElementsAdded;
      return Empty;
    }

    if (originals.Length != estimated.Length) {
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumerations doesn't match.");
    }

    var index = 0;
    var classValueIndexMapping = originals
                                 .Distinct()
                                 .OrderBy(x => x)
                                 .ToDictionary(classValue => classValue, _ => index++);

    var classes = classValueIndexMapping.Count;
    var confusionMatrix = new double[classes, classes];

    for (int i = 0; i < originals.Length; i++) {
      if (!classValueIndexMapping.TryGetValue(originals[i], out var originalIndex)) {
        errorState = OnlineCalculatorError.InvalidValueAdded;
        return Empty;
      }

      if (!classValueIndexMapping.TryGetValue(estimated[i], out var estimatedIndex)) {
        errorState = OnlineCalculatorError.InvalidValueAdded;
        return Empty;
      }

      confusionMatrix[estimatedIndex, originalIndex] += 1;
    }

    errorState = OnlineCalculatorError.None;
    return confusionMatrix;
  }
}
