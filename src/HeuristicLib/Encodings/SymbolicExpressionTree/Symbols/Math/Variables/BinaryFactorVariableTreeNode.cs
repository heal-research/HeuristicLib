using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math.Variables;

public sealed class BinaryFactorVariableTreeNode : VariableTreeNodeBase {
  public new BinaryFactorVariable Symbol => (BinaryFactorVariable)base.Symbol;

  public string VariableValue { get; set; } = "";

  private BinaryFactorVariableTreeNode(BinaryFactorVariableTreeNode original) : base(original) {
    VariableValue = original.VariableValue;
  }

  public BinaryFactorVariableTreeNode(BinaryFactorVariable variableSymbol) : base(variableSymbol) { }

  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    VariableValue = Symbol.GetVariableValues(VariableName).SampleRandom(random);
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    // 50% additive & 50% multiplicative (override of functionality of base class because of a BUG)
    if (random.Random() < 0.5) {
      var x = NormalDistributedRandomPolar.NextDouble(random, Symbol.WeightManipulatorMu, Symbol.WeightManipulatorSigma);
      Weight = Weight + x * shakingFactor;
    } else {
      var x = NormalDistributedRandomPolar.NextDouble(random, 1.0, Symbol.MultiplicativeWeightManipulatorSigma);
      Weight = Weight * x;
    }

    if (random.Random() < Symbol.VariableChangeProbability) {
      var oldName = VariableName;
      VariableName = Symbol.VariableNames.SampleRandom(random);
      // reinitialize weights if variable has changed (similar to FactorVariableTreeNode)
      if (oldName != VariableName)
        Weight = NormalDistributedRandomPolar.NextDouble(random, Symbol.WeightMu, Symbol.WeightSigma);
    }

    VariableValue = Symbol.GetVariableValues(VariableName).SampleRandom(random);
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new BinaryFactorVariableTreeNode(this);
  }

  public override string ToString() {
    return base.ToString() + " = " + VariableValue;
  }
}
