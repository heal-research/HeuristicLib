using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Encodings.SymbolicExpression.Symbols.Math;

public class LaggedTreeNode : SymbolicExpressionTreeNode {
  public new LaggedSymbol Symbol => (LaggedSymbol)base.Symbol;

  public int Lag { get; set; }

  protected LaggedTreeNode(LaggedTreeNode original) : base(original) => Lag = original.Lag;

  public LaggedTreeNode(LaggedSymbol timeLagSymbol) : base(timeLagSymbol) { }

  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandomNumberGenerator random) {
    base.ResetLocalParameters(random);
    Lag = random.Integer(Symbol.MinLag, Symbol.MaxLag + 1);
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor) {
    base.ShakeLocalParameters(random, shakingFactor);
    Lag = System.Math.Min(Symbol.MaxLag, System.Math.Max(Symbol.MinLag, Lag + random.Integer(-1, 2)));
  }

  public override SymbolicExpressionTreeNode Clone() {
    return new LaggedTreeNode(this);
  }
}
