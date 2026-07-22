using Generator.Equals;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.Random.Distributions;

namespace HEAL.HeuristicLib.Genotypes.SymbolicExpressions;

public abstract record Symbol(string Name, int Arity)
{
    public virtual bool SupportsLocalPerturbation => false;

    public abstract ExpressionNode CreateNode(IRandomNumberGenerator random, params ImmutableArray<ExpressionNode> children);

    public virtual bool CanPerturb(ExpressionNode node)
    {
        return false;
    }

    public virtual bool TryPerturb(ExpressionNode node, IRandomNumberGenerator random, out ExpressionNode perturbed)
    {
        perturbed = node;
        return false;
    }

    public abstract void Emit(ExpressionNode node, IExpressionEmitter emitter);

    private protected void ValidateChildCount(ImmutableArray<ExpressionNode> children)
    {
        if (children.Length != Arity)
            throw new ArgumentException($"Symbol '{Name}' requires {Arity} children but received {children.Length}.", nameof(children));
    }
}

public abstract record TerminalSymbol(string Name) : Symbol(Name, 0);

public abstract record ConstantSymbol(string Name) : TerminalSymbol(Name);

public sealed record FixedConstantSymbol(double Value, string? DisplayName = null)
    : ConstantSymbol(DisplayName ?? Value.ToString("G", System.Globalization.CultureInfo.InvariantCulture))
{
    public override ExpressionNode CreateNode(IRandomNumberGenerator random, params ImmutableArray<ExpressionNode> children)
    {
        ValidateChildCount(children);
        return new NumericConstantExpressionNode(this, Value);
    }

    public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
    {
        if (node is not NumericConstantExpressionNode constant || constant.Symbol != this)
            throw new InvalidOperationException("A fixed constant symbol can emit only its own numeric constant node.");

        emitter.EmitConstant(constant.Value);
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

    public override ExpressionNode CreateNode(IRandomNumberGenerator random, params ImmutableArray<ExpressionNode> children)
    {
        ValidateChildCount(children);
        return new NumericConstantExpressionNode(this, SampleInitialValue(random));
    }

    public override bool CanPerturb(ExpressionNode node)
    {
        return node is NumericConstantExpressionNode && node.Symbol == this;
    }

    public override bool TryPerturb(ExpressionNode node, IRandomNumberGenerator random, out ExpressionNode perturbed)
    {
        if (node is not NumericConstantExpressionNode constant || constant.Symbol != this)
        {
            perturbed = node;
            return false;
        }

        if (!Perturbation.TryApply(constant.Value, this, random, out var value))
        {
            perturbed = node;
            return false;
        }

        perturbed = new NumericConstantExpressionNode(this, value);
        return true;
    }

    public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
    {
        if (node is not NumericConstantExpressionNode constant || constant.Symbol != this)
            throw new InvalidOperationException("An evolvable constant symbol can emit only its own numeric constant node.");

        emitter.EmitConstant(constant.Value);
    }

    internal double SampleInitialValue(IRandomNumberGenerator random) => InitialDistribution.Sample(random);
}

[Equatable]
public sealed partial record VariableSymbol : TerminalSymbol
{
    public VariableSymbol(IEnumerable<string> variables, IEnumerable<double>? selectionWeights = null)
        : base("variable")
    {
        Variables = variables.ToImmutableArray();
        if (Variables.IsDefaultOrEmpty)
            throw new ArgumentException("At least one variable must be supplied.", nameof(variables));

        if (Variables.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Variable names must not be empty.", nameof(variables));

        SelectionWeights = WeightSelection.Normalize(selectionWeights?.ToImmutableArray(), Variables.Length);
    }

    [OrderedEquality] public ImmutableArray<string> Variables { get; }
    [OrderedEquality] public ImmutableArray<double> SelectionWeights { get; }

    public override bool SupportsLocalPerturbation => true;

    public override ExpressionNode CreateNode(IRandomNumberGenerator random, params ImmutableArray<ExpressionNode> children)
    {
        ValidateChildCount(children);
        return new VariableExpressionNode(this, Sample(random));
    }

    public override bool CanPerturb(ExpressionNode node)
    {
        return node is VariableExpressionNode && node.Symbol == this;
    }

    public override bool TryPerturb(ExpressionNode node, IRandomNumberGenerator random, out ExpressionNode perturbed)
    {
        if (!CanPerturb(node))
        {
            perturbed = node;
            return false;
        }

        perturbed = new VariableExpressionNode(this, Sample(random));
        return true;
    }

    public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
    {
        if (node is not VariableExpressionNode variable || variable.Symbol != this)
            throw new InvalidOperationException("A variable symbol can emit only its own variable node.");

        emitter.EmitVariable(variable.VariableName);
    }

    internal string Sample(IRandomNumberGenerator random)
    {
        var index = WeightSelection.SelectIndex(random, Variables.Length, SelectionWeights);
        return Variables[index];
    }
}

public abstract record PayloadlessTerminalSymbol(string Name) : TerminalSymbol(Name)
{
    public override ExpressionNode CreateNode(IRandomNumberGenerator random, params ImmutableArray<ExpressionNode> children)
    {
        ValidateChildCount(children);

        return new PayloadlessTerminalExpressionNode(this);
    }
}

public abstract record OperationSymbol(string Name, int Arity) : Symbol(Name, Arity)
{
    public override ExpressionNode CreateNode(IRandomNumberGenerator random, params ImmutableArray<ExpressionNode> children)
    {
        ValidateChildCount(children);

        return Arity switch
        {
            1 => new UnaryExpressionNode(this, children[0]),
            2 => new BinaryExpressionNode(this, children[0], children[1]),
            >= 3 => new NaryExpressionNode(this, children),
            _ => throw new InvalidOperationException("An operation symbol must have positive arity.")
        };
    }
}

public abstract record BuiltInOperationSymbol(string Name, OpCode OpCode)
    : OperationSymbol(Name, OpCodes.GetArity(OpCode))
{
    public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
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
    public override void Emit(ExpressionNode node, IExpressionEmitter emitter)
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
