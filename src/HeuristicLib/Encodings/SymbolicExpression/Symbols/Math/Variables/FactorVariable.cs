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

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math.Variables;

public sealed class FactorVariable : VariableBase {
  private readonly Dictionary<string, Dictionary<string, int>> variableValues = new(); // for each variable value also store a zero-based index

  public IEnumerable<KeyValuePair<string, Dictionary<string, int>>> VariableValues {
    get => variableValues;
    set {
      if (value == null) throw new ArgumentNullException();
      variableValues.Clear();
      foreach (var kvp in value) {
        variableValues.Add(kvp.Key, new Dictionary<string, int>(kvp.Value));
      }
    }
  }

  public override SymbolicExpressionTreeNode CreateTreeNode() {
    return new FactorVariableTreeNode(this);
  }

  public IEnumerable<string> GetVariableValues(string variableName) {
    return variableValues[variableName].Keys;
  }

  public int GetIndexForValue(string variableName, string variableValue) {
    return variableValues[variableName][variableValue];
  }
}
