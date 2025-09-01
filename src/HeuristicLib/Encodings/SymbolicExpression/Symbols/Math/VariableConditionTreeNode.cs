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

using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;

public sealed class VariableConditionTreeNode : SymbolicExpressionTreeNode {
  #region properties
  public new VariableCondition Symbol => (VariableCondition)base.Symbol;
  public double Threshold { get; set; }
  public string VariableName { get; set; } = "";
  public double Slope { get; set; }
  #endregion

  private VariableConditionTreeNode(VariableConditionTreeNode original)
    : base(original) {
    Threshold = original.Threshold;
    VariableName = original.VariableName;
    Slope = original.Slope;
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new VariableConditionTreeNode(this);
  }

  public VariableConditionTreeNode(VariableCondition variableConditionSymbol) : base(variableConditionSymbol) { }
  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandom random) {
    base.ResetLocalParameters(random);
    Threshold = NormalDistributedRandomPolar.NextDouble(random, Symbol.ThresholdInitializerMu, Symbol.ThresholdInitializerSigma);
    VariableName = Symbol.VariableNames.SampleRandom(random);
    Slope = NormalDistributedRandomPolar.NextDouble(random, Symbol.SlopeInitializerMu, Symbol.SlopeInitializerSigma);
  }

  public override void ShakeLocalParameters(IRandom random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);
    var x = NormalDistributedRandomPolar.NextDouble(random, Symbol.ThresholdManipulatorMu, Symbol.ThresholdManipulatorSigma);
    Threshold += x * shakingFactor;
    VariableName = Symbol.VariableNames.SampleRandom(random);

    x = NormalDistributedRandomPolar.NextDouble(random, Symbol.SlopeManipulatorMu, Symbol.SlopeManipulatorSigma);
    Slope += x * shakingFactor;
  }

  public override string ToString() {
    if (Slope.IsAlmost(0.0) || Symbol.IgnoreSlope) {
      return VariableName + " < " + Threshold.ToString("E4");
    }

    return VariableName + " > " + Threshold.ToString("E4") + Environment.NewLine +
           "slope: " + Slope.ToString("E4");
  }
}
