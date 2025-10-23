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

namespace HEAL.HeuristicLib.Operators.SymbolicExpression.Mutators;

public sealed class OnePointShaker : SymbolicExpressionTreeManipulator {
  #region properties
  public double ShakingFactor {
    get;
    set;
  } = 1.0;
  #endregion

  public static SymbolicExpressionTree Shake(IRandomNumberGenerator random, SymbolicExpressionTree tree, double shakingFactor) {
    tree = new SymbolicExpressionTree(tree);
    var parametricNodes = new List<SymbolicExpressionTreeNode?>();
    tree.Root.ForEachNodePostfix(n => {
      if (n!.HasLocalParameters) parametricNodes.Add(n);
    });
    if (parametricNodes.Count <= 0)
      return tree;
    parametricNodes.SampleRandom(random)!.ShakeLocalParameters(random, shakingFactor);
    return tree;
  }

  public override SymbolicExpressionTree Mutate(SymbolicExpressionTree parent, IRandomNumberGenerator random, SymbolicExpressionTreeEncoding encoding) {
    return Shake(random, parent, ShakingFactor);
  }
}
