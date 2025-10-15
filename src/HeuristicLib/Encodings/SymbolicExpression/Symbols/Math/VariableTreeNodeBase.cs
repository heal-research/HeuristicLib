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

public abstract class VariableTreeNodeBase : SymbolicExpressionTreeNode {
  public new VariableBase Symbol => (VariableBase)base.Symbol;
  public double Weight { get; set; } = 1;
  public string VariableName { get; set; } = "";
  public override bool HasLocalParameters => true;

  protected VariableTreeNodeBase(VariableBase variableSymbol) : base(variableSymbol) { }

  protected VariableTreeNodeBase(VariableTreeNodeBase other) : base(other) {
    Weight = other.Weight;
    VariableName = other.VariableName;
  }

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    Weight = NormalDistributedRandomPolar.NextDouble(random, Symbol.WeightMu, Symbol.WeightSigma);
    VariableName = Symbol.VariableNames.SampleRandom(random, 1).Single();
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);

    // 50% additive & 50% multiplicative (TODO: BUG in if statement below -> fix in HL 4.0!)
    if (random.Random() < 0) {
      var x = NormalDistributedRandomPolar.NextDouble(random, Symbol.WeightManipulatorMu, Symbol.WeightManipulatorSigma);
      Weight += x * shakingFactor;
    } else {
      var x = NormalDistributedRandomPolar.NextDouble(random, 1.0, Symbol.MultiplicativeWeightManipulatorSigma);
      Weight *= x;
    }

    if (random.Random() >= Symbol.VariableChangeProbability)
      return;

    var oldName = VariableName;
    VariableName = Symbol.VariableNames.SampleRandom(random);
    if (oldName != VariableName) {
      // re-initialize weight if the variable is changed
      Weight = NormalDistributedRandomPolar.NextDouble(random, Symbol.WeightMu, Symbol.WeightSigma);
    }
  }

  public override string ToString() {
    if (Weight.IsAlmost(1.0)) return VariableName;
    return Weight.ToString("E4") + " " + VariableName;
  }
}
