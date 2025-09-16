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

using System.Runtime.CompilerServices;
using HEAL.HeuristicLib.Encodings.SymbolicExpression;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using static HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic.BatchOperations;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic {
  public class SymbolicDataAnalysisExpressionTreeBatchInterpreter : ISymbolicDataAnalysisExpressionTreeInterpreter {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LoadData(in BatchInstruction instr, ReadOnlySpan<int> rows, int startIndex, int batchSize) {
      var data = instr.Data;
      var dst = instr.Buf;
      var w = instr.Weight;
      for (int i = 0; i < batchSize; ++i) {
        var row = rows[startIndex + i]; // ✅ use the i-th selected row
        dst[i] = w * data[row];
      }
    }

    private static void Evaluate(BatchInstruction[] code, ReadOnlySpan<int> rows, int rowIndex, int batchSize) {
      for (var i = code.Length - 1; i >= 0; --i) {
        ref readonly var instr = ref code[i];
        var c = instr.ChildIndex;
        var n = instr.NumberOfArguments;

        ref readonly var childInstruction = ref code[c];

        switch (instr.Opcode) {
          case OpCodes.Variable: {
            LoadData(instr, rows, rowIndex, batchSize);
            break;
          }
          case OpCodes.Constant: // fall through
          case OpCodes.Number:
            break; // nothing to do here, don't remove because we want to prevent falling into the default case here.
          case OpCodes.Add: {
            Load(instr.Buf, childInstruction.Buf);
            for (var j = 1; j < n; ++j) {
              ref readonly var ch = ref code[c + j];
              Add(instr.Buf, ch.Buf);
            }

            break;
          }

          case OpCodes.Sub: {
            if (n == 1) {
              Neg(instr.Buf, childInstruction.Buf);
            } else {
              Load(instr.Buf, childInstruction.Buf);
              for (var j = 1; j < n; ++j) {
                ref readonly var ch = ref code[c + j];
                Sub(instr.Buf, ch.Buf);
              }
            }

            break;
          }

          case OpCodes.Mul: {
            Load(instr.Buf, childInstruction.Buf);
            for (var j = 1; j < n; ++j) {
              ref readonly var ch = ref code[c + j];
              Mul(instr.Buf, ch.Buf);
            }

            break;
          }

          case OpCodes.Div: {
            if (n == 1) {
              Inv(instr.Buf, childInstruction.Buf);
            } else {
              Load(instr.Buf, childInstruction.Buf);
              for (var j = 1; j < n; ++j) {
                ref readonly var ch = ref code[c + j];
                Div(instr.Buf, ch.Buf);
              }
            }

            break;
          }

          case OpCodes.Square: {
            Square(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.Root: {
            Load(instr.Buf, childInstruction.Buf);
            ref readonly var ch = ref code[c + 1];
            Root(instr.Buf, ch.Buf);
            break;
          }

          case OpCodes.SquareRoot: {
            Sqrt(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.Cube: {
            Cube(instr.Buf, childInstruction.Buf);
            break;
          }
          case OpCodes.CubeRoot: {
            CubeRoot(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.Power: {
            Load(instr.Buf, childInstruction.Buf);
            ref readonly var ch = ref code[c + 1];
            Pow(instr.Buf, ch.Buf);
            break;
          }

          case OpCodes.Exp: {
            Exp(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.Log: {
            Log(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.Sin: {
            Sin(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.Cos: {
            Cos(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.Tan: {
            Tan(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.Tanh: {
            Tanh(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.Absolute: {
            Absolute(instr.Buf, childInstruction.Buf);
            break;
          }

          case OpCodes.AnalyticQuotient: {
            Load(instr.Buf, childInstruction.Buf);
            ref readonly var ch = ref code[c + 1];
            AnalyticQuotient(instr.Buf, ch.Buf);
            break;
          }

          case OpCodes.SubFunction: {
            Load(instr.Buf, childInstruction.Buf);
            break;
          }
          default:
            throw new NotSupportedException($"This interpreter does not support {(OpCode)instr.Opcode}");
        }
      }
    }

    [ThreadStatic] private static Dictionary<string, double[]>? cachedData;

    [ThreadStatic] private static Dataset? cachedDataset;

    private static void InitCache(Dataset dataset) {
      cachedDataset = dataset;
      cachedData = new();
      foreach (var v in dataset.DoubleVariables) {
        cachedData[v] = dataset.GetDoubleValues(v).ToArray();
      }
    }

    private static double[] GetValues(SymbolicExpressionTree tree, Dataset dataset, int[] rows) {
      if (cachedData == null || cachedDataset != dataset || cachedDataset is ModifiableDataset) {
        InitCache(dataset);
      }

      var code = Compile(tree, dataset, OpCodes.MapSymbolToOpCode);
      var remainingRows = rows.Length % BatchSize;
      var roundedTotal = rows.Length - remainingRows;

      var result = new double[rows.Length];

      for (var rowIndex = 0; rowIndex < roundedTotal; rowIndex += BatchSize) {
        Evaluate(code, rows, rowIndex, BatchSize);
        Array.Copy(code[0].Buf, 0, result, rowIndex, BatchSize);
      }

      if (remainingRows <= 0)
        return result;

      Evaluate(code, rows, roundedTotal, remainingRows);
      Array.Copy(code[0].Buf, 0, result, roundedTotal, remainingRows);

      return result;
    }

    public IEnumerable<double> GetSymbolicExpressionTreeValues(SymbolicExpressionTree tree, Dataset dataset, IEnumerable<int> rows) {
      if (rows is not int[] rowarr)
        rowarr = rows.ToArray();
      return GetValues(tree, dataset, rowarr);
    }

    private static BatchInstruction[] Compile(SymbolicExpressionTree tree, Dataset dataset, Func<SymbolicExpressionTreeNode, byte> opCodeMapper) {
      var root = tree.Root.GetSubtree(0).GetSubtree(0);
      var code = new BatchInstruction[root.GetLength()];
      if (root.SubtreeCount > ushort.MaxValue)
        throw new ArgumentException("Number of subtrees is too big (>65.535)");
      int c = 1, i = 0;
      foreach (var node in root.IterateNodesBreadth()) {
        if (node.SubtreeCount > ushort.MaxValue)
          throw new ArgumentException("Number of subtrees is too big (>65.535)");
        double w = 0, v = 0;
        double[] d = [];
        switch (node) {
          case VariableTreeNode variable: {
            w = variable.Weight;
            if (cachedData!.TryGetValue(variable.VariableName, out var value)) {
              d = value;
            } else {
              d = dataset.GetDoubleValues(variable.VariableName).ToArray();
              cachedData[variable.VariableName] = d;
            }

            break;
          }
          case NumberTreeNode numeric: {
            v = numeric.Value;
            Array.Fill(code[i].Buf, code[i].Value, 0, BatchSize);
            break;
          }
        }

        code[i] = new(opcode: opCodeMapper(node), numberOfArguments: (ushort)node.SubtreeCount, buf: new double[BatchSize], childIndex: c, weight: w, data: d, value: v);

        c += node.SubtreeCount;
        ++i;
      }

      return code;
    }
  }
}
