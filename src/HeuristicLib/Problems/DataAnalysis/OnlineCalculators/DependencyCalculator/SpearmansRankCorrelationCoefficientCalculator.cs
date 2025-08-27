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

namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators.DependencyCalculator;

public class SpearmansRankCorrelationCoefficientCalculator : IDependencyCalculator {
  public double Maximum => 1.0;

  public double Minimum => -1.0;

  public string Name => "Spearmans Rank";

  public double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) {
    return CalculateSpearmansRank(originalValues, estimatedValues, out errorState);
  }

  public double Calculate(IEnumerable<Tuple<double, double>> values, out OnlineCalculatorError errorState) {
    return CalculateSpearmansRank(values.Select(v => v.Item1), values.Select(v => v.Item2), out errorState);
  }

  public static double CalculateSpearmansRank(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) {
    errorState = OnlineCalculatorError.None;
    var xs = originalValues as double[] ?? originalValues.ToArray();
    var ys = estimatedValues as double[] ?? estimatedValues.ToArray();
    if (xs.Length != ys.Length)
      throw new ArgumentException("values must be the same length");
    if (xs.Length == 0)
      throw new ArgumentException("Values must not be empty");

    var rx = GetRanks(xs);
    var ry = GetRanks(ys);

    return Pearson(rx, ry);
  }

  private static double[] GetRanks(double[] values) {
    int n = values.Length;
    var sorted = values
                 .Select((v, i) => (Value: v, Index: i))
                 .OrderBy(x => x.Value)
                 .ToArray();

    double[] ranks = new double[n];
    int i = 0;
    while (i < n) {
      int j = i;
      while (j < n && sorted[j].Value.Equals(sorted[i].Value))
        j++;

      // average rank for ties
      double rank = (i + j - 1) / 2.0 + 1.0;

      for (int k = i; k < j; k++)
        ranks[sorted[k].Index] = rank;

      i = j;
    }

    return ranks;
  }

  private static double Pearson(double[] xs, double[] ys) {
    int n = xs.Length;
    double meanX = xs.Average();
    double meanY = ys.Average();

    double num = 0.0, denX = 0.0, denY = 0.0;
    for (int i = 0; i < n; i++) {
      double dx = xs[i] - meanX;
      double dy = ys[i] - meanY;
      num += dx * dy;
      denX += dx * dx;
      denY += dy * dy;
    }

    return num / Math.Sqrt(denX * denY);
  }
}
