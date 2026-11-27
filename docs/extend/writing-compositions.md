# Writing compositions

Write a composition when an operator coordinates child operators in a way no [built-in composition](/guide/fundamentals/operator-composition) does. A composition is an ordinary operator. It declares its children, resolves them when its executor is built and keeps private data in executor fields.

## Choose a base

| Base                                       | Use when                                                                       |
| ------------------------------------------ | ------------------------------------------------------------------------------ |
| `WrappingOperator<TOperation, TCandidate>` | The composition coordinates one child of the same operation                    |
| `MultiOperator<TOperation, TCandidate>`    | The composition coordinates an ordered collection of children of one operation |
| `Operator<TOperation, TCandidate>`         | The children play specific parts or perform different operations               |

`WrappingOperator` and `MultiOperator` serve every operation. They hold one child in `ChildOperator` or an ordered list in `ChildOperators`. They declare their children and forward candidate contracts, so validation reaches them. Their protected `CreateExecutor<TSearchSpace, TProblem, TSearchState>(scope)` validates the configuration and constructs an executor for the run's search-space, problem and state types on every build. Existing executors keep their original children when a new context is resolved.

The executor is a private nested `Executor` that derives from the role's executor base, such as `MutationExecutor<TCandidate>`. It holds the resolved children and mutable fields.

A generic wrapper of your own returns `TOperation.Wrap<TCandidate, TSearchState, TWrapper>(scope.ResolveOperator(ChildOperator), wrapper)` with a struct `IInvocationWrapper`, which works for every operation, including ones you [define](/extend/defining-operations). The wrapper can read the call's random number generator and proceed with another one or on another executor of the same operation.

## Declare children with a specific part

Derive from `Operator<TOperation, TCandidate>` when a composition coordinates different operations or declares children with specific domain responsibilities. `TransformedCreator` and `TransformedCrossover` follow this path because each coordinates both a source operator and a mutator.

A composition deriving from `Operator<TOperation, TCandidate>` declares its own children, because pre-run validation and substitution reach a child only through its declaration. `EliteSelector` declares the selector that fills the remaining places as a parameter and adds it to `GetParameters`. This configuration excerpt omits the executor body:

```csharp
public record EliteSelector<TCandidate> : Operator<Selection, TCandidate>
{
    public required IOperator<Selection, TCandidate> SelectorForRemaining { get; init; }
    public int Elites { get; init; } = 1;

    public static Parameter<EliteSelector<TCandidate>, IOperator<Selection, TCandidate>> SelectorForRemainingParameter { get; } =
        new(nameof(SelectorForRemaining), static selector => selector.SelectorForRemaining, static (selector, value) => selector with { SelectorForRemaining = value });

    public static Parameter<EliteSelector<TCandidate>, int> ElitesParameter { get; } =
        new(nameof(Elites), static selector => selector.Elites, static (selector, value) => selector with { Elites = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [SelectorForRemainingParameter, ElitesParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];

    protected override IExecutor CreateExecutor<TSearchSpace, TProblem, TSearchState>(
        ResolutionScope<TCandidate, TSearchSpace, TProblem, TSearchState> scope)
    {
        var selectorForRemaining = scope.Resolve(SelectorForRemaining);
        return new Executor(selectorForRemaining, Elites, scope.Problem);
    }

    // Executor derives from SelectionExecutor<TCandidate> and reads the bound problem's objective when ranking elites.
}
```

`EliteSelector` takes its elites first and asks `SelectorForRemaining` only for the places that are left, so calling that child `ChildSelector` would hide what it does. Wrapping it and adding a second, better-named property is not the fix. The two properties return the same object and a reader cannot tell which one to use. Declaring the child directly also lets the constructor fix how many children there are. `GenderSpecificSelector` needs exactly one `FemaleSelector` and one `MaleSelector`, which a multi base cannot express.

Like every composite, these name only the candidate. A child slot is `IOperator<Selection, TCandidate>`, and the run's search space and problem arrive as method type arguments on `CreateExecutor`. See [Choose an arity](/extend/writing-operators#choose-an-arity) for what that costs the author and why it costs the consumer nothing.

