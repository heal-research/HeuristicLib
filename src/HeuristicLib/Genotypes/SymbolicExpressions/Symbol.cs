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

public sealed record VariableSymbol : TerminalSymbol
{
    public VariableSymbol(IEnumerable<string> variables, IEnumerable<double>? selectionWeights = null)
        : base("variable")
    {
        Variables = variables.ToImmutableArray();
        if (Variables.IsEmpty)
            throw new ArgumentException("At least one variable must be supplied.", nameof(variables));

        if (Variables.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Variable names must not be empty.", nameof(variables));

        SelectionWeights = WeightSelection.Normalize(selectionWeights?.ToImmutableArray(), Variables.Count);
    }

    public ValueArray<string> Variables { get; }
    public ValueArray<double> SelectionWeights { get; }

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
        var index = WeightSelection.SelectIndex(random, Variables.Count, SelectionWeights.AsSpan());
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
public sealed record SineSymbol() : BuiltInOperationSymbol("sin", OpCode.Sin);
public sealed record CosineSymbol() : BuiltInOperationSymbol("cos", OpCode.Cos);
public sealed record TangentSymbol() : BuiltInOperationSymbol("tan", OpCode.Tan);
public sealed record HyperbolicTangentSymbol() : BuiltInOperationSymbol("tanh", OpCode.Tanh);
public sealed record LogarithmSymbol() : BuiltInOperationSymbol("log", OpCode.Log);
public sealed record SquareRootSymbol() : BuiltInOperationSymbol("sqrt", OpCode.Sqrt);
public sealed record AbsoluteSymbol() : BuiltInOperationSymbol("abs", OpCode.Abs);
public sealed record SquareSymbol() : BuiltInOperationSymbol("square", OpCode.Square);
public sealed record CubeSymbol() : BuiltInOperationSymbol("cube", OpCode.Cube);
public sealed record CubeRootSymbol() : BuiltInOperationSymbol("cbrt", OpCode.CubeRoot);

public sealed record PowerSymbol() : BuiltInOperationSymbol("pow", OpCode.Power);
public sealed record RootSymbol() : BuiltInOperationSymbol("root", OpCode.Root);
public sealed record AnalyticQuotientSymbol() : BuiltInOperationSymbol("aq", OpCode.AnalyticQuotient);

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
    public static SineSymbol Sine { get; } = new();
    public static CosineSymbol Cosine { get; } = new();
    public static TangentSymbol Tangent { get; } = new();
    public static HyperbolicTangentSymbol HyperbolicTangent { get; } = new();
    public static LogarithmSymbol Logarithm { get; } = new();
    public static SquareRootSymbol SquareRoot { get; } = new();
    public static AbsoluteSymbol Absolute { get; } = new();
    public static SquareSymbol Square { get; } = new();
    public static CubeSymbol Cube { get; } = new();
    public static CubeRootSymbol CubeRoot { get; } = new();
    public static PowerSymbol Power { get; } = new();
    public static RootSymbol Root { get; } = new();
    public static AnalyticQuotientSymbol AnalyticQuotient { get; } = new();
    public static SigmoidSymbol Sigmoid { get; } = new();

    public static IReadOnlyList<OperationSymbol> MinimalOperations { get; } =
        [Addition, Subtraction, Multiplication, Division];

    public static IReadOnlyList<OperationSymbol> DefaultOperations { get; } =
        [Addition, Subtraction, Multiplication, Division, Exponential, Logarithm, SquareRoot, Square];

    public static IReadOnlyList<OperationSymbol> AllOperations { get; } =
    [
        Addition, Subtraction, Multiplication, Division,
        Negation, Exponential, Logarithm, SquareRoot,
        Sine, Cosine, Tangent, HyperbolicTangent,
        Absolute, Square, Cube, CubeRoot,
        Power, Root, AnalyticQuotient,
        Sigmoid
    ];

    public static EvolvableConstantSymbol Constant(IDistribution<double>? initialDistribution = null, NumericPerturbation? perturbation = null)
    {
        return new EvolvableConstantSymbol(initialDistribution ?? new UniformDoubleDistribution(-1.0, 1.0), perturbation ?? NumericPerturbation.Default);
    }

    public static FixedConstantSymbol FixedConstant(double value, string? displayName = null) => new(value, displayName);
    public static VariableSymbol Variable(IEnumerable<string> variables, IEnumerable<double>? selectionWeights = null) => new(variables, selectionWeights);
}
