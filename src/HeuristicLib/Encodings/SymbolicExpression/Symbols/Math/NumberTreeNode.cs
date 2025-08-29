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

public sealed class NumberTreeNode : SymbolicExpressionTreeNode {
  public new Number Symbol => (Number)base.Symbol;

  public double Value { get; set; }

  public NumberTreeNode(Number numberSymbol) : base(numberSymbol) { }

  public NumberTreeNode(NumberTreeNode original) : base(original) {
    Value = original.Value;
  }

  public NumberTreeNode(double value) : this(new Number()) {
    Value = value;
  }

  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    var range = Symbol.MaxValue - Symbol.MinValue;
    Value = random.Random() * range + Symbol.MinValue;
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);
    // 50% additive & 50% multiplicative
    if (random.Random() < 0.5) {
      var x = NormalDistributedRandomPolar.NextDouble(random, Symbol.ManipulatorMu, Symbol.ManipulatorSigma);
      Value = Value + x * shakingFactor;
    } else {
      var x = NormalDistributedRandomPolar.NextDouble(random, 1.0, Symbol.MultiplicativeManipulatorSigma);
      Value = Value * x;
    }
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new NumberTreeNode(this);
  }

  public override string ToString() {
    return $"{Value:E4}";
  }
}
