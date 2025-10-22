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

using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;

public interface ISymbolicExpressionGrammar : ISymbolicExpressionGrammarBase {
  ProgramRootSymbol ProgramRootSymbol { get; }
  StartSymbol StartSymbol { get; }

  int MinimumFunctionDefinitions { get; set; }
  int MaximumFunctionDefinitions { get; set; }
  int MinimumFunctionArguments { get; set; }
  int MaximumFunctionArguments { get; set; }

  bool ReadOnly { get; set; }

  public SymbolicExpressionTree MakeStump(IRandomNumberGenerator random) {
    var rootNode = ProgramRootSymbol.CreateTreeNode();
    var tree = new SymbolicExpressionTree(rootNode);
    if (rootNode.HasLocalParameters) rootNode.ResetLocalParameters(random);
    var startNode = StartSymbol.CreateTreeNode();
    if (startNode.HasLocalParameters) startNode.ResetLocalParameters(random);
    rootNode.AddSubtree(startNode);
    tree.Root = rootNode;
    return tree;
  }

  public Symbol AddLinearScaling() {
    var rem = GetAllowedChildSymbols(StartSymbol).ToArray();
    foreach (var child in rem) {
      RemoveAllowedChildSymbol(StartSymbol, child);
    }

    var rem2 = GetAllowedChildSymbols(StartSymbol, 0).ToArray();
    foreach (var child in rem2) {
      RemoveAllowedChildSymbol(StartSymbol, child, 0);
    }

    var offset = new Number();
    var intercept = new Number();
    var mult = new Multiplication();
    var add = new Addition();
    AddSymbol(offset);
    AddSymbol(intercept);
    AddSymbol(mult);
    AddSymbol(add);
    AddAllowedChildSymbol(StartSymbol, add);
    AddAllowedChildSymbol(add, mult, 0);
    AddAllowedChildSymbol(add, offset, 1);
    AddAllowedChildSymbol(mult, intercept, 1);

    foreach (var symbol in rem.Concat(rem2)) {
      AddAllowedChildSymbol(mult, symbol, 0);
    }

    return mult;
  }

  public void AddFullyConnectedSymbols(ICollection<Symbol> symbols, Symbol root) {
    foreach (var symbol in symbols) {
      AddSymbol(symbol);
      AddAllowedChildSymbol(root, symbol, 0);
      if (symbol.MaximumArity == 0)
        continue;
      foreach (var symbol1 in symbols) {
        AddAllowedChildSymbol(symbol, symbol1);
      }
    }
  }

  bool Conforms(SymbolicExpressionTree symbolicExpressionTree);
}
