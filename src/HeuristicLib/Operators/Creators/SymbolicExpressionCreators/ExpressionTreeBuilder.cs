using HEAL.HeuristicLib.Genotypes.SymbolicExpressions;
using HEAL.HeuristicLib.Random;

namespace HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionCreators;

internal sealed class ExpressionTreeBuilder
{
    public ExpressionTreeBuilder()
    {
        Root = new Position(this);
    }

    public Position Root { get; }

    public Position[] Expand(Position position, Symbol symbol)
    {
        ValidateOwner(position);
        if (!position.IsPending)
            throw new InvalidOperationException("A completed expression position cannot be expanded.");
        if (symbol.Arity == 0)
            throw new ArgumentException("A terminal symbol cannot expand an expression position.", nameof(symbol));

        position.Symbol = symbol;
        position.Children = new Position[symbol.Arity];
        for (var i = 0; i < position.Children.Length; i++)
            position.Children[i] = new Position(this);

        return position.Children;
    }

    public void CompleteWithTerminal(Position position, ExpressionNode terminal)
    {
        ValidateOwner(position);
        if (!position.IsPending)
            throw new InvalidOperationException("An expression position can be completed only once.");
        if (terminal.Arity != 0)
            throw new ArgumentException("Only a terminal node can complete an expression position.", nameof(terminal));

        position.Terminal = terminal;
    }

    public ExpressionNode Build(IRandomNumberGenerator random)
    {
        return Build(Root, random);
    }

    private static ExpressionNode Build(Position position, IRandomNumberGenerator random)
    {
        if (position.Terminal is not null)
            return position.Terminal;
        if (position.Symbol is null)
            throw new InvalidOperationException("The expression tree contains an unfilled position.");

        var children = ImmutableArray.CreateBuilder<ExpressionNode>(position.Children.Length);
        foreach (var child in position.Children)
            children.Add(Build(child, random));

        return position.Symbol.CreateNode(random, children.MoveToImmutable());
    }

    private void ValidateOwner(Position position)
    {
        if (!ReferenceEquals(position.Owner, this))
            throw new ArgumentException("The expression position belongs to another builder.", nameof(position));
    }

    internal sealed class Position
    {
        internal Position(ExpressionTreeBuilder owner)
        {
            Owner = owner;
        }

        internal ExpressionTreeBuilder Owner { get; }
        internal Symbol? Symbol { get; set; }
        internal Position[] Children { get; set; } = [];
        internal ExpressionNode? Terminal { get; set; }

        internal bool IsPending => Symbol is null && Terminal is null;
    }
}
