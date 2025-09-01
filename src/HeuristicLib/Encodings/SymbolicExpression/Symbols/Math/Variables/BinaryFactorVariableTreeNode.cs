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

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math.Variables;

public sealed class BinaryFactorVariableTreeNode : VariableTreeNodeBase {
  public new BinaryFactorVariable Symbol => (BinaryFactorVariable)base.Symbol;

  public string VariableValue { get; set; } = "";

  private BinaryFactorVariableTreeNode(BinaryFactorVariableTreeNode original) : base(original) {
    VariableValue = original.VariableValue;
  }

  public BinaryFactorVariableTreeNode(BinaryFactorVariable variableSymbol) : base(variableSymbol) { }

  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandom random) {
    base.ResetLocalParameters(random);
    VariableValue = Symbol.GetVariableValues(VariableName).SampleRandom(random);
  }

  public override void ShakeLocalParameters(IRandom random, double shakingFactor) {
    // 50% additive & 50% multiplicative (override of functionality of base class because of a BUG)
    if (random.Random() < 0.5) {
      var x = NormalDistributedRandomPolar.NextDouble(random, Symbol.WeightManipulatorMu, Symbol.WeightManipulatorSigma);
      Weight = Weight + x * shakingFactor;
    } else {
      var x = NormalDistributedRandomPolar.NextDouble(random, 1.0, Symbol.MultiplicativeWeightManipulatorSigma);
      Weight = Weight * x;
    }

    if (random.Random() < Symbol.VariableChangeProbability) {
      var oldName = VariableName;
      VariableName = Symbol.VariableNames.SampleRandom(random);
      // reinitialize weights if variable has changed (similar to FactorVariableTreeNode)
      if (oldName != VariableName)
        Weight = NormalDistributedRandomPolar.NextDouble(random, Symbol.WeightMu, Symbol.WeightSigma);
    }

    VariableValue = Symbol.GetVariableValues(VariableName).SampleRandom(random);
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new BinaryFactorVariableTreeNode(this);
  }

  public override string ToString() {
    return base.ToString() + " = " + VariableValue;
  }
}