A declared child is checked against the run of its owner. A child resolved under other types states them on its parameter with `ChildExecution`. See [Resolve an operator under another objective](/extend/writing-operators#resolve-an-operator-under-another-objective) and [A composition answers for what it resolves](/extend/declaring-invariants#a-composition-answers-for-what-it-resolves).

## A complete stateful composition

This `RoundRobinMutator` sends each complete batch to the next child in a fixed order. Unlike weighted selection, it remembers which child should run next. Its cursor advances only after a successful child call, including a successful empty batch.

Use these imports in a C# project with implicit system usings enabled:

<<< ../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-imports{csharp}

<<< ../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-type{csharp}

The multi base snapshots the configured child collection. `CreateExecutor` validates it, resolves current children through the typed scope and constructs an executor with its own cursor field. Every separate construction starts at the first child.

The executor retains its contextual children and cursor. `offspring` is local to one invocation. This example expects sequential calls on one executor. Authors must coordinate any concurrent calls that mutate the same fields.

### Construct another observation context

Two distinct no-change mutators make the selection order visible through counters without changing candidate values:

<<< ../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-usage{csharp}

The results read through `observedScope.Results.Result(firstCalls)` and `observedScope.Results.Result(secondCalls)` have counts one and zero. The observed executor starts at the first child. The original executor continues to its second child with its original unobserved children.

An independent root prepares a fresh cursor:

<<< ../../test/HeuristicLib.Tests.Docs/Extending/StatefulCompositionDocumentationSpecs.cs#stateful-composition-independent{csharp}

`independentScope.Results.Result(independentFirstCalls).CurrentCount` is one. A fresh child scope builds its own executors unless they are [shared](/guide/fundamentals/configurations#sharing-executors). See [Execution resolution](/contributing/architecture/execution-resolution#results-and-construction).

## A concurrent cache wrapper

This executable example keeps an `EvaluationCache<int>` in its executor. `EvaluationCache<TKey>` uses MemoryCache for concurrent storage, key equality and eviction, with an optional entry count limit. The wrapper keeps its child executor private and evaluates outside cache storage operations. Lookup, evaluation and publication are separate operations, so overlapping misses may evaluate the same key more than once. The wrapper assumes immutable keys and deterministic evaluation with compatible semantics for every use of the shared executor: a cache cannot make incompatible problems, parameter settings or stochastic results interchangeable. Cache admission is not guaranteed at capacity, and eviction order is not part of the evaluator contract.

<<< ../../test/HeuristicLib.Tests.Docs/Extending/ResourceCachingDocumentationSpecs.cs#resource-cache-imports{csharp}

<<< ../../test/HeuristicLib.Tests.Docs/Extending/ResourceCachingDocumentationSpecs.cs#resource-cache-authoring{csharp}

The following usage resolves the wrapper twice in one scope, so both resolutions receive one executor and one cache, and calls it concurrently. Separate RNGs belong to the separate calls. `Substitute` places the count on the child evaluator in the configuration, so the shared executor counts through it.

<<< ../../test/HeuristicLib.Tests.Docs/Extending/ResourceCachingDocumentationSpecs.cs#resource-cache-usage{csharp}

Sharing does not synchronize calls. When the executor of an operator you write may be shared, use concurrency safe operations on its data or synchronize the complete invariant you need to preserve. Every participant must use the same synchronization object, and a lock must cover every path to the data. Prefer keeping synchronization inside the data structure itself. A cache with epoch metadata, for example, must coordinate invalidation and result publication with that epoch. Choose a simple lock, a read/write lock or concurrent storage according to those requirements, and avoid holding a shared lock across expensive child evaluation when validity can instead be checked before publication. Safe storage alone does not guarantee one evaluation per key or a strict shared evaluation budget. Child evaluators, key selectors and other dependencies must support the concurrency the author permits.

Executors have no disposal lifecycle, so keep only data that needs no deterministic cleanup in executor fields. `EvaluationCache<TKey>` uses MemoryCache with statistics and linked entry tracking disabled and without expiration tokens or callbacks, so it requires none.

## Next steps

- [Declare invariants](/extend/declaring-invariants) so validation knows what the composition keeps.
- [Write wrappers](/extend/writing-wrappers) for runtime policies around the executors of selected operators.
- [Configurations](/guide/fundamentals/configurations#sharing-executors) explains how `Sharing` keeps an executor, such as a cache, across child scopes.
