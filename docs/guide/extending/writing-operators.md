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

Assign it anywhere an `IMutator<Permutation>` is expected — the role contract names only the candidate, so an operator authored at any rung of the ladder fits:

```csharp
var algorithm = GeneticAlgorithm.For(problem)
    with
    {
        Mutator = new AdjacentSwapMutator(),
        MutationRate = 0.1
    };
```

Reduced arity bases such as `SingleCandidateMutator<Permutation>` omit inputs the operator does not use. Choose a search space or problem specific base when the operation needs those values.

## Keep execution state out of configuration

Operator records are reusable configuration. Do not store counters, caches or other changing run data in their properties.

Use a stateful base for ordinary mutable execution data. It prepares one state object per selected logical execution; descendant bindings can share ancestor state, while independent root scopes start fresh. Keep `CreateInitialState()` and the ordinary operation override. Use an explicit execution node when the operator owns child operators or more complex run resources.

The contributor guide covers [operator implementation internals](/contributing/architecture/operator-implementation), execution nodes and repository analyzer rules.

For a wrapping operator, override `CreateWrapperFactory` and return `WrapperExecutionFactory<TExecution>`; for a multi operator, override `CreateCompositeFactory` and return `CompositeExecutionFactory<TExecution>`. The execution type is the role interface shared by the children and result. Prepare counters or caches in that hook, then return a factory that receives the resolved child or ordered child array. The base resolves those children for each context. Keep persistent data outside the returned delegate so a child observation does not reset it.

## Test the contract

Test the operator directly before placing it in an algorithm. Check these properties where they apply:

1. The result belongs to the search space.
2. The same input and random seed produce the same result.
3. The input candidate remains unchanged.
4. Concurrent calls do not share mutable state.

Use [operator composition](/guide/extending/operator-composition) when several existing operators already express the required behavior.
