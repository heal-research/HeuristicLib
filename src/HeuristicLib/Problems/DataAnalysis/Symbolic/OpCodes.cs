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
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;
using HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math.Variables;

namespace HEAL.HeuristicLib.Problems.DataAnalysis.Symbolic {
  public enum OpCode : byte {
    Add = 1,
    Sub = 2,
    Mul = 3,
    Div = 4,
    Sin = 5,
    Cos = 6,
    Tan = 7,
    Log = 8,
    Exp = 9,
    IfThenElse = 10,
    GT = 11,
    LT = 12,
    AND = 13,
    OR = 14,
    NOT = 15,
    Average = 16,
    Call = 17,
    Variable = 18,
    LagVariable = 19,
    Number = 20,
    Arg = 21,
    Power = 22,
    Root = 23,
    TimeLag = 24,
    Integral = 25,
    Derivative = 26,
    VariableCondition = 27,
    Square = 28,
    SquareRoot = 29,
    Gamma = 30,
    Psi = 31,
    Dawson = 32,
    ExponentialIntegralEi = 33,
    CosineIntegral = 34,
    SineIntegral = 35,
    HyperbolicCosineIntegral = 36,
    HyperbolicSineIntegral = 37,
    FresnelCosineIntegral = 38,
    FresnelSineIntegral = 39,
    AiryA = 40,
    AiryB = 41,
    Norm = 42,
    Erf = 43,
    Bessel = 44,
    XOR = 45,
    FactorVariable = 46,
    BinaryFactorVariable = 47,
    Absolute = 48,
    AnalyticQuotient = 49,
    Cube = 50,
    CubeRoot = 51,
    Tanh = 52,
    SubFunction = 53,
    Constant = 54
  };

  public static class OpCodes {
    // constants for API compatibility only
    public const byte Add = (byte)OpCode.Add;
    public const byte Sub = (byte)OpCode.Sub;
    public const byte Mul = (byte)OpCode.Mul;
    public const byte Div = (byte)OpCode.Div;
    public const byte Sin = (byte)OpCode.Sin;
    public const byte Cos = (byte)OpCode.Cos;
    public const byte Tan = (byte)OpCode.Tan;
    public const byte Log = (byte)OpCode.Log;
    public const byte Exp = (byte)OpCode.Exp;
    public const byte IfThenElse = (byte)OpCode.IfThenElse;
    public const byte GT = (byte)OpCode.GT;
    public const byte LT = (byte)OpCode.LT;
    public const byte AND = (byte)OpCode.AND;
    public const byte OR = (byte)OpCode.OR;
    public const byte NOT = (byte)OpCode.NOT;
    public const byte Average = (byte)OpCode.Average;
    public const byte Call = (byte)OpCode.Call;
    public const byte Variable = (byte)OpCode.Variable;
    public const byte LagVariable = (byte)OpCode.LagVariable;
    public const byte Number = (byte)OpCode.Number;
    public const byte Constant = (byte)OpCode.Constant;
    public const byte Arg = (byte)OpCode.Arg;
    public const byte Power = (byte)OpCode.Power;
    public const byte Root = (byte)OpCode.Root;
    public const byte TimeLag = (byte)OpCode.TimeLag;
    public const byte Integral = (byte)OpCode.Integral;
    public const byte Derivative = (byte)OpCode.Derivative;
    public const byte VariableCondition = (byte)OpCode.VariableCondition;
    public const byte Square = (byte)OpCode.Square;
    public const byte SquareRoot = (byte)OpCode.SquareRoot;
    public const byte Gamma = (byte)OpCode.Gamma;
    public const byte Psi = (byte)OpCode.Psi;
    public const byte Dawson = (byte)OpCode.Dawson;
    public const byte ExponentialIntegralEi = (byte)OpCode.ExponentialIntegralEi;
    public const byte CosineIntegral = (byte)OpCode.CosineIntegral;
    public const byte SineIntegral = (byte)OpCode.SineIntegral;
    public const byte HyperbolicCosineIntegral = (byte)OpCode.HyperbolicCosineIntegral;
    public const byte HyperbolicSineIntegral = (byte)OpCode.HyperbolicSineIntegral;
    public const byte FresnelCosineIntegral = (byte)OpCode.FresnelCosineIntegral;
    public const byte FresnelSineIntegral = (byte)OpCode.FresnelSineIntegral;
    public const byte AiryA = (byte)OpCode.AiryA;
    public const byte AiryB = (byte)OpCode.AiryB;
    public const byte Norm = (byte)OpCode.Norm;
    public const byte Erf = (byte)OpCode.Erf;
    public const byte Bessel = (byte)OpCode.Bessel;
    public const byte XOR = (byte)OpCode.XOR;
    public const byte FactorVariable = (byte)OpCode.FactorVariable;
    public const byte BinaryFactorVariable = (byte)OpCode.BinaryFactorVariable;
    public const byte Absolute = (byte)OpCode.Absolute;
    public const byte AnalyticQuotient = (byte)OpCode.AnalyticQuotient;
    public const byte Cube = (byte)OpCode.Cube;
    public const byte CubeRoot = (byte)OpCode.CubeRoot;
    public const byte Tanh = (byte)OpCode.Tanh;
    public const byte SubFunction = (byte)OpCode.SubFunction;

