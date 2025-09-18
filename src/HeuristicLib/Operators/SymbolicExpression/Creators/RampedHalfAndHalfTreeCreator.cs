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

using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Creators;

public class RampedHalfAndHalfTreeCreator : SymbolicExpressionTreeCreator {
  /// <summary>
  /// Create a symbolic expression tree using 'RampedHalfAndHalf' strategy.
  /// Half the trees are created with the 'Grow' method, and the other half are created with the 'Full' method.
  /// </summary>
  /// <param name="random">Random generator</param>
  /// <param name="grammar">Available tree grammar</param>
  /// <param name="maxTreeLength">Maximum tree length (this parameter is ignored)</param>
  /// <param name="maxTreeDepth">Maximum tree depth</param>
  /// <returns></returns>
  public static SymbolicExpressionTree CreateTree(IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    var tree = encoding.Grammar.MakeStump(random);
    var startNode = tree.Root.GetSubtree(0);

    if (random.Random() < 0.5)
      GrowTreeCreator.Create(random, startNode, encoding.TreeDepth - 2, encoding);
    else
      FullTreeCreator.Create(random, startNode, encoding, encoding.TreeDepth - 2);

    return tree;
  }

  public override SymbolicExpressionTree Create(IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) => CreateTree(random, encoding);
}
