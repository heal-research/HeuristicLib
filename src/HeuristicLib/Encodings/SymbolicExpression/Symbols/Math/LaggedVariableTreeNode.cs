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

public sealed class LaggedVariableTreeNode : VariableTreeNodeBase {
  public new LaggedVariable Symbol => (LaggedVariable)base.Symbol;

  public int Lag { get; set; }

  public override bool HasLocalParameters => true;

  public LaggedVariableTreeNode(LaggedVariableTreeNode other) : base(other) { }
  public LaggedVariableTreeNode(LaggedVariable variableSymbol) : base(variableSymbol) { }

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    Lag = random.Integer(Symbol.MinLag, Symbol.MaxLag + 1);
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);
    Lag = System.Math.Min(Symbol.MaxLag, System.Math.Max(Symbol.MinLag, Lag + random.Integer(-1, 2)));
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new LaggedVariableTreeNode(this);
  }

  public override string ToString() {
    return base.ToString() + " (t" + (Lag > 0 ? "+" : "") + Lag + ")";
  }
}
