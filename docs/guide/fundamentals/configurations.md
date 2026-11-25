# Configurations

An algorithm and its operators are **configurations**: immutable data that describe what to run. A run builds **executors** from them and holds all runtime state there ([Core concepts](/guide/fundamentals/core-concepts#configurations-and-executors)). This page covers what you can do with a configuration before it runs: read it, rewrite it and decide which executors a run shares.

## The configuration graph

A configuration holds child configurations. A genetic algorithm holds its creator, crossover, mutator, selector and evaluator. A cycle holds the algorithms it repeats. Together they form the **configuration graph** below the algorithm you pass to a run.

Identity in the graph is the object reference. Two `GaussianMutator` configurations with equal settings are equal by value, but they are different nodes: the graph holds the one you placed, and only that one. Not every configuration has value equality. A `Counter` or a `Duration` stands for one count or time of a run and compares by reference, so two counters are different counters even when you create them alike.

<<< ../../../test/HeuristicLib.Tests.Docs/Fundamentals/ConfigurationDocumentationSpecs.cs#configuration-graph{csharp}

Every configuration declares its settings and children as **parameters**. A parameter reports the child configurations it holds for a given owner with `GetChildren`. The graph consists of what the declared parameters reach, so a configuration that declares no parameters is a leaf. [Why the configuration declares its properties](/guide/extending/writing-algorithms#why-the-configuration-declares-its-properties) explains how authors declare them.

## Declared parameters

Each configuration type exposes its parameters as typed values, such as `GeneticAlgorithm<RealVector>.PopulationSizeParameter`. A parameter reads the current value with `Get` and creates a copy of the configuration with a replacement through `With`. The original stays unchanged. Because the type declares the parameter, no reflection is involved.

<<< ../../../test/HeuristicLib.Tests.Docs/Fundamentals/ConfigurationDocumentationSpecs.cs#configuration-parameters{csharp}

`GetParameters()` lists every declared parameter of a configuration. Each reports its `Name` and `ValueType` and returns its value for an owner with `GetValue`. Experiments use the same declared parameters when they vary a setting across runs ([Experiments](/guide/execution/experiments)).

## Configurations that hold delegates

By convention a configuration is data: it compares by value and can be serialized. A configuration you write may still hold a delegate or another live object, such as a callback into your host application. That is allowed, and if you never persist the configuration, you lose nothing you need. You do give up three things:

- The configuration can no longer be serialized, and neither can any graph that contains it.
- Its equality depends on the delegate. Two delegates are equal only when they have the same method and the same target. Two configurations holding the same delegate instance are equal, but two lambdas that capture separate state usually are not.
- The state the delegate captures is not part of the configuration. It belongs to you and survives across runs that reuse the configuration.

Library configurations hold delegates only where they need them, such as `Observing` callbacks and `Trace` functions, which run your code. [Developer guidelines § 4.8](/contributing/developer-guidelines#§-4-8-represent-configured-behavior-with-value-strategies) lists them.

## Rewriting a graph with `Substitute`

`Substitute` rewrites a configuration graph before it runs. It returns a new configuration with the selected configurations replaced and leaves the receiver unchanged. The result has the receiver's static type, so a replacement of the receiver itself must be assignable to that type. The role interface, such as `IOperator<Evaluation, RealVector>`, is the usual type for a receiver that a wrapper replaces, and a compatible base type also works. A wrapper does not fit a receiver typed by a concrete configuration it does not derive from, and `Substitute` throws `InvalidOperationException`. The result is an ordinary configuration, equal by value to the same graph written by hand, as far as its nodes have value equality. It is the configuration that runs, so run it like any other. Whether you can compare or store it depends on what the graph contains: a `Counter` compares by reference, and a node that holds a delegate cannot be persisted.

### Select by reference

Name the configuration you placed, together with a function that returns its replacement. The function receives the selected configuration. Create the replacement from it with `with`.

<<< ../../../test/HeuristicLib.Tests.Docs/Fundamentals/ConfigurationDocumentationSpecs.cs#configuration-substitute-by-reference{csharp}

A reference selects exactly that configuration. Naming a configuration that is not in the graph, such as an equal but distinct copy, throws `InvalidOperationException`.

### Select by type or predicate

Type the parameter of the function to select every configuration of that type, and add a predicate to select by its properties. Selection by type or predicate reaches all matches in the graph, wherever they sit. A selection that matches nothing changes nothing and returns the receiver itself.

<<< ../../../test/HeuristicLib.Tests.Docs/Fundamentals/ConfigurationDocumentationSpecs.cs#configuration-substitute-by-type{csharp}

Give the lambda parameter its type, as above, instead of passing the type as an explicit type argument of `Substitute`: explicit type arguments bind to the receiver of the extension. `Substitution.Of<TConfiguration>(...)` takes them directly and creates a substitution you can pass to `Substitute`.

### What `Substitute` guarantees

- `Substitute` reaches the children that parameters declare. A configuration that a delegate creates while running is out of reach.
- Pass several substitutions to one call to apply them together, in the order given. One call resolves every reference against the graph it received. When an earlier substitution wraps its target and keeps it inside the wrapper, a later substitution that names the same target places its wrapper closer to the target, inside the earlier one. When an earlier substitution replaces its target outright, as the `with` copies in the examples above do, the target is no longer in the graph, and a later reference to it in the same call matches nothing and throws. After the groups in the call are expanded, one substitution instance that appears several times applies once, at its last position.
- Separate calls carry no memory of each other, and a substitution passed to two calls applies in both. A reference to a configuration that an earlier call replaced or copied no longer matches in a later call. An earlier call copies every ancestor of what it changed, so take a reference to such an ancestor from the current graph, for example from the configuration the earlier call returned.
- A replacement must fit the slot of the configuration it replaces. Otherwise `Substitute` throws `InvalidOperationException`.
- A substitution is never stored. It is applied once and only its result exists, so persist the configuration it produced. A node of that result that holds a delegate, such as a `Trace` function or an `Observing` callback, cannot be persisted. The state a callback captures belongs to you and survives reuse of the configuration across runs.

[Operator composition](/guide/extending/operator-composition) and [Writing wrappers](/guide/extending/writing-wrappers) show how authors write substitutions that wrap operators, and [Observability and analysis](/guide/execution/observability-and-analysis) shows the counters, clocks and analyzers that run through them.

## Substituting for one run

`CreateRun` accepts a list of substitutions. It applies them to the algorithm in one `Substitute` call, so the rules above hold for the list, and it validates the result by default ([Invariants and validation](/guide/fundamentals/invariants-and-validation#when-validation-happens)). Name the targets of the list from the configuration you pass to `CreateRun`: when a budget or an earlier `Substitute` call produced it, take them from that result, not from the algorithm it was made from. The run uses the substituted configuration, exposed as `run.Algorithm`. The algorithm you passed is unchanged, so the substitutions apply to that run only.

<<< ../../../test/HeuristicLib.Tests.Docs/Fundamentals/ConfigurationDocumentationSpecs.cs#configuration-substitute-for-one-run{csharp}

`evaluations` is a `Counter` configuration: it holds no count. The run creates a `CountAccumulator` for it and owns that accumulator as a run result, which is why the example reads the count from `run.Result(evaluations)`. Use `Substitute` directly when the instrumented configuration should be kept, and the list of `CreateRun` when it should not. [Running algorithms](/guide/execution/running-algorithms) covers creating, streaming and completing runs.

## Sharing executors

A run resolves configurations to executors within a scope. Within one scope an executor is bound per configuration reference and problem reference: asking again for the same configuration and problem returns the same executor, and a different problem object gets an executor of its own. A child scope, such as one turn of a cycle or one stage of a pipeline, builds its own executors instead of reusing those of the scope above. Without `Sharing`, every turn therefore starts with fresh private state, such as the iteration count of a terminator.

`Sharing` marks configurations that the child scopes below it use through one executor. Place it on the algorithm that encloses every use and name the configurations by reference. The nearest scope that shares a configuration owns its executor, and every request for it from that scope and from the scopes below receives that executor. Equal but distinct configurations keep separate executors. A shared executor keeps its private state and its resolved children across those scopes, and each run builds new ones. Its own children stay its own unless you share them too.

The example runs three turns of an inner algorithm that stops after three iterations. Without `Sharing`, every turn counts its own three iterations. Sharing the terminator carries its count across the turns, so the later turns stop after a single iteration each.

<<< ../../../test/HeuristicLib.Tests.Docs/Fundamentals/ConfigurationDocumentationSpecs.cs#configuration-sharing{csharp}

Counters and durations follow the same rule with one difference. The accumulators they resolve to are run results, so without `Sharing` a `Counter` or `Duration` normally resolves to one root accumulator for the whole run, whichever scope asks, and the count adds up across turns. Sharing it gives the sharing scope an accumulator of its own. A budget does this to restart its count each time it is built, as in every turn of a cycle. `run.Result(counter)` and clocks built from the counter still read the root's accumulator, which can differ from the one the sharing scope counts into. [Observability and analysis](/guide/execution/observability-and-analysis#count-time-and-observe-an-operator) shows counters and durations in use.

Share only what should persist. Sharing a cached evaluator of a cycle, for example, keeps the cache across turns while terminators and budgets restart. `Sharing` does not synchronize concurrent use. [Writing meta-algorithms](/guide/extending/writing-meta-algorithms) and [execution resolution](/contributing/architecture/execution-resolution#shared-configurations) describe the scope rules and the author's responsibilities.

## Next steps

- [Invariants and validation](/guide/fundamentals/invariants-and-validation) continues the Fundamentals path with what a run checks before it starts.
- [Running algorithms](/guide/execution/running-algorithms) and [Experiments](/guide/execution/experiments) use configurations in runs and in comparisons.
- [Operator composition](/guide/extending/operator-composition) and [Writing wrappers](/guide/extending/writing-wrappers) show how to author configurations that wrap and compose others.
- The [glossary](/guide/glossary) defines configuration, configuration graph, parameter, substitution and shared configuration.
