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

#pragma warning disable S2178
namespace HEAL.HeuristicLib.Problems.DataAnalysis.OnlineCalculators;

public class OnlineMeanAbsolutePercentageErrorCalculator {
  private double sre;
  private int n;
  public double MeanAbsolutePercentageError => n > 0 ? sre / n : 0.0;

  public OnlineMeanAbsolutePercentageErrorCalculator() {
    Reset();
  }

  #region IOnlineCalculator Members
  public OnlineCalculatorError ErrorState { get; private set; }
  public double Value => MeanAbsolutePercentageError;

  public void Reset() {
    n = 0;
    sre = 0.0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public void Add(double original, double estimated) {
    if (double.IsNaN(estimated) || double.IsInfinity(estimated) ||
        double.IsNaN(original) || double.IsInfinity(original) ||
        original.IsAlmost(0.0)) {
      ErrorState |= OnlineCalculatorError.InvalidValueAdded;
    } else {
      sre += Math.Abs((estimated - original) / original);
      n++;
      ErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded; // n >= 1
    }
  }
  #endregion

  public static double Calculate(IEnumerable<double> originalValues, IEnumerable<double> estimatedValues, out OnlineCalculatorError errorState) {
    using var originalEnumerator = originalValues.GetEnumerator();
    using var estimatedEnumerator = estimatedValues.GetEnumerator();
    var calculator = new OnlineMeanAbsolutePercentageErrorCalculator();

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (originalEnumerator.MoveNext() & estimatedEnumerator.MoveNext()) {
      var original = originalEnumerator.Current;
      var estimated = estimatedEnumerator.Current;
      calculator.Add(original, estimated);
      if (calculator.ErrorState != OnlineCalculatorError.None) break;
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (calculator.ErrorState == OnlineCalculatorError.None &&
        (estimatedEnumerator.MoveNext() || originalEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in originalValues and second estimatedValues doesn't match.");
    } else {
      errorState = calculator.ErrorState;
      return calculator.MeanAbsolutePercentageError;
    }
  }
}
