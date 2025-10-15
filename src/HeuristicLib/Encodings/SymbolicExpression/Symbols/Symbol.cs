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

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;

public abstract class Symbol {
  #region Properties
  private double initialFrequency = 1.0;
  public double InitialFrequency {
    get => initialFrequency;
    set {
      if (value < 0.0) throw new ArgumentException("InitialFrequency must be positive");
      if (value.IsAlmost(initialFrequency)) return;
      initialFrequency = value;
    }
  }
  public bool Enabled {
    get;
    set;
  } = true;

  public bool Fixed {
    get;
    set;
  }

  public abstract int MinimumArity { get; }
  public abstract int MaximumArity { get; }
  #endregion

  public virtual SymbolicExpressionTreeNode CreateTreeNode() {
    return new SymbolicExpressionTreeNode(this);
  }

  public virtual IEnumerable<Symbol> Flatten() {
    yield return this;
  }
}
