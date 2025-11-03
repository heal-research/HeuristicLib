namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols.Math;

public sealed class Number : Symbol {
  #region Properties
  public double MinValue { get; set; }
  public double MaxValue { get; set; }
  public double ManipulatorMu { get; set; }
  private double manipulatorSigma;
  public double ManipulatorSigma {
    get => manipulatorSigma;
    set {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      manipulatorSigma = value;
    }
  }
  private double multiplicativeManipulatorSigma;
  public double MultiplicativeManipulatorSigma {
    get => multiplicativeManipulatorSigma;
    set {
      ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
      multiplicativeManipulatorSigma = value;
    }
  }
  #endregion

  private Number(Number original) : base(original.MinimumArity, original.DefaultArity, original.MaximumArity) {
    MinValue = original.MinValue;
    MaxValue = original.MaxValue;
    ManipulatorMu = original.ManipulatorMu;
    manipulatorSigma = original.manipulatorSigma;
    multiplicativeManipulatorSigma = original.multiplicativeManipulatorSigma;
  }

  public Number() : base(0, 0, 0) {
    ManipulatorMu = 0.0;
    manipulatorSigma = 1.0;
    multiplicativeManipulatorSigma = 0.03;
    MinValue = -20.0;
    MaxValue = 20.0;
  }

  public override SymbolicExpressionTreeNode CreateTreeNode()
    => new NumberTreeNode(this);

  public NumberTreeNode CreateTreeNode(double number) => new(this) { Value = number };
}
