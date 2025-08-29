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

public sealed class VariableCondition : Symbol {
  #region properties
  public double ThresholdInitializerMu { get; set; } = 0.0;
  private double thresholdInitializerSigma = 0.1;
  public double ThresholdInitializerSigma {
    get => thresholdInitializerSigma;
    set {
      if (thresholdInitializerSigma < 0.0)
        throw new ArgumentException("Negative sigma is not allowed.");
      thresholdInitializerSigma = value;
    }
  }

  public double ThresholdManipulatorMu { get; set; } = 0.0;
  private double thresholdManipulatorSigma = 0.1;
  public double ThresholdManipulatorSigma {
    get => thresholdManipulatorSigma;
    set {
      if (thresholdManipulatorSigma < 0.0)
        throw new ArgumentException("Negative sigma is not allowed.");
      thresholdManipulatorSigma = value;
    }
  }

  private readonly List<string> variableNames = [];
  public IReadOnlyList<string> VariableNames {
    get => variableNames;
    set {
      variableNames.Clear();
      variableNames.AddRange(value);
    }
  }

  private List<string> allVariableNames = [];
  public IReadOnlyList<string> AllVariableNames {
    get => allVariableNames;
    set {
      allVariableNames.Clear();
      allVariableNames.AddRange(value);
    }
  }

  public double SlopeInitializerMu { get; set; } = 0.0;
  private double slopeInitializerSigma;
  public double SlopeInitializerSigma {
    get => slopeInitializerSigma;
    set {
      if (slopeInitializerSigma < 0.0)
        throw new ArgumentException("Negative sigma is not allowed.");
      slopeInitializerSigma = value;
    }
  }

  public double SlopeManipulatorMu { get; set; } = 0.0;
  private double slopeManipulatorSigma = 0.0;
  public double SlopeManipulatorSigma {
    get => slopeManipulatorSigma;
    set {
      if (slopeManipulatorSigma < 0.0)
        throw new ArgumentException("Negative sigma is not allowed.");
      slopeManipulatorSigma = value;
    }
  }

  /// <summary>
  /// Flag to indicate if the interpreter should ignore the slope parameter (introduced for representation of expression trees)
  /// </summary>

  public bool IgnoreSlope { get; set; }

  public override int MinimumArity => 2;
  public override int MaximumArity => 2;
  #endregion

  public override SymbolicExpressionTreeNode CreateTreeNode() {
    return new VariableConditionTreeNode(this);
  }
}
