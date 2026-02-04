using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

public sealed class BinaryFactorVariableTreeNode : VariableTreeNodeBase
{

  private BinaryFactorVariableTreeNode(BinaryFactorVariableTreeNode original) : base(original) => VariableValue = original.VariableValue;

  public BinaryFactorVariableTreeNode(BinaryFactorVariable variableSymbol) : base(variableSymbol) {}
  public new BinaryFactorVariable Symbol => (BinaryFactorVariable)base.Symbol;

  public string VariableValue { get; set; } = "";

  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandomNumberGenerator random)
  {
    base.ResetLocalParameters(random);
    VariableValue = Symbol.GetVariableValues(VariableName).SampleRandom(random);
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor)
  {
    // 50% additive & 50% multiplicative (override of functionality of base class because of a BUG)
    if (random.NextDouble() < 0.5) {
      var x = NormalDistribution.NextDouble(random, Symbol.WeightManipulatorMu, Symbol.WeightManipulatorSigma);
      Weight = Weight + x * shakingFactor;
    } else {
      var x = NormalDistribution.NextDouble(random, 1.0, Symbol.MultiplicativeWeightManipulatorSigma);
      Weight = Weight * x;
    }

    if (random.NextDouble() < Symbol.VariableChangeProbability) {
      var oldName = VariableName;
      VariableName = Symbol.VariableNames.SampleRandom(random);
      // reinitialize weights if variable has changed (similar to FactorVariableTreeNode)
      if (oldName != VariableName) {
        Weight = NormalDistribution.NextDouble(random, Symbol.WeightMu, Symbol.WeightSigma);
      }
    }

    VariableValue = Symbol.GetVariableValues(VariableName).SampleRandom(random);
  }

  public override SymbolicExpressionTreeNode Clone() => new BinaryFactorVariableTreeNode(this);

  public override string ToString() => base.ToString() + " = " + VariableValue;
}
