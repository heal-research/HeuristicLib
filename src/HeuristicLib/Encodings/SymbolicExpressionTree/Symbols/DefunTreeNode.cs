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

using HeuristicLab.Common;
using HEAL.Attic;
namespace HeuristicLab.Encodings.SymbolicExpressionTreeEncoding {
  [StorableType("0DEAAC29-2BC7-4018-8CAA-ACF77F8C89C8")]
  public sealed class DefunTreeNode : SymbolicExpressionTreeTopLevelNode {
    private int numberOfArguments;
    private string functionName;
    [Storable]
    public int NumberOfArguments {
      get => numberOfArguments;
      set => numberOfArguments = value;
    }
    [Storable]
    public string FunctionName {
      get => functionName;
      set => functionName = value;
    }

    [StorableConstructor]
    private DefunTreeNode(StorableConstructorFlag _) : base(_) { }
    private DefunTreeNode(DefunTreeNode original, Cloner cloner)
      : base(original, cloner) {
      FunctionName = original.FunctionName;
      NumberOfArguments = original.NumberOfArguments;
    }

    public DefunTreeNode(Defun defunSymbol) : base(defunSymbol) { }

    public override IDeepCloneable Clone(Cloner cloner) {
      return new DefunTreeNode(this, cloner);
    }

    public override string ToString() {
      return FunctionName;
    }
  }
}
