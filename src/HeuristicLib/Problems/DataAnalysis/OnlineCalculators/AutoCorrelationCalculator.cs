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

public class AutoCorrelationCalculator {
  public static double[] Calculate(double[] values, out OnlineCalculatorError error) {
    if (values.Any(x => double.IsNaN(x) || double.IsInfinity(x))) {
      error = OnlineCalculatorError.InvalidValueAdded;
      return [];
    }

    error = OnlineCalculatorError.None;
    return CircularCrossCorrelation(values, values);
  }

  public static double[] CircularCrossCorrelation(double[] x, double[] y) {
    int n = Math.Max(x.Length, y.Length);
    double[] result = new double[n];

    for (int shift = 0; shift < n; shift++) {
      double sum = 0.0;
      for (int i = 0; i < n; i++) {
        double xi = x[i % x.Length];
        double yi = y[(i + shift) % y.Length];
        sum += xi * yi;
      }

      result[shift] = sum;
    }

    return result;
  }
}
