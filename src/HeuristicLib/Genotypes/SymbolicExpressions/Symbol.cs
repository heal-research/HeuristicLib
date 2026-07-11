using Generator.Equals;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public abstract record Symbol(string Name, int Arity)
{
    public virtual bool SupportsLocalPerturbation => false;

    public virtual ExpressionNode CreateNode(IRandomNumberGenerator random)
    {
        return new ExpressionNode(this);
    }

    public virtual bool CanPerturb(ExpressionNode node)
    {
        return false;
    }

    public virtual bool TryPerturb(ExpressionNode node, IRandomNumberGenerator random, out ExpressionNode perturbed)
    {
        perturbed = node;
        return false;
    }

    internal void EmitNode(ExpressionNode node, IExpressionEmitter emitter)
    {
        Emit(node, emitter);
    }

    protected abstract void Emit(ExpressionNode node, IExpressionEmitter emitter);
}

public abstract record OperationSymbol(string Name, int Arity) : Symbol(Name, Arity);

public abstract record BuiltInOperationSymbol(string Name, OpCode OpCode)
    : OperationSymbol(Name, OpCodes.GetArity(OpCode))
{
    protected override void Emit(ExpressionNode node, IExpressionEmitter emitter)
    {
        for (var i = 0; i < Arity; i++)
            emitter.EmitChild(i);

        emitter.EmitOperator(OpCode);
    }
}

public sealed record AdditionSymbol() : BuiltInOperationSymbol("+", OpCode.Add);
public sealed record SubtractionSymbol() : BuiltInOperationSymbol("-", OpCode.Subtract);
public sealed record MultiplicationSymbol() : BuiltInOperationSymbol("*", OpCode.Multiply);
public sealed record DivisionSymbol() : BuiltInOperationSymbol("/", OpCode.Divide);
public sealed record NegationSymbol() : BuiltInOperationSymbol("negate", OpCode.Negate);
public sealed record ExponentialSymbol() : BuiltInOperationSymbol("exp", OpCode.Exp);
public sealed record LogarithmSymbol() : BuiltInOperationSymbol("log", OpCode.Log);
public sealed record SquareRootSymbol() : BuiltInOperationSymbol("sqrt", OpCode.Sqrt);

public sealed record SigmoidSymbol() : OperationSymbol("sigmoid", 1)
{
    protected override void Emit(ExpressionNode node, IExpressionEmitter emitter)
    {
        emitter.EmitConstant(1.0);
        emitter.EmitConstant(1.0);
        emitter.EmitChild(0);
        emitter.EmitOperator(OpCode.Negate);
        emitter.EmitOperator(OpCode.Exp);
        emitter.EmitOperator(OpCode.Add);
        emitter.EmitOperator(OpCode.Divide);
    }
}

public abstract record ConstantSymbol(string Name) : Symbol(Name, 0);

public sealed record FixedConstantSymbol(double Value, string? DisplayName = null)
    : ConstantSymbol(DisplayName ?? Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture))
{
    public override ExpressionNode CreateNode(IRandomNumberGenerator random)
    {
        return new ExpressionNode(this, Value);
    }

    protected override void Emit(ExpressionNode node, IExpressionEmitter emitter)
    {
        emitter.EmitConstant(node.NumericValue);
    }
}

public sealed record EvolvableConstantSymbol(IDistribution<double> InitialDistribution, NumericPerturbation Perturbation)
    : ConstantSymbol("constant")
{
    public EvolvableConstantSymbol()
        : this(new UniformDoubleDistribution(-1.0, 1.0), NumericPerturbation.Default)
    {
    }

    public override bool SupportsLocalPerturbation => true;

    public override ExpressionNode CreateNode(IRandomNumberGenerator random)
    {
        return new ExpressionNode(this, InitialDistribution.Sample(random));
    }

    public override bool CanPerturb(ExpressionNode node)
    {
        return node.Symbol == this && node.HasNumericValue;
    }

    public override bool TryPerturb(ExpressionNode node, IRandomNumberGenerator random, out ExpressionNode perturbed)
    {
        if (!CanPerturb(node))
        {
            perturbed = node;
            return false;
        }

        if (!Perturbation.TryApply(node.NumericValue, this, random, out var value))
        {
            perturbed = node;
            return false;
        }

        perturbed = new ExpressionNode(this, value);
        return true;
    }

    protected override void Emit(ExpressionNode node, IExpressionEmitter emitter)
    {
        emitter.EmitConstant(node.NumericValue);
    }
}

