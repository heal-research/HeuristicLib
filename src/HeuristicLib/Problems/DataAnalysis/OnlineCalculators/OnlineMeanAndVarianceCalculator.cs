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

public class OnlineMeanAndVarianceCalculator {
  private double m_oldM, m_newM, m_oldS, m_newS;

  public OnlineCalculatorError VarianceErrorState { get; private set; }

  public double Variance => Count > 1 ? m_newS / (Count - 1) : 0.0;

  public OnlineCalculatorError PopulationVarianceErrorState { get; private set; }
  public double PopulationVariance => Count > 0 ? m_newS / Count : 0.0;

  public OnlineCalculatorError MeanErrorState => PopulationVarianceErrorState;
  public double Mean => Count > 0 ? m_newM : 0.0;

  public int Count { get; private set; }

  public OnlineMeanAndVarianceCalculator() {
    Reset();
  }

  public void Reset() {
    Count = 0;
    PopulationVarianceErrorState = OnlineCalculatorError.InsufficientElementsAdded;
    VarianceErrorState = OnlineCalculatorError.InsufficientElementsAdded;
  }

  public void Add(double x) {
    if (double.IsNaN(x) || double.IsInfinity(x) || x > 1E13 || x < -1E13 || (PopulationVarianceErrorState & OnlineCalculatorError.InvalidValueAdded) > 0) {
      PopulationVarianceErrorState |= OnlineCalculatorError.InvalidValueAdded;
      VarianceErrorState |= OnlineCalculatorError.InvalidValueAdded;
    } else {
      Count++;
      // See Knuth TAOCP vol 2, 3rd edition, page 232
      if (Count == 1) {
        m_oldM = m_newM = x;
        m_oldS = 0.0;
        PopulationVarianceErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded; // n >= 1
      } else {
        VarianceErrorState &= ~OnlineCalculatorError.InsufficientElementsAdded; // n >= 2
        m_newM = m_oldM + (x - m_oldM) / Count;
        m_newS = m_oldS + (x - m_oldM) * (x - m_newM);

        // set up for next iteration
        m_oldM = m_newM;
        m_oldS = m_newS;
      }
    }
  }

  public static void Calculate(IEnumerable<double> x, out double mean, out double variance, out OnlineCalculatorError meanErrorState, out OnlineCalculatorError varianceErrorState) {
    var meanAndVarianceCalculator = new OnlineMeanAndVarianceCalculator();
    foreach (var xi in x) {
      meanAndVarianceCalculator.Add(xi);
    }

    mean = meanAndVarianceCalculator.Mean;
    variance = meanAndVarianceCalculator.Variance;
    meanErrorState = meanAndVarianceCalculator.MeanErrorState;
    varianceErrorState = meanAndVarianceCalculator.VarianceErrorState;
  }
}
