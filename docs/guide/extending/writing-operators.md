# Writing operators

Create an operator when your candidate representation or problem needs a search step that the built-in operators do not provide. Most applications should configure or compose existing operators first.

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

Author an executor with fields for mutable counters, cursors and adaptation values. Each separate construction starts with independent fields. Share a configuration explicitly with `Sharing` when its executor must outlive a child scope. Instruments describe run-bound results; their definitions contain no live result.

The contributor guide covers [operator implementation internals](/contributing/architecture/operator-implementation), executors and repository analyzer rules.

Derive a composition from `WrappingOperator<TOperation, TCandidate>` or `MultiOperator<TOperation, TCandidate>`. Override the protected `CreateExecutor<TSearchSpace, TProblem, TSearchState>(scope)`, which builds the executor for the run's types, resolves children through its scope and reads run-bound instrument results through `scope.Results.Result(instrument)`. A fixed-signature leaf using the explicit executor base instead overrides `CreateExecutor(ResolutionScope scope)`.

A wrapper for one operation derives from `WrappingOperator<TOperation, TCandidate>` for that operation and wraps its typed executor. A wrapper for every operation stays generic in its operation and passes a struct `IInvocationWrapper` to the operation's `Wrap`. Place either with `Substitute`. See [Write wrappers](/guide/extending/writing-wrappers) for complete examples.

An operator you write is observed through its operation's `Invocations` source, such as `mutator.Invocations`, exactly like a built-in one. See [Observability and analysis](/guide/execution/observability-and-analysis).

The [stateful composition walkthrough](/guide/extending/operator-composition#a-complete-stateful-composition) implements a complete round-robin mutator and follows its cursor through a new inspection context and an independent root execution.

## Test the contract

Test the operator directly before placing it in an algorithm. Check these properties where they apply:

1. The result belongs to the search space.
2. The same input and random seed produce the same result.
3. The input candidate remains unchanged.
4. Concurrent calls do not share mutable state.

Use [operator composition](/guide/extending/operator-composition) when several existing operators already express the required behavior.
