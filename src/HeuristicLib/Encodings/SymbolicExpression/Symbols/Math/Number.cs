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

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;

public sealed class Number : Symbol {
  #region Properties
  public double MinValue { get; set; }
  public double MaxValue { get; set; }
  private double manipulatorMu;
  public double ManipulatorMu {
    get => manipulatorMu;
    set {
      manipulatorMu = value;
    }
  }
  private double manipulatorSigma;
  public double ManipulatorSigma {
    get => manipulatorSigma;
    set {
      if (value < 0)
        throw new ArgumentException();
      if (value != manipulatorSigma) {
        manipulatorSigma = value;
      }
    }
  }
  private double multiplicativeManipulatorSigma;
  public double MultiplicativeManipulatorSigma {
    get => multiplicativeManipulatorSigma;
    set {
      if (value < 0)
        throw new ArgumentException();
      if (value != multiplicativeManipulatorSigma) {
        multiplicativeManipulatorSigma = value;
      }
    }
  }

  public override int MinimumArity => 0;
  public override int MaximumArity => 0;
  #endregion

  private Number(Number original) {
    MinValue = original.MinValue;
    MaxValue = original.MaxValue;
    manipulatorMu = original.manipulatorMu;
    manipulatorSigma = original.manipulatorSigma;
    multiplicativeManipulatorSigma = original.multiplicativeManipulatorSigma;
  }

  public Number() {
    manipulatorMu = 0.0;
    manipulatorSigma = 1.0;
    multiplicativeManipulatorSigma = 0.03;
    MinValue = -20.0;
    MaxValue = 20.0;
  }

  public override SymbolicExpressionTreeNode CreateTreeNode() {
    return new NumberTreeNode(this);
  }
}
