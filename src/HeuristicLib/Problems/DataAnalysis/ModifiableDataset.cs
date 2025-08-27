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

using System.Collections;

#pragma warning disable S2368

namespace HEAL.HeuristicLib.Problems.DataAnalysis;

public class ModifiableDataset : Dataset {
  public ModifiableDataset() { }

  public ModifiableDataset(IEnumerable<string> variableNames, IEnumerable<IList> variableValues, bool cloneValues) :
    base(variableNames, variableValues, cloneValues) { }

  public ModifiableDataset(IEnumerable<string> variableNames, IEnumerable<IList> variableValues) :
    base(variableNames, variableValues) { }

  public ModifiableDataset(IEnumerable<string> variableNames, double[,] variableValues) :
    base(variableNames, variableValues) { }

  public IEnumerable<object> GetRow(int row) {
    if (row < 0 || row >= Rows)
      throw new ArgumentException($"Invalid row {row} specified. The dataset contains {Rows} row(s).");
    return VariableValues.Select(x => x.Value[row]!);
  }

  public void AddRow(IEnumerable<object> values) {
    var list = values.ToList();
    if (list.Count != VariableNames.Count)
      throw new ArgumentException("The number of values must be equal to the number of variable names.");
    // check if all the values are of the correct type
    for (var i = 0; i < list.Count; ++i) {
      if (list[i].GetType() != GetVariableType(VariableNames[i])) {
        throw new ArgumentException("The type of the provided value does not match the variable type.");
      }
    }

    // add values
    for (var i = 0; i < list.Count; ++i) {
      VariableValues[VariableNames[i]].Add(list[i]);
    }

    Rows++;
  }

  public void ReplaceRow(int row, IEnumerable<object> values) {
    var list = values.ToList();
    if (list.Count != VariableNames.Count)
      throw new ArgumentException("The number of values must be equal to the number of variable names.");
    // check if all the values are of the correct type
    for (var i = 0; i < list.Count; ++i) {
      if (list[i].GetType() != GetVariableType(VariableNames[i])) {
        throw new ArgumentException("The type of the provided value does not match the variable type.");
      }
    }

    // replace values
    for (var i = 0; i < list.Count; ++i) {
      VariableValues[VariableNames[i]][row] = list[i];
    }
  }

  // slow, avoid using this
  public void RemoveRow(int row) {
    foreach (var list in VariableValues.Values)
      list.RemoveAt(row);
    Rows--;
  }

  // adds a new variable to the dataset
  public void AddVariable(string variableName, IList values) {
    InsertVariable(variableName, Columns, values);
  }

  public void InsertVariable(string variableName, int position, IList values) {
    if (VariableValues.ContainsKey(variableName))
      throw new ArgumentException($"Variable {variableName} is already present in the dataset.");

    if (position < 0 || position > Columns)
      throw new ArgumentException($"Incorrect position {position} specified. The position must be between 0 and {Columns}.");

    if (values == null)
      throw new ArgumentNullException(nameof(values), "Values must not be null. At least an empty list of values has to be provided.");

    if (values.Count != Rows)
      throw new ArgumentException($"{values.Count} values are provided, but {Rows} rows are present in the dataset.");

    if (!IsAllowedType(values))
      throw new ArgumentException($"Unsupported type {GetElementType(values)} for variable {variableName}.");

    VariableNames.Insert(position, variableName);
    VariableValues[variableName] = values;
  }

  public void ReplaceVariable(string variableName, IList values) {
    if (!VariableValues.TryGetValue(variableName, out var value))
      throw new ArgumentException($"Variable {variableName} is not present in the dataset.");
    if (values.Count != value.Count)
      throw new ArgumentException("The number of values must coincide with the number of dataset rows.");
    if (GetVariableType(variableName) != values[0]?.GetType())
      throw new ArgumentException("The type of the provided value does not match the variable type.");
    VariableValues[variableName] = values;
  }

  public void RemoveVariable(string variableName) {
    if (!VariableValues.ContainsKey(variableName))
      throw new ArgumentException($"The variable {variableName} does not exist in the dataset.");
    VariableValues.Remove(variableName);
    VariableNames.Remove(variableName);
  }

  public void ClearValues() {
    foreach (var list in VariableValues.Values) {
      list.Clear();
    }

    Rows = 0;
  }

  public void SetVariableValue(object value, string variableName, int row) {
    VariableValues.TryGetValue(variableName, out var list);
    if (list == null)
      throw new ArgumentException("The variable " + variableName + " does not exist in the dataset.");
    if (row < 0 || list.Count < row)
      throw new ArgumentOutOfRangeException(nameof(row), "Invalid row value");
    if (GetVariableType(variableName) != value.GetType())
      throw new ArgumentException("The type of the provided value does not match the variable type.");

    list[row] = value;
  }
}
