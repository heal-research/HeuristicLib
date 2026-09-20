using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.LegacySymbolicExpressions;

public record SymbolicExpressionTreeSearchSpace : SearchSpace<SymbolicExpressionTree>
{
    public SymbolicExpressionTreeSearchSpace(ISymbolicExpressionGrammar Grammar, int TreeLength = 50, int TreeDepth = 50)
    {
        this.TreeLength = TreeLength;
        this.TreeDepth = TreeDepth;
        this.Grammar = Grammar;
    }

    public int TreeLength { get; set; }

    public int TreeDepth { get; set; }

    public ISymbolicExpressionGrammar Grammar { get; set; }

    public int FunctionDefinitions { get; set; }

    public int FunctionArguments { get; set; }

    public override bool Contains(SymbolicExpressionTree candidate)
    {
        return candidate.Length <= TreeLength
            && candidate.Depth <= TreeDepth
            && Grammar.Conforms(candidate);
    }
}
