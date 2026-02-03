using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

public sealed class VariableConditionTreeNode : SymbolicExpressionTreeNode
{

  private VariableConditionTreeNode(VariableConditionTreeNode original)
    : base(original)
  {
    Threshold = original.Threshold;
    VariableName = original.VariableName;
    Slope = original.Slope;
  }

  public VariableConditionTreeNode(VariableCondition variableConditionSymbol) : base(variableConditionSymbol) {}
  public override bool HasLocalParameters => true;

  public override SymbolicExpressionTreeNode Clone() => new VariableConditionTreeNode(this);

  public override void ResetLocalParameters(IRandomNumberGenerator random)
  {
    base.ResetLocalParameters(random);
    Threshold = random.NextGaussian(Symbol.ThresholdInitializerMu, Symbol.ThresholdInitializerSigma);
    VariableName = Symbol.VariableNames.SampleRandom(random);
    Slope = random.NextGaussian(Symbol.SlopeInitializerMu, Symbol.SlopeInitializerSigma);
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor)
  {
    base.ShakeLocalParameters(random, shakingFactor);
    var x = random.NextGaussian(Symbol.ThresholdManipulatorMu, Symbol.ThresholdManipulatorSigma);
    Threshold += x * shakingFactor;
    VariableName = Symbol.VariableNames.SampleRandom(random);

    x = random.NextGaussian(Symbol.SlopeManipulatorMu, Symbol.SlopeManipulatorSigma);
    Slope += x * shakingFactor;
  }

  public override string ToString()
  {
    if (Slope.IsAlmost(0.0) || Symbol.IgnoreSlope) {
      return VariableName + " < " + Threshold.ToString("E4");
    }

    return VariableName + " > " + Threshold.ToString("E4") + Environment.NewLine +
           "slope: " + Slope.ToString("E4");
  }

  #region properties

  public new VariableCondition Symbol => (VariableCondition)base.Symbol;
  public double Threshold { get; set; }
  public string VariableName { get; set; } = "";
  public double Slope { get; set; }

  #endregion

}
