using HEAL.HeuristicLib.Genotypes.Trees;

namespace HEAL.HeuristicLib.Encodings.Trees.SymbolicExpressionTree.Symbols.Math;

public sealed class VariableCondition() : Symbol(2, 2, 2) {
  #region properties
  public double ThresholdInitializerMu { get; set; } = 0.0;
  private double thresholdInitializerSigma = 0.1;
  public double ThresholdInitializerSigma {
    get => thresholdInitializerSigma;
    set {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      thresholdInitializerSigma = value;
    }
  }

  public double ThresholdManipulatorMu { get; set; } = 0.0;
  private double thresholdManipulatorSigma = 0.1;
  public double ThresholdManipulatorSigma {
    get => thresholdManipulatorSigma;
    set {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      thresholdManipulatorSigma = value;
    }
  }

  private readonly List<string> variableNames = [];
  public IReadOnlyList<string> VariableNames {
    get => variableNames;
    set {
      variableNames.Clear();
      variableNames.AddRange(value);
    }
  }

  private readonly List<string> allVariableNames = [];
  public IReadOnlyList<string> AllVariableNames {
    get => allVariableNames;
    set {
      allVariableNames.Clear();
      allVariableNames.AddRange(value);
    }
  }

  public double SlopeInitializerMu { get; set; } = 0.0;
  private double slopeInitializerSigma;
  public double SlopeInitializerSigma {
    get => slopeInitializerSigma;
    set {
      if (slopeInitializerSigma < 0.0)
        throw new ArgumentException("Negative sigma is not allowed.");
      slopeInitializerSigma = value;
    }
  }

  public double SlopeManipulatorMu { get; set; } = 0.0;
  private double slopeManipulatorSigma;
  public double SlopeManipulatorSigma {
    get => slopeManipulatorSigma;
    set {
      if (slopeManipulatorSigma < 0.0)
        throw new ArgumentException("Negative sigma is not allowed.");
      slopeManipulatorSigma = value;
    }
  }

  /// <summary>
  /// Flag to indicate if the interpreter should ignore the slope parameter (introduced for representation of expression trees)
  /// </summary>

  public bool IgnoreSlope { get; set; }
  #endregion

  public override SymbolicExpressionTreeNode CreateTreeNode() {
    return new VariableConditionTreeNode(this);
  }
}
