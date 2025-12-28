using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

public sealed class VariableConditionTreeNode : SymbolicExpressionTreeNode {
  #region properties
  public new VariableCondition Symbol => (VariableCondition)base.Symbol;
  public double Threshold { get; set; }
  public string VariableName { get; set; } = "";
  public double Slope { get; set; }
  #endregion

  private VariableConditionTreeNode(VariableConditionTreeNode original)
    : base(original) {
    Threshold = original.Threshold;
    VariableName = original.VariableName;
    Slope = original.Slope;
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new VariableConditionTreeNode(this);
  }

  public VariableConditionTreeNode(VariableCondition variableConditionSymbol) : base(variableConditionSymbol) { }
  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    Threshold = NormalDistributedRandomPolar.NextDouble(random, Symbol.ThresholdInitializerMu, Symbol.ThresholdInitializerSigma);
    VariableName = Symbol.VariableNames.SampleRandom(random);
    Slope = NormalDistributedRandomPolar.NextDouble(random, Symbol.SlopeInitializerMu, Symbol.SlopeInitializerSigma);
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);
    var x = NormalDistributedRandomPolar.NextDouble(random, Symbol.ThresholdManipulatorMu, Symbol.ThresholdManipulatorSigma);
    Threshold += x * shakingFactor;
    VariableName = Symbol.VariableNames.SampleRandom(random);

    x = NormalDistributedRandomPolar.NextDouble(random, Symbol.SlopeManipulatorMu, Symbol.SlopeManipulatorSigma);
    Slope += x * shakingFactor;
  }

  public override string ToString() {
    if (Slope.IsAlmost(0.0) || Symbol.IgnoreSlope) {
      return VariableName + " < " + Threshold.ToString("E4");
    }

    return VariableName + " > " + Threshold.ToString("E4") + Environment.NewLine +
           "slope: " + Slope.ToString("E4");
  }
}