    private static readonly Dictionary<Type, byte> SymbolToOpcode = new Dictionary<Type, byte>() {
      { typeof(Addition), Add },
      { typeof(Subtraction), Sub },
      { typeof(Multiplication), Mul },
      { typeof(Division), Div },
      { typeof(Sine), Sin },
      { typeof(Cosine), Cos },
      { typeof(Tangent), Tan },
      { typeof(HyperbolicTangent), Tanh },
      { typeof(Logarithm), Log },
      { typeof(Exponential), Exp },
      { typeof(IfThenElse), IfThenElse },
      { typeof(GreaterThan), GT },
      { typeof(LessThan), LT },
      { typeof(And), AND },
      { typeof(Or), OR },
      { typeof(Not), NOT },
      { typeof(Xor), XOR },
      { typeof(Average), Average },
      { typeof(InvokeFunction), Call },
      { typeof(Variable), Variable },
      { typeof(LaggedVariable), LagVariable },
      { typeof(AutoregressiveTargetVariable), LagVariable },
      { typeof(Number), Number },
      { typeof(Constant), Constant },
      { typeof(Argument), Arg },
      { typeof(Power), Power },
      { typeof(Root), Root },
      { typeof(TimeLag), TimeLag },
      { typeof(Integral), Integral },
      { typeof(Derivative), Derivative },
      { typeof(VariableCondition), VariableCondition },
      { typeof(Square), Square },
      { typeof(SquareRoot), SquareRoot },
      { typeof(Gamma), Gamma },
      { typeof(Psi), Psi },
      { typeof(Dawson), Dawson },
      { typeof(ExponentialIntegralEi), ExponentialIntegralEi },
      { typeof(CosineIntegral), CosineIntegral },
      { typeof(SineIntegral), SineIntegral },
      { typeof(HyperbolicCosineIntegral), HyperbolicCosineIntegral },
      { typeof(HyperbolicSineIntegral), HyperbolicSineIntegral },
      { typeof(FresnelCosineIntegral), FresnelCosineIntegral },
      { typeof(FresnelSineIntegral), FresnelSineIntegral },
      { typeof(AiryA), AiryA },
      { typeof(AiryB), AiryB },
      { typeof(Norm), Norm },
      { typeof(Erf), Erf },
      { typeof(Bessel), Bessel },
      { typeof(FactorVariable), FactorVariable },
      { typeof(BinaryFactorVariable), BinaryFactorVariable },
      { typeof(Absolute), Absolute },
      { typeof(AnalyticQuotient), AnalyticQuotient },
      { typeof(Cube), Cube },
      { typeof(CubeRoot), CubeRoot },
      { typeof(SubFunctionSymbol), SubFunction }
    };

    public static byte MapSymbolToOpCode(SymbolicExpressionTreeNode treeNode) {
      if (SymbolToOpcode.TryGetValue(treeNode.Symbol.GetType(), out var opCode)) return opCode;
      throw new NotSupportedException("Symbol: " + treeNode.Symbol);
    }
  }

  /// <summary>
  /// Placeholder symbol Function Invocation and Arguments are not yet implemented
  /// </summary>
  internal class Argument(int minimumArity, int maximumArity) : Symbol {
    public override int MinimumArity { get; } = minimumArity;
    public override int MaximumArity { get; } = maximumArity;
  }

  /// <summary>
  /// Placeholder symbol Function Invocation and Arguments are not yet implemented
  /// </summary>
  internal class InvokeFunction(int minimumArity, int maximumArity) : Symbol {
    public override int MinimumArity { get; } = minimumArity;
    public override int MaximumArity { get; } = maximumArity;
  }
}
