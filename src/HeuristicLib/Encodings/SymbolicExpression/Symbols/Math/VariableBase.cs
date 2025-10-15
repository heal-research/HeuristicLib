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

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;

public abstract class VariableBase : Symbol {
  #region Properties
  public double WeightMu { get; set; }
  private double weightSigma;
  public double WeightSigma {
    get => weightSigma;
    set {
      if (weightSigma < 0.0) throw new ArgumentException("Negative sigma is not allowed.");
      weightSigma = value;
    }
  }
  public double WeightManipulatorMu { get; set; }
  private double weightManipulatorSigma;
  public double WeightManipulatorSigma {
    get => weightManipulatorSigma;
    set {
      if (weightManipulatorSigma < 0.0) throw new ArgumentException("Negative sigma is not allowed.");
      weightManipulatorSigma = value;
    }
  }
  private double multiplicativeWeightManipulatorSigma;
  public double MultiplicativeWeightManipulatorSigma {
    get => multiplicativeWeightManipulatorSigma;
    set {
      if (multiplicativeWeightManipulatorSigma < 0.0) throw new ArgumentException("Negative sigma is not allowed.");
      multiplicativeWeightManipulatorSigma = value;
    }
  }

  private double variableChangeProbability;

  public double VariableChangeProbability {
    get => variableChangeProbability;
    set {
      if (value is < 0 or > 1.0) throw new ArgumentException("Variable change probability must lie in the interval [0..1]");
      variableChangeProbability = value;
    }
  }

  private readonly List<string> variableNames;
  public IEnumerable<string> VariableNames {
    get => variableNames;
    set {
      ArgumentNullException.ThrowIfNull(value);
      variableNames.Clear();
      variableNames.AddRange(value);
    }
  }

  private List<string> allVariableNames;
  public IEnumerable<string> AllVariableNames {
    get => allVariableNames;
    set {
      ArgumentNullException.ThrowIfNull(value);
      allVariableNames.Clear();
      allVariableNames.AddRange(value);
    }
  }

  public override int MinimumArity => 0;
  public override int MaximumArity => 0;
  #endregion

  protected VariableBase(VariableBase original) {
    WeightMu = original.WeightMu;
    weightSigma = original.weightSigma;
    variableNames = [..original.variableNames];
    allVariableNames = [..original.allVariableNames];
    WeightManipulatorMu = original.WeightManipulatorMu;
    weightManipulatorSigma = original.weightManipulatorSigma;
    multiplicativeWeightManipulatorSigma = original.multiplicativeWeightManipulatorSigma;
    variableChangeProbability = original.variableChangeProbability;
  }

  protected VariableBase() {
    WeightMu = 1.0;
    weightSigma = 1.0;
    WeightManipulatorMu = 0.0;
    weightManipulatorSigma = 0.05;
    multiplicativeWeightManipulatorSigma = 0.03;
    variableChangeProbability = 0.2;
    variableNames = [];
    allVariableNames = [];
  }
}
