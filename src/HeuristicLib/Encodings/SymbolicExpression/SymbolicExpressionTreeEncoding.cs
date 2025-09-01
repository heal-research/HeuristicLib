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

using HEAL.HeuristicLib.Encodings.SymbolicExpression.Grammars;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;
using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression;

public sealed record SymbolicExpressionTreeEncoding(ISymbolicExpressionGrammar Grammar, int TreeLength, int TreeDepth)
  : Encoding<SymbolicExpressionTree> {
  #region Parameter properties
  public int TreeLength { get; } = TreeLength;

  public int TreeDepth { get; } = TreeDepth;

  public ISymbolicExpressionGrammar Grammar { get; } = Grammar;

  public int FunctionDefinitions { get; set; }

  public int FunctionArguments { get; set; }
  #endregion

  public SymbolicExpressionTreeEncoding(ISymbolicExpressionGrammar grammar) : this(grammar, 50, 50) { }

  public override bool Contains(SymbolicExpressionTree genotype) {
    return genotype.Length <= TreeLength &&
           genotype.Depth <= TreeDepth
      //TODO && Grammar.Conforms(genotype)
      ;
  }
}
