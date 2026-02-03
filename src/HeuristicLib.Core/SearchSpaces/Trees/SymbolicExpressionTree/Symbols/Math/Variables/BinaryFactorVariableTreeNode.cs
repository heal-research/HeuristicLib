using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;

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
    if (random.Random() < 0.5) {
      var x = random.NextGaussian(Symbol.WeightManipulatorMu, Symbol.WeightManipulatorSigma);
      Weight = Weight + x * shakingFactor;
    } else {
      var x = random.NextGaussian(1.0, Symbol.MultiplicativeWeightManipulatorSigma);
      Weight = Weight * x;
    }

    if (random.Random() < Symbol.VariableChangeProbability) {
      var oldName = VariableName;
      VariableName = Symbol.VariableNames.SampleRandom(random);
      // reinitialize weights if variable has changed (similar to FactorVariableTreeNode)
      if (oldName != VariableName) {
        Weight = random.NextGaussian(Symbol.WeightMu, Symbol.WeightSigma);
      }
    }

    VariableValue = Symbol.GetVariableValues(VariableName).SampleRandom(random);
  }

  public override SymbolicExpressionTreeNode Clone() => new BinaryFactorVariableTreeNode(this);

  public override string ToString() => base.ToString() + " = " + VariableValue;
}
