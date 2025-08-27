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

public class NormalizedGiniCalculator {
  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) {
    var originalValuesArr = originalValues.ToArray();
    var estimatedValuesArr = estimatedValues.ToArray();
    if (originalValuesArr.Length != estimatedValuesArr.Length) {
      throw new ArgumentException("Number of elements in originalValues and estimatedValues enumerations doesn't match.");
    }

    var oe = Gini(originalValuesArr, estimatedValuesArr, out errorState);
    if (errorState != OnlineCalculatorError.None) return double.NaN;

    return oe / Gini(originalValuesArr, originalValuesArr, out errorState);
  }

  private static double Gini(IEnumerable<double> original, IEnumerable<double> estimated, out OnlineCalculatorError errorState) {
    var pairs =
      estimated.Zip(original, (e, o) => new { e, o })
               .OrderByDescending(p => p.e);
    errorState = pairs.Any() ? OnlineCalculatorError.None : OnlineCalculatorError.InsufficientElementsAdded;
    var giniSum = 0.0;
    var sumOriginal = 0.0;
    var n = 0;
    foreach (var p in pairs) {
      if (double.IsNaN(p.o) || double.IsNaN(p.e)) {
        errorState = OnlineCalculatorError.InvalidValueAdded;
        return double.NaN;
      }

      sumOriginal += p.o;
      giniSum += sumOriginal;
      n++;
    }

    giniSum /= sumOriginal;

    return (giniSum - (n + 1) / 2.0) / n;
  }
}
