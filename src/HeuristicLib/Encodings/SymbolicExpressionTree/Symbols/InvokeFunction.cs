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

using HeuristicLab.Common;
using HeuristicLab.Core;
using HEAL.Attic;
namespace HeuristicLab.Encodings.SymbolicExpressionTreeEncoding {
  /// <summary>
  /// Symbol for invoking automatically defined functions
  /// </summary>
  [StorableType("F98DAAD3-1FCB-490B-9D28-160ED9718441")]
  [Item(InvokeFunctionName, InvokeFunctionDescription)]
  public sealed class InvokeFunction : Symbol, IReadOnlySymbol {
    [Storable] private readonly string functionName;
    public const string InvokeFunctionName = nameof(InvokeFunction);
    public const string InvokeFunctionDescription = "Symbol that the invocation of another function.";
    private const int minimumArity = 0;
    private const int maximumArity = byte.MaxValue;

    public override int MinimumArity => minimumArity;
    public override int MaximumArity => maximumArity;

    
    public string FunctionName => functionName;

    [StorableConstructor]
    private InvokeFunction(StorableConstructorFlag _) : base(_) { }
    private InvokeFunction(InvokeFunction original, Cloner cloner)
      : base(original, cloner) {
      functionName = original.FunctionName;
      name = "Invoke: " + original.FunctionName;
    }
    public InvokeFunction(string functionName)
      : base("Invoke: " + functionName, InvokeFunctionDescription) {
      this.functionName = functionName;
    }

    public override ISymbolicExpressionTreeNode CreateTreeNode() {
      return new InvokeFunctionTreeNode(this);
    }

    public override IDeepCloneable Clone(Cloner cloner) {
      return new InvokeFunction(this, cloner);
    }
  }
}
