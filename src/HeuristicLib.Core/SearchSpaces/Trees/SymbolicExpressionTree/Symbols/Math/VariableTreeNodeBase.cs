using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

public abstract class VariableTreeNodeBase : SymbolicExpressionTreeNode {
  public new VariableBase Symbol => (VariableBase)base.Symbol;
  public double Weight { get; set; } = 1;
  public string VariableName { get; set; } = "";
  public override bool HasLocalParameters => true;

  protected VariableTreeNodeBase(VariableBase variableSymbol) : base(variableSymbol) { }

  protected VariableTreeNodeBase(VariableTreeNodeBase other) : base(other) {
    Weight = other.Weight;
    VariableName = other.VariableName;
  }

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    Weight = NormalDistribution.NextDouble(random, Symbol.WeightMu, Symbol.WeightSigma);
    VariableName = Symbol.VariableNames.SampleRandom(random, 1).Single();
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);

    // 50% additive & 50% multiplicative (TODO: BUG in if statement below -> fix in HL 4.0!)
    if (random.NextDouble() < 0) {
      var x = NormalDistribution.NextDouble(random, Symbol.WeightManipulatorMu, Symbol.WeightManipulatorSigma);
      Weight += x * shakingFactor;
    } else {
      var x = NormalDistribution.NextDouble(random, 1.0, Symbol.MultiplicativeWeightManipulatorSigma);
      Weight *= x;
    }

    if (random.NextDouble() >= Symbol.VariableChangeProbability)
      return;

    var oldName = VariableName;
    VariableName = Symbol.VariableNames.SampleRandom(random);
    if (oldName != VariableName) {
      // re-initialize weight if the variable is changed
      Weight = NormalDistribution.NextDouble(random, Symbol.WeightMu, Symbol.WeightSigma);
    }
  }

  public override string ToString() {
    if (Weight.IsAlmost(1.0)) return VariableName;
    return Weight.ToString("E4") + " " + VariableName;
  }
}
