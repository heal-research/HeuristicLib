using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;

public sealed class NumberTreeNode : SymbolicExpressionTreeNode {
  public new Number Symbol => (Number)base.Symbol;

  public double Value { get; set; }

  public NumberTreeNode(Number numberSymbol) : base(numberSymbol) { }

  public NumberTreeNode(NumberTreeNode original) : base(original) {
    Value = original.Value;
  }

  public NumberTreeNode(double value) : this(new Number()) {
    Value = value;
  }

  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    var range = Symbol.MaxValue - Symbol.MinValue;
    Value = random.Random() * range + Symbol.MinValue;
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);
    // 50% additive & 50% multiplicative
    if (random.Random() < 0.5) {
      var x = random.NextGaussian(Symbol.ManipulatorMu, Symbol.ManipulatorSigma);
      Value = Value + x * shakingFactor;
    } else {
      var x = random.NextGaussian(1.0, Symbol.MultiplicativeManipulatorSigma);
      Value = Value * x;
    }
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new NumberTreeNode(this);
  }

  public override string ToString() {
    return $"{Value:E4}";
  }
}
