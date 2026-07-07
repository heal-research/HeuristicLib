namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public abstract record ExpressionDraft
{
    public SymbolicExpression Build()
    {
        var symbols = new List<Symbol>();

        Emit(this, symbols);

        return SymbolicExpression.Create(symbols);
    }

    private static void Emit(ExpressionDraft draft, List<Symbol> symbols)
    {
        switch (draft)
        {
            case VariableDraft variable:
                symbols.Add(new VariableSymbol(variable.Name));
                return;
            case NumericLiteralDraft literal:
                symbols.Add(new NumericLiteralSymbol(new NumericLiteral(literal.Value, literal.Kind)));
                return;
            case UnaryDraft unary:
                Emit(unary.Child, symbols);
                symbols.Add(unary.Symbol);
                return;
            case BinaryDraft binary:
                Emit(binary.Left, symbols);
                Emit(binary.Right, symbols);
                symbols.Add(binary.Symbol);
                return;
            default:
                throw new InvalidOperationException($"Unsupported expression draft node {draft.GetType()}.");
        }
    }

    internal sealed record VariableDraft(string Name) : ExpressionDraft;

    internal sealed record NumericLiteralDraft(double Value, NumericLiteralKind Kind) : ExpressionDraft;

    internal sealed record UnaryDraft(Symbol Symbol, ExpressionDraft Child) : ExpressionDraft;

    internal sealed record BinaryDraft(Symbol Symbol, ExpressionDraft Left, ExpressionDraft Right) : ExpressionDraft;

    public static ExpressionDraft Variable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name must not be empty.", nameof(name));

        return new VariableDraft(name);
    }

    public static ExpressionDraft Fixed(double value) => new NumericLiteralDraft(value, NumericLiteralKind.Fixed);

    public static ExpressionDraft Parameter(double value) => new NumericLiteralDraft(value, NumericLiteralKind.Optimizable);

    public static ExpressionDraft Add(ExpressionDraft left, ExpressionDraft right) => new BinaryDraft(new AddSymbol(), left, right);

    public static ExpressionDraft Subtract(ExpressionDraft left, ExpressionDraft right) => new BinaryDraft(new SubtractSymbol(), left, right);

    public static ExpressionDraft Multiply(ExpressionDraft left, ExpressionDraft right) => new BinaryDraft(new MultiplySymbol(), left, right);

    public static ExpressionDraft Divide(ExpressionDraft left, ExpressionDraft right) => new BinaryDraft(new DivideSymbol(), left, right);

    public static ExpressionDraft Negate(ExpressionDraft child) => new UnaryDraft(new NegateSymbol(), child);

    public static ExpressionDraft Exp(ExpressionDraft child) => new UnaryDraft(new ExpSymbol(), child);

    public static ExpressionDraft Log(ExpressionDraft child) => new UnaryDraft(new LogSymbol(), child);

    public static ExpressionDraft Sqrt(ExpressionDraft child) => new UnaryDraft(new SqrtSymbol(), child);

    public static ExpressionDraft Sigmoid(ExpressionDraft child) => new UnaryDraft(new SigmoidSymbol(), child);

    public static ExpressionDraft operator +(ExpressionDraft left, ExpressionDraft right) => Add(left, right);

    public static ExpressionDraft operator -(ExpressionDraft left, ExpressionDraft right) => Subtract(left, right);

    public static ExpressionDraft operator *(ExpressionDraft left, ExpressionDraft right) => Multiply(left, right);

    public static ExpressionDraft operator /(ExpressionDraft left, ExpressionDraft right) => Divide(left, right);
}