[Equatable]
public sealed partial record VariableSymbol : Symbol
{
    [OrderedEquality] public ImmutableArray<string> Variables { get; }
    [OrderedEquality] public ImmutableArray<double> SelectionWeights { get; }

    public VariableSymbol(IEnumerable<string> variables, IEnumerable<double>? selectionWeights = null)
        : base("variable", 0)
    {
        Variables = variables.ToImmutableArray();
        if (Variables.IsDefaultOrEmpty)
            throw new ArgumentException("At least one variable must be supplied.", nameof(variables));

        if (Variables.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Variable names must not be empty.", nameof(variables));

        SelectionWeights = WeightSelection.Normalize(selectionWeights?.ToImmutableArray(), Variables.Length);
    }

    public override bool SupportsLocalPerturbation => true;

    public override ExpressionNode CreateNode(IRandomNumberGenerator random)
    {
        var index = WeightSelection.SelectIndex(random, Variables.Length, SelectionWeights);
        return new ExpressionNode(this, Variables[index]);
    }

    public override bool CanPerturb(ExpressionNode node)
    {
        return node.Symbol == this && node.HasVariableName;
    }

    public override bool TryPerturb(ExpressionNode node, IRandomNumberGenerator random, out ExpressionNode perturbed)
    {
        if (!CanPerturb(node))
        {
            perturbed = node;
            return false;
        }

        perturbed = CreateNode(random);
        return true;
    }

    protected override void Emit(ExpressionNode node, IExpressionEmitter emitter)
    {
        if (node.Symbol != this || !node.HasVariableName)
            throw new InvalidOperationException("A variable symbol can emit only its own variable node.");

        emitter.EmitVariable(node.VariableName!);
    }
}

public static class Symbols
{
    public static AdditionSymbol Addition { get; } = new();
    public static SubtractionSymbol Subtraction { get; } = new();
    public static MultiplicationSymbol Multiplication { get; } = new();
    public static DivisionSymbol Division { get; } = new();
    public static NegationSymbol Negation { get; } = new();
    public static ExponentialSymbol Exponential { get; } = new();
    public static LogarithmSymbol Logarithm { get; } = new();
    public static SquareRootSymbol SquareRoot { get; } = new();
    public static SigmoidSymbol Sigmoid { get; } = new();

    public static IReadOnlyList<OperationSymbol> BasicArithmetic { get; } = [Addition, Subtraction, Multiplication, Division];
    public static IReadOnlyList<OperationSymbol> ElementaryFunctions { get; } = [Negation, Exponential, Logarithm, SquareRoot, Sigmoid];

    public static IReadOnlyList<OperationSymbol> Standard { get; } = [.. BasicArithmetic, .. ElementaryFunctions];

    public static EvolvableConstantSymbol Constant(IDistribution<double>? initialDistribution = null, NumericPerturbation? perturbation = null)
    {
        return new EvolvableConstantSymbol(initialDistribution ?? new UniformDoubleDistribution(-1.0, 1.0), perturbation ?? NumericPerturbation.Default);
    }

    public static FixedConstantSymbol FixedConstant(double value, string? displayName = null) => new(value, displayName);
    public static VariableSymbol Variable(IEnumerable<string> variables, IEnumerable<double>? selectionWeights = null) => new(variables, selectionWeights);
}
