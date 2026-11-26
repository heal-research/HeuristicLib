# Writing operators

Create an operator when your candidate representation or problem needs a search step that the built-in operators do not provide. Most applications should configure or compose existing operators first. This page starts the extending path: [compose operators](/guide/extending/operator-composition), [write wrappers](/guide/extending/writing-wrappers), [write an algorithm](/guide/extending/writing-algorithms) and [write a meta-algorithm](/guide/extending/writing-meta-algorithms) build on it.

## Choose the operator role

Match the new operation to the role an algorithm already expects.

| Role      | Implement it when you need to                   |
| --------- | ----------------------------------------------- |
| Creator   | Produce initial candidates                      |
| Evaluator | Calculate objective vectors outside the problem |
| Selector  | Choose candidates from a population             |
| Crossover | Combine parent candidates                       |
| Mutator   | Change one candidate                            |
| Refiner   | Improve a candidate after variation             |
| Replacer  | Decide which candidates survive                 |

When no role fits, define a new operation; see [Define a new operation](/contributing/architecture/operator-implementation#define-a-new-operation).

## Implement a stateless operator

Use a single candidate base when each candidate can be processed independently. This adjacent swap mutation preserves a permutation while making a local change:

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

Assign it anywhere an `IOperator<Mutation, Permutation>` is expected — the slot names the candidate and operation without requiring run types, so an operator authored at any rung of the ladder fits:

```csharp
var algorithm = GeneticAlgorithm.For(problem)
    with
    {
        Mutator = new AdjacentSwapMutator(),
        MutationRate = 0.1
    };
```

Reduced arity bases such as `SingleCandidateMutator<Permutation>` omit inputs the operator does not use. Choose a search space or problem specific base when the operation needs those values.

## Keep private execution data out of configuration

Operator records are reusable configuration. Do not store counters, caches or other changing run data in their properties.

Author an executor for mutable counters, cursors and adaptation values. A leaf operator with an explicit executor overrides `CreateExecutor`, which receives a typed scope exposing its bound `Problem` and `SearchSpace`, and returns a private nested `Executor` that derives from the role's executor base, such as `MutationExecutor<TCandidate>`. The executor keeps what it reads, such as the problem or the search space, in its own constructor parameters, and its mutable data in fields. Each separate construction starts with independent fields. [Configurations](/guide/fundamentals/configurations#sharing-executors) explains how `Sharing` keeps one executor across child scopes.

An operator you write is observed through its operation's `Invocations` source, such as `mutator.Invocations`, exactly like a built-in one. See [Analysis of algorithm runs](/guide/execution/observability-and-analysis).

The contributor guide covers [operator implementation internals](/contributing/architecture/operator-implementation), explicit executors and the repository analyzer rules.

## Resolve an operator under another objective

A selector or a replacer ranks candidates by the objective of the problem it is bound to, and a run binds every operator to the run's problem. An algorithm that needs an operator to rank by another objective than its problem's does not pass the objective to the operator. It resolves the operator under a problem that has that objective.

The example algorithm creates a sample of candidates and keeps the sample's best candidate under a maximizing objective with the `BestSelector` it holds, which it resolves under a problem that maximizes. In the example's run, whose problem minimizes, that is the sample's worst candidate. The algorithm is written for real vectors in a `BoundedRealVectorSearchSpace`, because the signature it declares for its selector names the search space. Use these imports in a C# project with implicit system usings enabled:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/OperatorObjectiveDocumentationSpecs.cs#operator-objective-imports{csharp}

The delegating problem forwards the evaluation and the search space of the problem of the run and states the objective it was given. It forwards nothing else, so it does not preserve capability interfaces of the run's problem, such as a known optimum, and an operator that reads one of them cannot be resolved under it:

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/OperatorObjectiveDocumentationSpecs.cs#operator-objective-problem{csharp}

The algorithm's `CreateExecutor` creates that problem once and resolves the selector through a typed view of its scope. The selector parameter declares, with `ChildExecution`, that its child runs under the delegating problem instead of the problem of the run. Preflight validation reads that declaration, so it checks the selector against the problem it is resolved under. Without it, preflight asks the selector about the algorithm's own signature, and a selector written for the delegating problem, one that reads something only that problem has, is rejected before the run starts; a selector such as `BestSelector`, which accepts any problem, would hide the mismatch. State the same types the hook resolves the child under. The rest of `WorstOfSampleAlgorithm` is an ordinary [iterative algorithm](/guide/extending/writing-algorithms#implement-an-iterative-algorithm).

<<< ../../../test/HeuristicLib.Tests.Docs/Extending/OperatorObjectiveDocumentationSpecs.cs#operator-objective-resolution{csharp}

Four rules apply:

- **One problem object is the single source of the objective** for everything resolved under it: the operator, its child operators, refiners and observation records all read the objective of that problem.
- **Create the problem once per `CreateExecutor`**, not per call. A scope keeps one executor for each configuration and problem object, so every new problem object gets new executors that the scope retains. See [Sharing executors](/guide/fundamentals/configurations#sharing-executors).
- **The view is the same scope, not a child scope.** Executors resolved through it sit beside the executors of the run's problem, and sharing and run results behave as before.
- **Declare the signature of the child on its parameter.** The resolution is typed by the delegating problem, so the parameter's `ChildExecution` names the same types.

This binds an operator to one fixed objective. An objective that changes during a run is not an argument of an operator: see [an objective that changes during a run](/guide/extending/writing-algorithms#an-objective-that-changes-during-a-run).

## Test the contract

Test the operator directly before placing it in an algorithm. Check these properties where they apply:

1. The result belongs to the search space.
2. The same input and random seed produce the same result.
3. The input candidate remains unchanged.
4. Concurrent calls safely coordinate access to shared mutable execution data.

## Next steps

- [Compose operators](/guide/extending/operator-composition) when several existing operators already express the required behavior. Its [stateful composition walkthrough](/guide/extending/operator-composition#a-complete-stateful-composition) implements a complete round-robin mutator with a cursor in its executor.
- [Write wrappers](/guide/extending/writing-wrappers) for runtime policies around the executors of selected operators.
- [Write an algorithm](/guide/extending/writing-algorithms) when the state transition or control flow is new.
