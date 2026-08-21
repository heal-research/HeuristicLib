# Symbolic expressions

Symbolic expressions are useful when the candidate itself is an interpretable formula. HeuristicLib represents each formula as an immutable tree, supports safe structural edits and can format it for several target languages.

Use these APIs to build, edit and format expression candidates. Read [Symbolic regression](/guide/domains/symbolic-regression) to train expressions from data.

## Create an expression

`ExpressionTree` is the aggregate root of an immutable symbolic-expression genotype. It owns the root node and is the public entry point for edits.

The quickest way to obtain one by hand is the infix parser. During a search, a creator such as `RampedHalfAndHalfTreeCreator` produces them instead.

```csharp
var expression = InfixExpressionParser.Parse("x0 + 2 * sin(x1)");

Console.WriteLine($"length {expression.Length}   depth {expression.Depth}");
```

```
length 6   depth 4
```

Length counts nodes and depth counts levels, and both are what an `ExpressionTreeSearchSpace` bounds during a search.

## Structure and identity

- `ExpressionNode` represents immutable expression structure and payload. Nodes can be shared between trees or occur more than once in the same tree.
- `ExpressionPoint` identifies one node occurrence and its path within a particular tree. Use a point when selecting a mutation, crossover, or replacement location.
- `ExpressionTree` applies edits and returns a new tree. Unaffected nodes remain structurally shared with the original tree.

Node reference identity therefore does not identify a unique location. Obtain points from `ExpressionTree.RootPoint` and traverse those points when an operation must target a specific occurrence.

## Editing

Use the tree or a tree-bound point for public edits:

```csharp
var point = tree.RootPoint.Child(0);
var edited = tree.Replace(point, replacementNode);

// Equivalent convenience form.
var editedFromPoint = point.ReplaceWith(replacementNode);
```

`ExpressionTree.Replace` accepts either a replacement node or a replacement tree. `ReplaceMany` applies non-overlapping edits in one operation and rebuilds every affected ancestor at most once. `WithVariable` and `WithConstant` provide domain-specific tree edits.

Child-rebuilding methods on nodes are implementation details. Keeping those methods internal ensures that public edits retain tree ownership checks, occurrence semantics, and efficient structural sharing.

## Parsing infix expressions

`InfixExpressionParser` parses arithmetic operators, supported functions, variables, and numeric literals directly into an `ExpressionTree`:

```csharp
var expression = InfixExpressionParser.Parse("x0 + 2 * sin(x1)");
```

Bare numeric literals are fixed constants by default. Select evolvable constants when the input describes parameters that may later be optimized or perturbed:

```csharp
var expression = InfixExpressionParser.Parse(
    "x0 + 2",
    NumericLiteralInterpretation.Evolvable);
```

`fixed(value)` and `param(value)` override the selected default for an individual literal:

```csharp
var expression = InfixExpressionParser.Parse("fixed(2) * x0 + param(0.5)");
```

Use `TryParse` for expected user-input failures. `Parse` throws `FormatException`. Syntax failures include a one-based line and column without exposing the underlying parser library.

```csharp
if (!InfixExpressionParser.TryParse(text, out var expression))
{
    // Report invalid input.
}
```

Supplying an `ExpressionTreeSearchSpace` binds parsed nodes to its symbols and rejects unavailable variables, operations, ambiguous matches, and expressions outside its length or depth limits:

```csharp
var expression = InfixExpressionParser.Parse(text, searchSpace);
```

### Identifier escaping

Unquoted identifiers start with a letter or underscore and continue with letters, digits, or underscores. Enclose other variable and function names in backticks. Escape a literal backtick by doubling it:

````text
`sensor value`
`moving average`(x0)
`sensor ``A```
````

The infix formatter applies this escaping automatically. Backticks are specific to HeuristicLib's generic infix notation; C#, Python, and LaTeX formatters follow their target language instead.

## Formatting and round trips

The default infix output writes every numeric value as an ordinary literal. This is concise but intentionally loses the distinction between fixed and evolvable constants:

```csharp
string text = expression.ToInfixString();
```

Choose a constant notation when that distinction must survive an infix round trip:

```csharp
string marked = expression.ToInfixString(InfixConstantNotation.MarkAll);
ExpressionTree reparsed = InfixExpressionParser.Parse(marked);
```

Two less verbose round-trip pairings are available:

- `MarkParameters` with the parser's default `Fixed` interpretation.
- `MarkFixedConstants` with the parser's `Evolvable` interpretation.

The predefined formatter instances are available through `ExpressionFormatters`. Extension methods cover the common exports:

```csharp
string infix = expression.ToInfixString();
string csharp = expression.ToCSharpString();
string python = expression.ToPythonString();
string latex = expression.ToLatexString();
```

For `x0 + 2 * sin(x1)` those four produce:

```
infix   (x0 + (2 * sin(x1)))
csharp  (x0 + (2 * Math.Sin(x1)))
python  (x0 + (2 * math.sin(x1)))
latex   (\mathrm{x0} + (2 \cdot \sin\left(\mathrm{x1}\right)))
```

Each formatter targets its own language rather than translating the infix text, so `sin` becomes `Math.Sin` for C# and `math.sin` for Python. The output is fully parenthesized instead of relying on precedence rules, which keeps it correct when pasted into another language.

C#, Python, and LaTeX output describes the final mathematical expression and does not preserve whether a numeric value was evolvable. Those formats are exports, not inputs to `InfixExpressionParser`. Implement `IExpressionFormatter` or derive from `ExpressionFormatter` for another target syntax.
