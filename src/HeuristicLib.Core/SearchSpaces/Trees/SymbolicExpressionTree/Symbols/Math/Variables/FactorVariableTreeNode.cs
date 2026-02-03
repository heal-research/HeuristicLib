using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math.Variables;

public sealed class FactorVariableTreeNode : SymbolicExpressionTreeNode
{

  private FactorVariableTreeNode(FactorVariableTreeNode original) : base(original)
  {
    VariableName = original.VariableName;
    if (original.Weights == null) {
      return;
    }
    Weights = new double[original.Weights.Length];
    Array.Copy(original.Weights, Weights, Weights.Length);
  }

  public FactorVariableTreeNode(FactorVariable variableSymbol)
    : base(variableSymbol)
  {
  }
  public new FactorVariable Symbol => (FactorVariable)base.Symbol;
  public double[]? Weights { get; set; }
  public string VariableName { get; set; } = "";

  public override bool HasLocalParameters => true;

  public override SymbolicExpressionTreeNode Clone() => new FactorVariableTreeNode(this);

  public override void ResetLocalParameters(IRandomNumberGenerator random)
  {
    base.ResetLocalParameters(random);
    VariableName = Symbol.VariableNames.SampleRandom(random);
    Weights =
      Symbol.GetVariableValues(VariableName)
        .Select(_ => random.NextGaussian()).ToArray();
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor)
  {
    // mutate only one randomly selected weight
    var idx = random.Integer(Weights!.Length);
    // 50% additive & 50% multiplicative
    if (random.Random() < 0.5) {
      var x = random.NextGaussian(Symbol.WeightManipulatorMu,
      Symbol.WeightManipulatorSigma);
      Weights[idx] += x * shakingFactor;
    } else {
      var x = random.NextGaussian(1.0, Symbol.MultiplicativeWeightManipulatorSigma);
      Weights[idx] *= x;
    }

    if (random.Random() >= Symbol.VariableChangeProbability) {
      return;
    }

    VariableName = Symbol.VariableNames.SampleRandom(random);
    if (Weights.Length != Symbol.GetVariableValues(VariableName).Count()) {
      // if the length of the weight array does not match => re-initialize weights
      Weights =
        Symbol.GetVariableValues(VariableName)
          .Select(_ => random.NextGaussian())
          .ToArray();
    }
  }

  public double GetValue(string cat) => Weights![Symbol.GetIndexForValue(VariableName, cat)];

  public override string ToString()
  {
    var weightStr = string.Join("; ",
    Symbol.GetVariableValues(VariableName).Select(value => value + ": " + GetValue(value).ToString("E4")));

    return VariableName + " (factor) "
                        + "[" + weightStr + "]";
  }
}
