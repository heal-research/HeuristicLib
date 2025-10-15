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

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math.Variables;

public sealed class FactorVariableTreeNode : SymbolicExpressionTreeNode {
  public new FactorVariable Symbol => (FactorVariable)base.Symbol;
  public double[]? Weights { get; set; }
  public string VariableName { get; set; } = "";

  private FactorVariableTreeNode(FactorVariableTreeNode original) : base(original) {
    VariableName = original.VariableName;
    if (original.Weights == null)
      return;
    Weights = new double[original.Weights.Length];
    Array.Copy(original.Weights, Weights, Weights.Length);
  }

  public FactorVariableTreeNode(FactorVariable variableSymbol)
    : base(variableSymbol) { }

  public override SymbolicExpressionTreeNode Clone() {
    return new FactorVariableTreeNode(this);
  }

  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    VariableName = Symbol.VariableNames.SampleRandom(random);
    Weights =
      Symbol.GetVariableValues(VariableName)
            .Select(_ => NormalDistributedRandomPolar.NextDouble(random, 0, 1)).ToArray();
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    // mutate only one randomly selected weight
    var idx = random.Integer(Weights!.Length);
    // 50% additive & 50% multiplicative
    if (random.Random() < 0.5) {
      var x = NormalDistributedRandomPolar.NextDouble(random, Symbol.WeightManipulatorMu,
        Symbol.WeightManipulatorSigma);
      Weights[idx] += x * shakingFactor;
    } else {
      var x = NormalDistributedRandomPolar.NextDouble(random, 1.0, Symbol.MultiplicativeWeightManipulatorSigma);
      Weights[idx] *= x;
    }

    if (random.Random() >= Symbol.VariableChangeProbability) return;

    VariableName = Symbol.VariableNames.SampleRandom(random);
    if (Weights.Length != Symbol.GetVariableValues(VariableName).Count()) {
      // if the length of the weight array does not match => re-initialize weights
      Weights =
        Symbol.GetVariableValues(VariableName)
              .Select(_ => NormalDistributedRandomPolar.NextDouble(random, 0, 1))
              .ToArray();
    }
  }

  public double GetValue(string cat) {
    return Weights![Symbol.GetIndexForValue(VariableName, cat)];
  }

  public override string ToString() {
    var weightStr = string.Join("; ",
      Symbol.GetVariableValues(VariableName).Select(value => value + ": " + GetValue(value).ToString("E4")));
    return VariableName + " (factor) "
                        + "[" + weightStr + "]";
  }
}
