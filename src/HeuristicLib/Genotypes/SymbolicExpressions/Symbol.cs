namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public abstract record Symbol
{
    public abstract string Name { get; }
    public abstract int Arity { get; }
    public abstract void Emit(ref SymbolCompilationContext context);
}

public abstract record PrimitiveSymbol : Symbol
{
    protected PrimitiveSymbol(string name, int arity, SymbolicExpressionOpCode opCode)
    {
        Name = name;
        Arity = arity;
        OpCode = opCode;
    }

    public override string Name { get; }
    public override int Arity { get; }
    public SymbolicExpressionOpCode OpCode { get; }

    public override void Emit(ref SymbolCompilationContext context)
    {
        for (var i = 0; i < Arity; i++)
        {
            context.EmitChild(i);
        }

        context.EmitOperator(OpCode);
    }
}

public sealed record VariableSymbol(string VariableName) : Symbol
{
    public override string Name => VariableName;
    public override int Arity => 0;

    public override void Emit(ref SymbolCompilationContext context)
    {
        context.EmitVariable(VariableName);
    }
}

public sealed record NumericLiteralSymbol(NumericLiteral Literal) : Symbol
{
    public override string Name => Literal.Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
    public override int Arity => 0;

    public override void Emit(ref SymbolCompilationContext context)
    {
        context.EmitNumericLiteral(Literal);
    }
}

public sealed record AddSymbol() : PrimitiveSymbol("+", 2, SymbolicExpressionOpCode.Add);

public sealed record SubtractSymbol() : PrimitiveSymbol("-", 2, SymbolicExpressionOpCode.Subtract);

public sealed record MultiplySymbol() : PrimitiveSymbol("*", 2, SymbolicExpressionOpCode.Multiply);

public sealed record DivideSymbol() : PrimitiveSymbol("/", 2, SymbolicExpressionOpCode.Divide);

public sealed record NegateSymbol() : PrimitiveSymbol("negate", 1, SymbolicExpressionOpCode.Negate);

public sealed record ExpSymbol() : PrimitiveSymbol("exp", 1, SymbolicExpressionOpCode.Exp);

public sealed record LogSymbol() : PrimitiveSymbol("log", 1, SymbolicExpressionOpCode.Log);

public sealed record SqrtSymbol() : PrimitiveSymbol("sqrt", 1, SymbolicExpressionOpCode.Sqrt);

public sealed record SigmoidSymbol : Symbol
{
    public override string Name => "sigmoid";
    public override int Arity => 1;

    public override void Emit(ref SymbolCompilationContext context)
    {
        context.EmitNumericLiteral(new NumericLiteral(1.0, NumericLiteralKind.Fixed));
        context.EmitNumericLiteral(new NumericLiteral(1.0, NumericLiteralKind.Fixed));
        context.EmitChild(0);
        context.EmitOperator(SymbolicExpressionOpCode.Negate);
        context.EmitOperator(SymbolicExpressionOpCode.Exp);
        context.EmitOperator(SymbolicExpressionOpCode.Add);
        context.EmitOperator(SymbolicExpressionOpCode.Divide);
    }
}

public static class Symbols
{
    public static Symbol Add { get; } = new AddSymbol();
    public static Symbol Subtract { get; } = new SubtractSymbol();
    public static Symbol Multiply { get; } = new MultiplySymbol();
    public static Symbol Divide { get; } = new DivideSymbol();
    public static Symbol Negate { get; } = new NegateSymbol();
    public static Symbol Exp { get; } = new ExpSymbol();
    public static Symbol Log { get; } = new LogSymbol();
    public static Symbol Sqrt { get; } = new SqrtSymbol();
    public static Symbol Sigmoid { get; } = new SigmoidSymbol();

    public static IReadOnlyList<Symbol> BasicArithmetic { get; } =
    [
        Add,
        Subtract,
        Multiply,
        Divide,
        Log,
        Sqrt
    ];
}
