using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic;

public class SymbolicDataAnalysisExpressionTreeInterpreter : ISymbolicDataAnalysisExpressionTreeInterpreter
{
  #region properties

  public bool CheckExpressionsWithIntervalArithmetic
  {
    get;
    set;
  }

  #endregion

  public IEnumerable<double> GetSymbolicExpressionTreeValues(SymbolicExpressionTree tree, Dataset dataset, IEnumerable<int> rows)
  {
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

  private static InterpreterState PrepareInterpreterState(SymbolicExpressionTree tree, Dataset dataset)
  {
    var code = SymbolicExpressionTreeCompiler.Compile(tree, OpCodes.MapSymbolToOpCode);
    var necessaryArgStackSize = 0;
    foreach (var instr in code) {
      switch (instr.OpCode) {
        case OpCodes.Variable: {
          var variableTreeNode = (VariableTreeNode)instr.DynamicNode;
          instr.Data = dataset.GetDoubleValues(variableTreeNode.VariableName);
          break;
        }
        case OpCodes.FactorVariable: {
          var factorTreeNode = (FactorVariableTreeNode)instr.DynamicNode;
          instr.Data = dataset.GetStringValues(factorTreeNode.VariableName);
          break;
        }
        case OpCodes.BinaryFactorVariable: {
          var factorTreeNode = (BinaryFactorVariableTreeNode)instr.DynamicNode;
          instr.Data = dataset.GetStringValues(factorTreeNode.VariableName);
          break;
        }
        case OpCodes.LagVariable: {
          var laggedVariableTreeNode = (LaggedVariableTreeNode)instr.DynamicNode;
          instr.Data = dataset.GetDoubleValues(laggedVariableTreeNode.VariableName);
          break;
        }
        case OpCodes.VariableCondition: {
          var variableConditionTreeNode = (VariableConditionTreeNode)instr.DynamicNode;
          instr.Data = dataset.GetDoubleValues(variableConditionTreeNode.VariableName);
          break;
        }
        case OpCodes.Call:
          necessaryArgStackSize += instr.NArguments + 1;
          break;
      }
    }

    return new InterpreterState(code, necessaryArgStackSize);
  }

  public virtual double Evaluate(Dataset dataset, ref int row, InterpreterState state)
  {
    var currentInstr = state.NextInstruction();
    switch (currentInstr.OpCode) {
      case OpCodes.Add: {
        var s = Evaluate(dataset, ref row, state);
        for (var i = 1; i < currentInstr.NArguments; i++) {
          s += Evaluate(dataset, ref row, state);
        }

        return s;
      }
      case OpCodes.Sub: {
        var s = Evaluate(dataset, ref row, state);
        for (var i = 1; i < currentInstr.NArguments; i++) {
          s -= Evaluate(dataset, ref row, state);
        }

        if (currentInstr.NArguments == 1) { s = -s; }

        return s;
      }
      case OpCodes.Mul: {
        var p = Evaluate(dataset, ref row, state);
        for (var i = 1; i < currentInstr.NArguments; i++) {
          p *= Evaluate(dataset, ref row, state);
        }

        return p;
      }
      case OpCodes.Div: {
        var p = Evaluate(dataset, ref row, state);
        for (var i = 1; i < currentInstr.NArguments; i++) {
          p /= Evaluate(dataset, ref row, state);
        }

        if (currentInstr.NArguments == 1) { p = 1.0 / p; }

        return p;
      }
      case OpCodes.Average: {
        var sum = Evaluate(dataset, ref row, state);
        for (var i = 1; i < currentInstr.NArguments; i++) {
          sum += Evaluate(dataset, ref row, state);
        }

        return sum / currentInstr.NArguments;
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
        return double.IsNaN(x) ? double.NaN : throw new NotImplementedException("Gamma function is currently not implemented");
        ;
      }
      case OpCodes.Psi: {
        var x = Evaluate(dataset, ref row, state);
        switch (x) {
          case double.NaN:
          case <= 0 when (Math.Floor(x) - x).IsAlmost(0):
            return double.NaN;
          default:
            throw new NotImplementedException("Psi function is currently not implemented");
            ;
        }
      }
      case OpCodes.Dawson: {
        var x = Evaluate(dataset, ref row, state);
        return double.IsNaN(x) ? double.NaN : throw new NotImplementedException("Dawson Integral is currently not implemented");
        ;
      }
      case OpCodes.ExponentialIntegralEi: {
        var x = Evaluate(dataset, ref row, state);
        return double.IsNaN(x) ? double.NaN : throw new NotImplementedException("Exponential Integral EI is currently not implemented");
        ;
      }
      case OpCodes.SineIntegral: {
        var x = Evaluate(dataset, ref row, state);
        if (double.IsNaN(x)) {
          return double.NaN;
        }

        throw new NotImplementedException("Sine integral is currently not implemented");
      }
      case OpCodes.CosineIntegral: {
        var x = Evaluate(dataset, ref row, state);
        if (double.IsNaN(x)) {
          return double.NaN;
        }

        throw new NotImplementedException("Cosine Integral is currently not implemented");
      }
      case OpCodes.HyperbolicSineIntegral: {
        var x = Evaluate(dataset, ref row, state);
        if (double.IsNaN(x)) {
          return double.NaN;
        }

        throw new NotImplementedException("Hyperbolic Sine Integral is currently not implemented");
      }
      case OpCodes.HyperbolicCosineIntegral: {
        var x = Evaluate(dataset, ref row, state);
        if (double.IsNaN(x)) {
          return double.NaN;
        }

        throw new NotImplementedException("Hyperbolic Cosine Integral is currently not implemented");
      }
      case OpCodes.FresnelCosineIntegral: {
        var x = Evaluate(dataset, ref row, state);
        if (double.IsNaN(x)) {
          return double.NaN;
        }

        throw new NotImplementedException("Fresnel Cosine Integral is currently not implemented");
      }
      case OpCodes.FresnelSineIntegral: {
        var x = Evaluate(dataset, ref row, state);
        if (double.IsNaN(x)) {
          return double.NaN;
        }

        throw new NotImplementedException("Fresnel Sine Integral is currently not implemented");
      }
      case OpCodes.AiryA: {
        var x = Evaluate(dataset, ref row, state);
        if (double.IsNaN(x)) {
          return double.NaN;
        }

        throw new NotImplementedException("AiryA is currently not implemented");
      }
      case OpCodes.AiryB: {
        var x = Evaluate(dataset, ref row, state);
        if (double.IsNaN(x)) {
          return double.NaN;
        }

        throw new NotImplementedException("AiryB is currently not implemented");
      }
      case OpCodes.Norm: {
        var x = Evaluate(dataset, ref row, state);
        return double.IsNaN(x) ? double.NaN : throw new NotImplementedException("Norm is currently not implemented");
        ;
      }
      case OpCodes.Erf: {
        var x = Evaluate(dataset, ref row, state);
        return double.IsNaN(x) ? double.NaN : throw new NotImplementedException("Error function is currently not implemented");
        ;
      }
      case OpCodes.Bessel: {
        var x = Evaluate(dataset, ref row, state);
        return double.IsNaN(x) ? double.NaN : throw new NotImplementedException("Bessel function is currently not implemented");
        ;
      }

      case OpCodes.AnalyticQuotient: {
        var x1 = Evaluate(dataset, ref row, state);
        var x2 = Evaluate(dataset, ref row, state);
        return x1 / Math.Pow(1 + (x2 * x2), 0.5);
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
      case OpCodes.And: {
        var result = Evaluate(dataset, ref row, state);
        for (var i = 1; i < currentInstr.NArguments; i++) {
          if (result > 0.0) {
            result = Evaluate(dataset, ref row, state);
          } else {
            state.SkipInstructions();
          }
        }

        return result > 0.0 ? 1.0 : -1.0;
      }
      case OpCodes.Or: {
        var result = Evaluate(dataset, ref row, state);
        for (var i = 1; i < currentInstr.NArguments; i++) {
          if (result <= 0.0) {
            result = Evaluate(dataset, ref row, state);
          } else {
            state.SkipInstructions();
          }
        }

        return result > 0.0 ? 1.0 : -1.0;
      }
      case OpCodes.Not: {
        return Evaluate(dataset, ref row, state) > 0.0 ? -1.0 : 1.0;
      }
      case OpCodes.Xor: {
        //mkommend: XOR on multiple inputs is defined as true if the number of positive signals is odd
        // this is equal to a consecutive execution of binary XOR operations.
        var positiveSignals = 0;
        for (var i = 0; i < currentInstr.NArguments; i++) {
          if (Evaluate(dataset, ref row, state) > 0.0) { positiveSignals++; }
        }

        return positiveSignals % 2 != 0 ? 1.0 : -1.0;
      }
      case OpCodes.Gt: {
        var x = Evaluate(dataset, ref row, state);
        var y = Evaluate(dataset, ref row, state);
        if (x > y) { return 1.0; }

        return -1.0;
      }
      case OpCodes.Lt: {
        var x = Evaluate(dataset, ref row, state);
        var y = Evaluate(dataset, ref row, state);
        if (x < y) { return 1.0; }

        return -1.0;
      }
      case OpCodes.TimeLag: {
        var timeLagTreeNode = (LaggedTreeNode)currentInstr.DynamicNode;
        row += timeLagTreeNode.Lag;
        var result = Evaluate(dataset, ref row, state);
        row -= timeLagTreeNode.Lag;
        return result;
      }
      case OpCodes.Integral: {
        var savedPc = state.ProgramCounter;
        var timeLagTreeNode = (LaggedTreeNode)currentInstr.DynamicNode;
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

        return (f0 + (2 * f1) - (2 * f3) - f4) / 8; // h = 1
      }
      case OpCodes.Call: {
        // evaluate sub-trees
        var argValues = new double[currentInstr.NArguments];
        for (var i = 0; i < currentInstr.NArguments; i++) {
          argValues[i] = Evaluate(dataset, ref row, state);
        }

        // push on argument values on stack 
        state.CreateStackFrame(argValues);

        // save the pc
        var savedPc = state.ProgramCounter;
        // set pc to start of function  
        state.ProgramCounter = (ushort)currentInstr.Data;
        // evaluate the function
        var v = Evaluate(dataset, ref row, state);

        // delete the stack frame
        state.RemoveStackFrame();

        // restore the pc => evaluation will continue at point after my subtrees  
        state.ProgramCounter = savedPc;
        return v;
      }
      case OpCodes.Arg: {
        return state.GetStackFrameValue((ushort)currentInstr.Data);
      }
      case OpCodes.Variable: {
        if (row < 0 || row >= dataset.Rows) {
          return double.NaN;
        }

        var variableTreeNode = (VariableTreeNode)currentInstr.DynamicNode;
        return ((IList<double>)currentInstr.Data)[row] * variableTreeNode.Weight;
      }
      case OpCodes.BinaryFactorVariable: {
        if (row < 0 || row >= dataset.Rows) {
          return double.NaN;
        }

        var factorVarTreeNode = (BinaryFactorVariableTreeNode)currentInstr.DynamicNode;
        return ((IList<string>)currentInstr.Data)[row] == factorVarTreeNode.VariableValue ? factorVarTreeNode.Weight : 0;
      }
      case OpCodes.FactorVariable: {
        if (row < 0 || row >= dataset.Rows) {
          return double.NaN;
        }

        var factorVarTreeNode = (FactorVariableTreeNode)currentInstr.DynamicNode;
        return factorVarTreeNode.GetValue(((IList<string>)currentInstr.Data)[row]);
      }
      case OpCodes.LagVariable: {
        var laggedVariableTreeNode = (LaggedVariableTreeNode)currentInstr.DynamicNode;
        var actualRow = row + laggedVariableTreeNode.Lag;
        if (actualRow < 0 || actualRow >= dataset.Rows) { return double.NaN; }

        return ((IList<double>)currentInstr.Data)[actualRow] * laggedVariableTreeNode.Weight;
      }
      case OpCodes.Constant: // fall through
      case OpCodes.Number: {
        var numericTreeNode = (NumberTreeNode)currentInstr.DynamicNode;
        return numericTreeNode.Value;
      }

      //mkommend: this symbol uses the logistic function f(x) = 1 / (1 + e^(-alpha * x) ) 
      //to determine the relative amounts of the true and false branch see http://en.wikipedia.org/wiki/Logistic_function
      case OpCodes.VariableCondition: {
        if (row < 0 || row >= dataset.Rows) {
          return double.NaN;
        }

        var variableConditionTreeNode = (VariableConditionTreeNode)currentInstr.DynamicNode;
        if (!variableConditionTreeNode.Symbol.IgnoreSlope) {
          var variableValue = ((IList<double>)currentInstr.Data)[row];
          var x = variableValue - variableConditionTreeNode.Threshold;
          var p = 1 / (1 + Math.Exp(-variableConditionTreeNode.Slope * x));

          var trueBranch = Evaluate(dataset, ref row, state);
          var falseBranch = Evaluate(dataset, ref row, state);

          return (trueBranch * p) + (falseBranch * (1 - p));
        } else {
          // strict threshold
          var variableValue = ((IList<double>)currentInstr.Data)[row];
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
