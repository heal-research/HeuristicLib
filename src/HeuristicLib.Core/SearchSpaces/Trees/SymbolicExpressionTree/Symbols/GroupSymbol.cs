namespace HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;

public sealed class GroupSymbol : Symbol
{

  public GroupSymbol() : this([]) {}

  public GroupSymbol(IEnumerable<Symbol> symbols) : base(0, 0, 0)
  {
    SymbolsCollection = [..symbols];
    InitialFrequency = 0.0;
  }
  public List<Symbol> SymbolsCollection { get; }
  public IEnumerable<Symbol> Symbols => SymbolsCollection;

  public override IEnumerable<Symbol> Flatten() => base.Flatten().Union(SymbolsCollection.SelectMany(s => s.Flatten()));
}
