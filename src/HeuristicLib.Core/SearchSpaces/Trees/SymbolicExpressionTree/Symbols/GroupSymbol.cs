namespace HEAL.HeuristicLib.Encodings.SymbolicExpressionTree.Symbols;

public sealed class GroupSymbol : Symbol {
  public List<Symbol> SymbolsCollection { get; }
  public IEnumerable<Symbol> Symbols => SymbolsCollection;

  public GroupSymbol() : this([]) { }

  public GroupSymbol(IEnumerable<Symbol> symbols) : base(0, 0, 0) {
    SymbolsCollection = [..symbols];
    InitialFrequency = 0.0;
  }

  public override IEnumerable<Symbol> Flatten() {
    return base.Flatten().Union(SymbolsCollection.SelectMany(s => s.Flatten()));
  }
}
