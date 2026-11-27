# Writing operators

Write an operator when your representation or problem needs a search step the built-in operators lack. Configure or [compose](/guide/fundamentals/operator-composition) existing operators first.

An operator configuration is a reusable description. Its executor does the work and owns the mutable fields.

## Choose the operator role

Pick the role an algorithm already expects.

| Role        | Implement it when you need to                   |
| ----------- | ----------------------------------------------- |
| Creator     | Produce initial candidates                      |
| Evaluator   | Calculate objective vectors outside the problem |
| Selector    | Choose candidates from a population             |
| Crossover   | Combine parent candidates                       |
| Mutator     | Change one candidate                            |
| Refiner     | Improve a candidate after variation             |
| Replacer    | Decide which candidates survive                 |
| Interceptor | Transform a produced search state               |
| Terminator  | Decide when an algorithm stops                  |

When no role fits, [define a new operation](/extend/defining-operations).

## Choose an authoring path

| Path              | Typical base              | Use when                                                                                     |
| ----------------- | ------------------------- | -------------------------------------------------------------------------------------------- |
| Stateless         | `StatelessEvaluator<...>` | The operation needs configuration values but no mutable execution data                       |
| Explicit executor | `Evaluator<...>`          | The operator resolves child operators or needs complete control over its execution structure |

Every role has both paths. The stateless bases derive from the unprefixed role base. An explicit executor derives from the unprefixed base directly.

Some roles add a single-candidate base for operations that treat each item of a batch independently: `SingleCandidateMutator<...>`, `SingleCandidateCrossover<...>`, `SingleCandidateCreator<...>`, `SingleCandidateRefiner<...>` and `SingleCandidateEvaluator<...>`. Implement the method for one item, such as `MutateCandidate` or `CrossParents`. The inherited batch operation handles batching and forks a random number generator per item. Set `Concurrency` to run the items concurrently. Each generator is forked from the item's batch position, so the result does not depend on the setting.

## Implement a stateless operator

This mutator swaps two adjacent elements, so its result stays a permutation:

```csharp
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Random;

public sealed record AdjacentSwapMutator : SingleCandidateMutator<Permutation>
{
    public override Permutation MutateCandidate(
        Permutation parent,
        IRandomNumberGenerator random)
    {
        if (parent.Count < 2)
            return parent;

        var index = random.NextInt(parent.Count - 1);
        return parent.Swap(index, index + 1);
    }
}
```

Assign it anywhere an `IOperator<Mutation, Permutation>` is expected:

```csharp
var algorithm = GeneticAlgorithm.For(problem)
    with
    {
        Mutator = new AdjacentSwapMutator(),
        MutationRate = 0.1
    };
```

A stateless operation receives what it reads as arguments. That is the search space and problem at the arities that name them, and the objective for selection and replacement. Its configuration values must not change during execution. This evaluator reads its search space and problem:

```csharp
private sealed record FirstValueEvaluator
    : StatelessEvaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    public override IReadOnlyList<ObjectiveVector> Evaluate(
        IReadOnlyList<RealVector> candidates,
        IRandomNumberGenerator random,
        BoundedRealVectorSearchSpace searchSpace,
        TestFunctionProblem problem)
    {
        return candidates.Select(candidate => new ObjectiveVector(candidate[0])).ToArray();
    }
}
```

## Choose an arity

Each role base comes in three arities. Name only the types the operation reads:

| Declared as                                                  | Operation receives                    | Use when                                       |
| ------------------------------------------------------------ | ------------------------------------- | ---------------------------------------------- |
| `SingleCandidateMutator<TCandidate, TSearchSpace, TProblem>` | parent, random, search space, problem | The operation depends on the problem           |
| `SingleCandidateMutator<TCandidate, TSearchSpace>`           | parent, random, search space          | The operation depends on the search space only |
| `SingleCandidateMutator<TCandidate>`                         | parent, random                        | The operation depends on the candidate only    |

