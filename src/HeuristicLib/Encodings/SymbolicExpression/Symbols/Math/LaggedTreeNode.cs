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

public class LaggedTreeNode : SymbolicExpressionTreeNode {
  public new LaggedSymbol Symbol => (LaggedSymbol)base.Symbol;

  public int Lag { get; set; }

  protected LaggedTreeNode(LaggedTreeNode original) : base(original) {
    Lag = original.Lag;
  }

  public LaggedTreeNode(LaggedSymbol timeLagSymbol) : base(timeLagSymbol) { }

  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandom random) {
    base.ResetLocalParameters(random);
    Lag = random.Integer(Symbol.MinLag, Symbol.MaxLag + 1);
  }

  public override void ShakeLocalParameters(IRandom random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);
    Lag = System.Math.Min(Symbol.MaxLag, System.Math.Max(Symbol.MinLag, Lag + random.Integer(-1, 2)));
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new LaggedTreeNode(this);
  }
}
