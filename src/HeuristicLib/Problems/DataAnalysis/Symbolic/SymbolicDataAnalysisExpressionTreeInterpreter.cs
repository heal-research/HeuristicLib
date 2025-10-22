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
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic {
  public class SymbolicDataAnalysisExpressionTreeInterpreter : ISymbolicDataAnalysisExpressionTreeInterpreter {
    #region properties
    public bool CheckExpressionsWithIntervalArithmetic {
      get;
      set;
    }
    #endregion

    public IEnumerable<double> GetSymbolicExpressionTreeValues(SymbolicExpressionTree tree, Dataset dataset, IEnumerable<int> rows) {
      if (CheckExpressionsWithIntervalArithmetic) {
        throw new NotSupportedException("Interval arithmetic is not yet supported in the symbolic data analysis interpreter.");
      }

      var state = PrepareInterpreterState(tree, dataset);

      foreach (var rowEnum in rows) {
        var row = rowEnum;
        yield return Evaluate(dataset, ref row, state);
        state.Reset();
      }
    }

    private static InterpreterState PrepareInterpreterState(SymbolicExpressionTree tree, Dataset dataset) {
      var code = SymbolicExpressionTreeCompiler.Compile(tree, OpCodes.MapSymbolToOpCode);
      var necessaryArgStackSize = 0;
      foreach (var instr in code) {
        switch (instr.opCode) {
          case OpCodes.Variable: {
            var variableTreeNode = (VariableTreeNode)instr.dynamicNode;
            instr.data = dataset.GetDoubleValues(variableTreeNode.VariableName);
            break;
          }
          case OpCodes.FactorVariable: {
            var factorTreeNode = (FactorVariableTreeNode)instr.dynamicNode;
            instr.data = dataset.GetStringValues(factorTreeNode.VariableName);
            break;
          }
          case OpCodes.BinaryFactorVariable: {
            var factorTreeNode = (BinaryFactorVariableTreeNode)instr.dynamicNode;
            instr.data = dataset.GetStringValues(factorTreeNode.VariableName);
            break;
          }
          case OpCodes.LagVariable: {
            var laggedVariableTreeNode = (LaggedVariableTreeNode)instr.dynamicNode;
            instr.data = dataset.GetDoubleValues(laggedVariableTreeNode.VariableName);
            break;
          }
          case OpCodes.VariableCondition: {
            var variableConditionTreeNode = (VariableConditionTreeNode)instr.dynamicNode;
            instr.data = dataset.GetDoubleValues(variableConditionTreeNode.VariableName);
            break;
          }
          case OpCodes.Call:
            necessaryArgStackSize += instr.nArguments + 1;
            break;
        }
      }

      return new InterpreterState(code, necessaryArgStackSize);
    }

    public virtual double Evaluate(Dataset dataset, ref int row, InterpreterState state) {
      var currentInstr = state.NextInstruction();
      switch (currentInstr.opCode) {
        case OpCodes.Add: {
          var s = Evaluate(dataset, ref row, state);
          for (var i = 1; i < currentInstr.nArguments; i++) {
            s += Evaluate(dataset, ref row, state);
          }

          return s;
        }
        case OpCodes.Sub: {
          var s = Evaluate(dataset, ref row, state);
          for (var i = 1; i < currentInstr.nArguments; i++) {
            s -= Evaluate(dataset, ref row, state);
          }

          if (currentInstr.nArguments == 1) { s = -s; }

          return s;
        }
        case OpCodes.Mul: {
          var p = Evaluate(dataset, ref row, state);
          for (var i = 1; i < currentInstr.nArguments; i++) {
            p *= Evaluate(dataset, ref row, state);
          }

          return p;
        }
        case OpCodes.Div: {
          var p = Evaluate(dataset, ref row, state);
          for (var i = 1; i < currentInstr.nArguments; i++) {
            p /= Evaluate(dataset, ref row, state);
          }

          if (currentInstr.nArguments == 1) { p = 1.0 / p; }

          return p;
        }
        case OpCodes.Average: {
          var sum = Evaluate(dataset, ref row, state);
          for (var i = 1; i < currentInstr.nArguments; i++) {
            sum += Evaluate(dataset, ref row, state);
          }

          return sum / currentInstr.nArguments;
        }
        case OpCodes.Absolute: {
          return Math.Abs(Evaluate(dataset, ref row, state));
        }
        case OpCodes.Tanh: {
          return Math.Tanh(Evaluate(dataset, ref row, state));
        }
        case OpCodes.Cos: {
          return Math.Cos(Evaluate(dataset, ref row, state));
        }
        case OpCodes.Sin: {
          return Math.Sin(Evaluate(dataset, ref row, state));
        }
        case OpCodes.Tan: {
          return Math.Tan(Evaluate(dataset, ref row, state));
        }
        case OpCodes.Square: {
          return Math.Pow(Evaluate(dataset, ref row, state), 2);
        }
        case OpCodes.Cube: {
          return Math.Pow(Evaluate(dataset, ref row, state), 3);
        }
        case OpCodes.Power: {
          var x = Evaluate(dataset, ref row, state);
          var y = Math.Round(Evaluate(dataset, ref row, state));
          return Math.Pow(x, y);
        }
        case OpCodes.SquareRoot: {
          return Math.Sqrt(Evaluate(dataset, ref row, state));
        }
        case OpCodes.CubeRoot: {
          var arg = Evaluate(dataset, ref row, state);
          return arg < 0 ? -Math.Pow(-arg, 1.0 / 3.0) : Math.Pow(arg, 1.0 / 3.0);
        }
        case OpCodes.Root: {
          var x = Evaluate(dataset, ref row, state);
          var y = Math.Round(Evaluate(dataset, ref row, state));
          return Math.Pow(x, 1 / y);
        }
        case OpCodes.Exp: {
          return Math.Exp(Evaluate(dataset, ref row, state));
        }
        case OpCodes.Log: {
          return Math.Log(Evaluate(dataset, ref row, state));
        }
        case OpCodes.Gamma: {
          var x = Evaluate(dataset, ref row, state);
          return double.IsNaN(x) ? double.NaN : alglib.gammafunction(x);
        }
        case OpCodes.Psi: {
          var x = Evaluate(dataset, ref row, state);
          switch (x) {
            case Double.NaN:
            case <= 0 when (Math.Floor(x) - x).IsAlmost(0):
              return double.NaN;
            default:
              return alglib.psi(x);
          }
        }
        case OpCodes.Dawson: {
          var x = Evaluate(dataset, ref row, state);
          return double.IsNaN(x) ? double.NaN : alglib.dawsonintegral(x);
        }
        case OpCodes.ExponentialIntegralEi: {
          var x = Evaluate(dataset, ref row, state);
          return double.IsNaN(x) ? double.NaN : alglib.exponentialintegralei(x);
        }
        case OpCodes.SineIntegral: {
          var x = Evaluate(dataset, ref row, state);
          if (double.IsNaN(x)) return double.NaN;
          alglib.sinecosineintegrals(x, out var si, out _);
          return si;
        }
        case OpCodes.CosineIntegral: {
          var x = Evaluate(dataset, ref row, state);
          if (double.IsNaN(x)) return double.NaN;
          alglib.sinecosineintegrals(x, out _, out var ci);
          return ci;
        }
        case OpCodes.HyperbolicSineIntegral: {
          var x = Evaluate(dataset, ref row, state);
          if (double.IsNaN(x)) return double.NaN;
          alglib.hyperbolicsinecosineintegrals(x, out var shi, out _);
          return shi;
        }
        case OpCodes.HyperbolicCosineIntegral: {
          var x = Evaluate(dataset, ref row, state);
          if (double.IsNaN(x)) return double.NaN;
          alglib.hyperbolicsinecosineintegrals(x, out _, out var chi);
          return chi;
        }
        case OpCodes.FresnelCosineIntegral: {
          double c = 0, s = 0;
          var x = Evaluate(dataset, ref row, state);
          if (double.IsNaN(x)) return double.NaN;
          alglib.fresnelintegral(x, ref c, ref s);
          return c;
        }
        case OpCodes.FresnelSineIntegral: {
          double c = 0, s = 0;
          var x = Evaluate(dataset, ref row, state);
          if (double.IsNaN(x)) return double.NaN;
          alglib.fresnelintegral(x, ref c, ref s);
          return s;
        }
        case OpCodes.AiryA: {
          var x = Evaluate(dataset, ref row, state);
          if (double.IsNaN(x)) return double.NaN;
          alglib.airy(x, out var ai, out _, out _, out _);
          return ai;
        }
        case OpCodes.AiryB: {
          var x = Evaluate(dataset, ref row, state);
          if (double.IsNaN(x)) return double.NaN;
          alglib.airy(x, out _, out _, out var bi, out _);
          return bi;
        }
        case OpCodes.Norm: {
          var x = Evaluate(dataset, ref row, state);
          return double.IsNaN(x) ? double.NaN : alglib.normaldistribution(x);
        }
        case OpCodes.Erf: {
          var x = Evaluate(dataset, ref row, state);
          return double.IsNaN(x) ? double.NaN : alglib.errorfunction(x);
        }
        case OpCodes.Bessel: {
          var x = Evaluate(dataset, ref row, state);
          return double.IsNaN(x) ? double.NaN : alglib.besseli0(x);
        }

        case OpCodes.AnalyticQuotient: {
          var x1 = Evaluate(dataset, ref row, state);
          var x2 = Evaluate(dataset, ref row, state);
          return x1 / Math.Pow(1 + x2 * x2, 0.5);
        }
        case OpCodes.IfThenElse: {
          var condition = Evaluate(dataset, ref row, state);
          double result;
          if (condition > 0.0) {
            result = Evaluate(dataset, ref row, state);
            state.SkipInstructions();
          } else {
            state.SkipInstructions();
            result = Evaluate(dataset, ref row, state);
          }

          return result;
        }
        case OpCodes.AND: {
          var result = Evaluate(dataset, ref row, state);
          for (var i = 1; i < currentInstr.nArguments; i++) {
            if (result > 0.0) result = Evaluate(dataset, ref row, state);
            else {
              state.SkipInstructions();
            }
          }

          return result > 0.0 ? 1.0 : -1.0;
        }
        case OpCodes.OR: {
          var result = Evaluate(dataset, ref row, state);
          for (var i = 1; i < currentInstr.nArguments; i++) {
            if (result <= 0.0) result = Evaluate(dataset, ref row, state);
            else {
              state.SkipInstructions();
            }
          }

          return result > 0.0 ? 1.0 : -1.0;
        }
        case OpCodes.NOT: {
          return Evaluate(dataset, ref row, state) > 0.0 ? -1.0 : 1.0;
        }
        case OpCodes.XOR: {
          //mkommend: XOR on multiple inputs is defined as true if the number of positive signals is odd
          // this is equal to a consecutive execution of binary XOR operations.
          var positiveSignals = 0;
          for (var i = 0; i < currentInstr.nArguments; i++) {
            if (Evaluate(dataset, ref row, state) > 0.0) { positiveSignals++; }
          }

          return positiveSignals % 2 != 0 ? 1.0 : -1.0;
        }
        case OpCodes.GT: {
          var x = Evaluate(dataset, ref row, state);
          var y = Evaluate(dataset, ref row, state);
          if (x > y) { return 1.0; }

          return -1.0;
        }
        case OpCodes.LT: {
          var x = Evaluate(dataset, ref row, state);
          var y = Evaluate(dataset, ref row, state);
          if (x < y) { return 1.0; }

          return -1.0;
        }
        case OpCodes.TimeLag: {
          var timeLagTreeNode = (LaggedTreeNode)currentInstr.dynamicNode;
          row += timeLagTreeNode.Lag;
          var result = Evaluate(dataset, ref row, state);
          row -= timeLagTreeNode.Lag;
          return result;
        }
        case OpCodes.Integral: {
          var savedPc = state.ProgramCounter;
          var timeLagTreeNode = (LaggedTreeNode)currentInstr.dynamicNode;
          var sum = 0.0;
          for (var i = 0; i < Math.Abs(timeLagTreeNode.Lag); i++) {
            row += Math.Sign(timeLagTreeNode.Lag);
            sum += Evaluate(dataset, ref row, state);
            state.ProgramCounter = savedPc;
          }

          row -= timeLagTreeNode.Lag;
          sum += Evaluate(dataset, ref row, state);
          return sum;
        }

        //mkommend: derivate calculation taken from: 
        //http://www.holoborodko.com/pavel/numerical-methods/numerical-derivative/smooth-low-noise-differentiators/
        //one sided smooth differentiatior, N = 4
        // y' = 1/8h (f_i + 2f_i-1, -2 f_i-3 - f_i-4)
        case OpCodes.Derivative: {
          var savedPc = state.ProgramCounter;
          var f0 = Evaluate(dataset, ref row, state);
          row--;
          state.ProgramCounter = savedPc;
          var f1 = Evaluate(dataset, ref row, state);
          row -= 2;
          state.ProgramCounter = savedPc;
          var f3 = Evaluate(dataset, ref row, state);
          row--;
          state.ProgramCounter = savedPc;
          var f4 = Evaluate(dataset, ref row, state);
          row += 4;

          return (f0 + 2 * f1 - 2 * f3 - f4) / 8; // h = 1
        }
        case OpCodes.Call: {
          // evaluate sub-trees
          var argValues = new double[currentInstr.nArguments];
          for (var i = 0; i < currentInstr.nArguments; i++) {
            argValues[i] = Evaluate(dataset, ref row, state);
          }

          // push on argument values on stack 
          state.CreateStackFrame(argValues);

          // save the pc
          var savedPc = state.ProgramCounter;
          // set pc to start of function  
          state.ProgramCounter = (ushort)currentInstr.data;
          // evaluate the function
          var v = Evaluate(dataset, ref row, state);

          // delete the stack frame
          state.RemoveStackFrame();

          // restore the pc => evaluation will continue at point after my subtrees  
          state.ProgramCounter = savedPc;
          return v;
        }
        case OpCodes.Arg: {
          return state.GetStackFrameValue((ushort)currentInstr.data);
        }
        case OpCodes.Variable: {
          if (row < 0 || row >= dataset.Rows) return double.NaN;
          var variableTreeNode = (VariableTreeNode)currentInstr.dynamicNode;
          return ((IList<double>)currentInstr.data)[row] * variableTreeNode.Weight;
        }
        case OpCodes.BinaryFactorVariable: {
          if (row < 0 || row >= dataset.Rows) return double.NaN;
          var factorVarTreeNode = (BinaryFactorVariableTreeNode)currentInstr.dynamicNode;
          return ((IList<string>)currentInstr.data)[row] == factorVarTreeNode.VariableValue ? factorVarTreeNode.Weight : 0;
        }
        case OpCodes.FactorVariable: {
          if (row < 0 || row >= dataset.Rows) return double.NaN;
          var factorVarTreeNode = (FactorVariableTreeNode)currentInstr.dynamicNode;
          return factorVarTreeNode.GetValue(((IList<string>)currentInstr.data)[row]);
        }
        case OpCodes.LagVariable: {
          var laggedVariableTreeNode = (LaggedVariableTreeNode)currentInstr.dynamicNode;
          var actualRow = row + laggedVariableTreeNode.Lag;
          if (actualRow < 0 || actualRow >= dataset.Rows) { return double.NaN; }

          return ((IList<double>)currentInstr.data)[actualRow] * laggedVariableTreeNode.Weight;
        }
        case OpCodes.Constant: // fall through
        case OpCodes.Number: {
          var numericTreeNode = (NumberTreeNode)currentInstr.dynamicNode;
          return numericTreeNode.Value;
        }

        //mkommend: this symbol uses the logistic function f(x) = 1 / (1 + e^(-alpha * x) ) 
        //to determine the relative amounts of the true and false branch see http://en.wikipedia.org/wiki/Logistic_function
        case OpCodes.VariableCondition: {
          if (row < 0 || row >= dataset.Rows) return double.NaN;
          var variableConditionTreeNode = (VariableConditionTreeNode)currentInstr.dynamicNode;
          if (!variableConditionTreeNode.Symbol.IgnoreSlope) {
            var variableValue = ((IList<double>)currentInstr.data)[row];
            var x = variableValue - variableConditionTreeNode.Threshold;
            var p = 1 / (1 + Math.Exp(-variableConditionTreeNode.Slope * x));

            var trueBranch = Evaluate(dataset, ref row, state);
            var falseBranch = Evaluate(dataset, ref row, state);

            return trueBranch * p + falseBranch * (1 - p);
          } else {
            // strict threshold
            var variableValue = ((IList<double>)currentInstr.data)[row];
            if (variableValue <= variableConditionTreeNode.Threshold) {
              var left = Evaluate(dataset, ref row, state);
              state.SkipInstructions();
              return left;
            }

            state.SkipInstructions();
            return Evaluate(dataset, ref row, state);
          }
        }
        case OpCodes.SubFunction: {
          return Evaluate(dataset, ref row, state);
        }
        default:
          throw new NotSupportedException();
      }
    }
  }
}
