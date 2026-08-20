# Problems

A problem is the boundary between your domain and the search algorithm. It tells HeuristicLib which candidates are valid, how to evaluate them and how to compare their objective values.

## Start with a function problem

When evaluation is a single function, `FuncProblem.Create` is the shortest useful definition:

```csharp
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

var searchSpace = new RealVectorSearchSpace(
    length: 3,
    minimum: [-10.0],
    maximum: [10.0]);

var problem = FuncProblem.Create(
    evaluateFunc: (RealVector candidate) => candidate.Sum(x => x * x),
    encoding: searchSpace,
    objective: SingleObjective.Minimize);
```

The candidate `[0, 0, 0]` has value `0` and is the global optimum of this sphere function.

## What belongs in a problem

Keep behavior that defines the optimization question in the problem:

- candidate evaluation
- domain data needed by evaluation
- the search space
- minimize or maximize directions

Keep search policy in the algorithm configuration. Population size, mutation strength and stopping criteria describe how to search, not what counts as a good answer.

## Built-in problems

The library includes benchmark and domain problems that are useful for learning, tests and comparisons. `TestFunctionProblem` wraps numerical benchmarks such as Rastrigin. Other namespaces cover traveling salesperson and data analysis tasks.

Built-in problems expose their search space, so creators can use the same bounds:

```csharp
var creator = new UniformDistributedCreator(problem.SearchSpace);
```

## Single and multiple objectives

Use `SingleObjective.Minimize` or `SingleObjective.Maximize` for one value. Multiobjective problems return several values and provide one direction for each. A multiobjective algorithm such as NSGA-II then works with dominance instead of reducing every tradeoff to one score.

See [Objective vectors and evaluated candidates](/guide/fundamentals/objectives) for comparison semantics.

## When to create a problem type

Create a dedicated problem type when evaluation needs named domain data, validation or reusable behavior. A domain type makes that contract visible and avoids closing over a large mutable object graph in a delegate.

[Model your own problem](/examples/custom-problem) builds one end to end, including the choice of representation and how to treat candidates that violate a constraint.

Evaluation should be safe to call more than once. Parallel algorithms and experiments may evaluate different candidates concurrently. If evaluation uses randomness, derive it from the run's explicit random source rather than hidden global state.

## Checklist

Before choosing an algorithm, verify that:

- the candidate type can express every acceptable answer
- the search space rejects or repairs invalid values
- evaluation always returns the expected number of objectives
- every objective has the correct minimize or maximize direction
- evaluation failures produce clear domain errors
- repeated evaluation is safe
