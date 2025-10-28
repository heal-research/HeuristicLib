using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;

public sealed class LaggedVariableTreeNode : VariableTreeNodeBase {
  public new LaggedVariable Symbol => (LaggedVariable)base.Symbol;

  public int Lag { get; set; }

  public override bool HasLocalParameters => true;

  public LaggedVariableTreeNode(LaggedVariableTreeNode other) : base(other) { }
  public LaggedVariableTreeNode(LaggedVariable variableSymbol) : base(variableSymbol) { }

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    Lag = random.Integer(Symbol.MinLag, Symbol.MaxLag + 1);
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);
    Lag = System.Math.Min(Symbol.MaxLag, System.Math.Max(Symbol.MinLag, Lag + random.Integer(-1, 2)));
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new LaggedVariableTreeNode(this);
  }

  public override string ToString() {
    return base.ToString() + " (t" + (Lag > 0 ? "+" : "") + Lag + ")";
  }
}