The arity changes what the author declares, not where the operator fits. **A slot names only the operation and the candidate.** `AdjacentSwapMutator` fills any `IOperator<Mutation, Permutation>` slot, and so does a mutator written for one specific problem. A run with other types refuses that one before it starts, see [Operator type compatibility](/guide/fundamentals/invariants-and-validation#the-other-question-was-it-written-for-this-run-at-all).

Interceptor and Terminator keep `TSearchState` at every arity, because they transform or inspect a search state. `Terminator<TCandidate>` serves conditions that read no input at all, such as elapsed time or cancellation.

## Keep private execution data in an executor

Never store counters, caches or other run data in configuration properties. Put them in an executor.

An operator with an explicit executor overrides `CreateExecutor`. The hook receives a typed scope with the bound `Problem` and `SearchSpace`, resolves the declared children and returns a private nested `Executor`. The executor derives from the role's executor base, such as `EvaluationExecutor<TCandidate>`. Pass it what it reads through its constructor and keep mutable data in its fields:

```csharp
public sealed partial record ForwardingEvaluator(IOperator<Evaluation, RealVector> Inner)
    : Evaluator<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem>
{
    protected override IEvaluationExecutor<RealVector> CreateExecutor<TSearchState>(ResolutionScope<RealVector, BoundedRealVectorSearchSpace, TestFunctionProblem, TSearchState> scope)
    {
        var inner = scope.Resolve(Inner);
        return new Executor(inner);
    }

    private sealed class Executor(IEvaluationExecutor<RealVector> inner)
        : EvaluationExecutor<RealVector>
    {
        public override IReadOnlyList<ObjectiveVector> Evaluate(
            IReadOnlyList<RealVector> candidates,
            IRandomNumberGenerator random)
        {
            return inner.Evaluate(candidates, random);
        }
    }
}
```

::: details Parameter declarations

```csharp
public sealed partial record ForwardingEvaluator
{
    public static Parameter<ForwardingEvaluator, IOperator<Evaluation, RealVector>> InnerParameter { get; } =
        new(nameof(Inner), static evaluator => evaluator.Inner, static (evaluator, value) => evaluator with { Inner = value });

    public static IReadOnlyList<IParameter> Parameters { get; } = [InnerParameter];

    public override IReadOnlyList<IParameter> GetParameters() => [.. base.GetParameters(), .. Parameters];
}
```

:::

The child is declared as a parameter, so validation and substitution reach it. Each construction starts with fresh fields. Resolving the same configuration and problem again in one scope reuses the executor. To share fields such as a cache, users share the configuration with `Sharing`, see [Sharing executors](/guide/fundamentals/configurations#sharing-executors). Make data that is shared on its own a separate node. Executors are never disposed, so the caller owns any dependency that needs cleanup.

Your operator is observed through `Invocations`, such as `mutator.Invocations`, like a built-in one. See [Analysis of algorithm runs](/guide/execution/observability-and-analysis).

## Adaptable parameters

To read a value that changes during the run, resolve an `IConfigurationNode<IAdaptation>` child in `CreateExecutor` and apply it to the configured base value. `GaussianMutator` does this for `MutationStrength`. [Adaptable parameters](/guide/fundamentals/operator-composition#adaptable-parameters) shows the wiring.

## Resolve an operator under another objective

A selector or a replacer ranks candidates by the objective of the problem it is bound to, and a run binds every operator to the run's problem. An algorithm that needs an operator to rank by another objective than its problem's does not pass the objective to the operator. It resolves the operator under a problem that has that objective.

The example algorithm creates a sample of candidates and keeps the sample's best candidate under a maximizing objective with the `BestSelector` it holds, which it resolves under a problem that maximizes. In the example's run, whose problem minimizes, that is the sample's worst candidate. The algorithm is written for real vectors in a `BoundedRealVectorSearchSpace`, because the signature it declares for its selector names the search space. Use these imports in a C# project with implicit system usings enabled:

<<< ../../test/HeuristicLib.Tests.Docs/Extending/OperatorObjectiveDocumentationSpecs.cs#operator-objective-imports{csharp}

The delegating problem forwards the evaluation and the search space of the problem of the run and states the objective it was given. It forwards nothing else, so it does not preserve capability interfaces of the run's problem, such as a known optimum, and an operator that reads one of them cannot be resolved under it:

<<< ../../test/HeuristicLib.Tests.Docs/Extending/OperatorObjectiveDocumentationSpecs.cs#operator-objective-problem{csharp}

The algorithm's `CreateExecutor` creates that problem once and resolves the selector through a typed view of its scope. The selector parameter declares, with `ChildExecution`, that its child runs under the delegating problem instead of the problem of the run. Preflight validation reads that declaration, so it checks the selector against the problem it is resolved under. Without it, preflight asks the selector about the algorithm's own signature, and a selector written for the delegating problem, one that reads something only that problem has, is rejected before the run starts. A selector such as `BestSelector`, which accepts any problem, would hide the mismatch. State the same types the hook resolves the child under. The rest of `WorstOfSampleAlgorithm` is an ordinary [iterative algorithm](/extend/writing-algorithms#implement-an-iterative-algorithm).

<<< ../../test/HeuristicLib.Tests.Docs/Extending/OperatorObjectiveDocumentationSpecs.cs#operator-objective-resolution{csharp}

Four rules apply:

- **One problem object is the single source of the objective** for everything resolved under it: the operator, its child operators, refiners and observation records all read the objective of that problem.
- **Create the problem once per `CreateExecutor`**, not per call. A scope keeps one executor for each configuration and problem object, so every new problem object gets new executors that the scope retains. See [Sharing executors](/guide/fundamentals/configurations#sharing-executors).
- **The view is the same scope, not a child scope.** Executors resolved through it sit beside the executors of the run's problem, and sharing and run results behave as before.
- **Declare the signature of the child on its parameter.** The resolution is typed by the delegating problem, so the parameter's `ChildExecution` names the same types.

This binds an operator to one fixed objective. An objective that changes during a run is not an argument of an operator: see [an objective that changes during a run](/extend/writing-algorithms#an-objective-that-changes-during-a-run).

## Test the contract

Test the operator directly before placing it in an algorithm. Check these properties where they apply:

1. The result belongs to the search space.
2. The same input and random seed produce the same result.
3. The input candidate remains unchanged.
4. Concurrent calls safely coordinate access to shared mutable execution data.

An operator that keeps or requires invariants [declares them](/extend/declaring-invariants). Test that the declaration holds.

## Next steps

- [Write a composition](/extend/writing-compositions) when the operator coordinates child operators. Its [stateful composition](/extend/writing-compositions#a-complete-stateful-composition) implements a complete round-robin mutator with a cursor in its executor.
- [Write wrappers](/extend/writing-wrappers) for runtime policies around the executors of selected operators.
- [Write an algorithm](/extend/writing-algorithms) when the state transition or control flow is new.
- [Operator implementation](/contributing/architecture/operator-implementation) states the rules the library's own operators follow.
