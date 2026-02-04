using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

public class LaggedTreeNode : SymbolicExpressionTreeNode
{

  protected LaggedTreeNode(LaggedTreeNode original) : base(original) => Lag = original.Lag;

  public LaggedTreeNode(LaggedSymbol timeLagSymbol) : base(timeLagSymbol) {}
  public new LaggedSymbol Symbol => (LaggedSymbol)base.Symbol;

  public int Lag { get; set; }

  public override bool HasLocalParameters => true;

  public override void ResetLocalParameters(IRandomNumberGenerator random)
  {
    base.ResetLocalParameters(random);
    Lag = random.NextInt(Symbol.MinLag, Symbol.MaxLag + 1);
  }

  public override void ShakeLocalParameters(IRandomNumberGenerator random, double shakingFactor)
  {
    base.ShakeLocalParameters(random, shakingFactor);
    Lag = System.Math.Min(Symbol.MaxLag, System.Math.Max(Symbol.MinLag, Lag + random.NextInt(-1, 2)));
  }

  public override SymbolicExpressionTreeNode Clone() => new LaggedTreeNode(this);
}
