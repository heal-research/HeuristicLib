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

public class OnlinePearsonsRCalculator {
  private readonly OnlineCovarianceCalculator covCalculator = new();
  private readonly OnlineMeanAndVarianceCalculator sxCalculator = new();
  private readonly OnlineMeanAndVarianceCalculator syCalculator = new();

  public double R {
    get {
      var xVar = sxCalculator.PopulationVariance;
      var yVar = syCalculator.PopulationVariance;
      if (xVar.IsAlmost(0.0) || yVar.IsAlmost(0.0)) {
        return 0.0;
      }

      var r = covCalculator.Covariance / (Math.Sqrt(xVar) * Math.Sqrt(yVar));
      r = r switch {
        < -1.0 => -1.0,
        > 1.0 => 1.0,
        _ => r
      };
      return r;
    }
  }

  public OnlinePearsonsRCalculator() { }

  #region IOnlineCalculator Members
  public OnlineCalculatorError ErrorState => covCalculator.ErrorState | sxCalculator.PopulationVarianceErrorState | syCalculator.PopulationVarianceErrorState;
  public double Value => R;

  public void Reset() {
    covCalculator.Reset();
    sxCalculator.Reset();
    syCalculator.Reset();
  }

  public void Add(double x, double y) {
    // no need to check validity of values explicitly here as it is checked in all three evaluators 
    covCalculator.Add(x, y);
    sxCalculator.Add(x);
    syCalculator.Add(y);
  }
  #endregion

  public static double Calculate(IEnumerable<double> first, IEnumerable<double> second, out OnlineCalculatorError errorState) {
    using var firstEnumerator = first.GetEnumerator();
    using var secondEnumerator = second.GetEnumerator();
    var calculator = new OnlinePearsonsRCalculator();

    // always move forward both enumerators (do not use short-circuit evaluation!)
    while (firstEnumerator.MoveNext() & secondEnumerator.MoveNext()) {
      var original = firstEnumerator.Current;
      var estimated = secondEnumerator.Current;
      calculator.Add(original, estimated);
      if (calculator.ErrorState != OnlineCalculatorError.None) break;
    }

    // check if both enumerators are at the end to make sure both enumerations have the same length
    if (calculator.ErrorState == OnlineCalculatorError.None &&
        (secondEnumerator.MoveNext() || firstEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in first and second enumeration doesn't match.");
    } else {
      errorState = calculator.ErrorState;
      return calculator.R;
    }
  }
}
