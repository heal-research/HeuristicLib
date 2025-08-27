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

public class OnlineWeightedDirectionalSymmetryCalculator {
  private int n;
  private double correctSum;
  private double incorrectSum;

  public double WeightedDirectionalSymmetry {
    get {
      if (n <= 1) return 0.0;
      return incorrectSum / correctSum;
    }
  }

  public OnlineWeightedDirectionalSymmetryCalculator() {
    Reset();
  }

  public double Value => WeightedDirectionalSymmetry;

  public OnlineCalculatorError ErrorState { get; private set; }

  public void Add(double startValue, IEnumerable<double> actualContinuation, IEnumerable<double> predictedContinuation) {
    if (double.IsNaN(startValue) || (ErrorState & OnlineCalculatorError.InvalidValueAdded) > 0) {
      ErrorState |= OnlineCalculatorError.InvalidValueAdded;
    } else {
      using var actualEnumerator = actualContinuation.GetEnumerator();
      using var predictedEnumerator = predictedContinuation.GetEnumerator();
      while (actualEnumerator.MoveNext() & predictedEnumerator.MoveNext() & ErrorState != OnlineCalculatorError.InvalidValueAdded) {
        var actual = actualEnumerator.Current;
        var predicted = predictedEnumerator.Current;
        if (double.IsNaN(actual) || double.IsNaN(predicted)) {
          ErrorState |= OnlineCalculatorError.InvalidValueAdded;
        } else {
          var err = Math.Abs(actual - predicted);
          // count as correct only if the trend (positive/negative/no change) is predicted correctly
          if ((actual - startValue) * (predicted - startValue) > 0.0 ||
              (actual - startValue).IsAlmost(predicted - startValue)) {
            correctSum += err;
          } else {
            incorrectSum += err;
          }

          n++;
        }
      }

      // check if both enumerators are at the end to make sure both enumerations have the same length
      if (actualEnumerator.MoveNext() || predictedEnumerator.MoveNext()) {
        ErrorState |= OnlineCalculatorError.InvalidValueAdded;
      } else {
        ErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded; // n >= 1
      }
    }
  }

  public void Reset() {
    n = 0;
    correctSum = 0;
    incorrectSum = 0;
    ErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public static double Calculate(double startValue, IEnumerable<double> actualContinuation, IEnumerable<double> predictedContinuation, out OnlineCalculatorError errorState) {
    var calculator = new OnlineWeightedDirectionalSymmetryCalculator();
    calculator.Add(startValue, actualContinuation, predictedContinuation);
    errorState = calculator.ErrorState;
    return calculator.WeightedDirectionalSymmetry;
  }

  public static double Calculate(IEnumerable<double> startValues, IEnumerable<IEnumerable<double>> actualContinuations, IEnumerable<IEnumerable<double>> predictedContinuations, out OnlineCalculatorError errorState) {
    using var startValueEnumerator = startValues.GetEnumerator();
    using var actualContinuationsEnumerator = actualContinuations.GetEnumerator();
    using var predictedContinuationsEnumerator = predictedContinuations.GetEnumerator();
    var calculator = new OnlineWeightedDirectionalSymmetryCalculator();

    // always move forward all enumerators (do not use short-circuit evaluation!)
    while (startValueEnumerator.MoveNext() & actualContinuationsEnumerator.MoveNext() & predictedContinuationsEnumerator.MoveNext()) {
      calculator.Add(startValueEnumerator.Current, actualContinuationsEnumerator.Current, predictedContinuationsEnumerator.Current);
      if (calculator.ErrorState != OnlineCalculatorError.None) break;
    }

    // check if all enumerators are at the end to make sure both enumerations have the same length
    if (calculator.ErrorState == OnlineCalculatorError.None &&
        (startValueEnumerator.MoveNext() || actualContinuationsEnumerator.MoveNext() || predictedContinuationsEnumerator.MoveNext())) {
      throw new ArgumentException("Number of elements in startValues, actualContinuations and estimatedValues predictedContinuations doesn't match.");
    } else {
      errorState = calculator.ErrorState;
      return calculator.WeightedDirectionalSymmetry;
    }
  }
}
