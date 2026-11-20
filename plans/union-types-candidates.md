# Union types in algorithm and operator configuration

Status: candidate inventory recorded on 2026-11-03. Resume when C# unions are released and supported by the repository's selected SDK and target frameworks. The developer requested these ideas be retained for future evaluation. No individual API, union type or implementation design is accepted by this document.

## Scope and decision status

Investigate algorithm and operator configuration first. Prioritize alternatives that answer the same user question, particularly an absolute count or a relative proportion. Domain specific packages and internal compiler results are deferred.

Examples are proposed configuration fragments, not implemented APIs or complete C# programs. Names introduced in examples are placeholders. Existing members and behavior are evidence, not requirements to preserve every current design choice.

Read this inventory with the [developer guidelines](../docs/contributing/developer-guidelines.md) and [glossary](../docs/guide/glossary.md). Architectural changes still require a concrete design and explicit acceptance before implementation.

## First candidates

### U1. Absolute count or relative proportion

Priority: first evaluation. Proposed alternatives: `int` and `double` in a named union.

| Setting            | Current locations                                                                          | Integer example                                       | Floating point example                                                           | Recommended reference count                                                           |
| ------------------ | ------------------------------------------------------------------------------------------ | ----------------------------------------------------- | -------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| `Elites`           | `GeneticAlgorithm`, `EliteSelector`, `ElitismReplacer` and experimental genetic algorithms | `Elites = 3` retains three candidates                 | `Elites = 0.05` retains five percent                                             | Requested output count, which is the target population size for the genetic algorithm |
| `TournamentSize`   | `TournamentSelector` and `ParetoCrowdingTournamentSelector`                                | `TournamentSize = 4` uses four participant draws      | `TournamentSize = 0.1` scales participant draws to ten percent of the population | Current input population count                                                        |
| `NumberOfChildren` | `EvolutionStrategy`                                                                        | `NumberOfChildren = 100` creates one hundred children | `NumberOfChildren = 2.0` creates twice the configured population size            | Configured `PopulationSize`                                                           |

Benefit: configurations retain their relative meaning when population sizes change, including across experiment grids. Elitism is the strongest starting point because users commonly want to preserve a share of the population.

Current code: [genetic algorithm](../src/HeuristicLib/Algorithms/Evolutionary/GeneticAlgorithm.cs), [elite selector](../src/HeuristicLib/Operators/Selectors/EliteSelector.cs), [elitism replacer](../src/HeuristicLib/Operators/Replacers/ElitismReplacer.cs), [tournament selector](../src/HeuristicLib/Operators/Selectors/TournamentSelector.cs), [Pareto tournament selector](../src/HeuristicLib/Operators/Selectors/ParetoCrowdingTournamentSelector.cs) and [evolution strategy](../src/HeuristicLib/Algorithms/Evolutionary/EvolutionStrategy.cs).

Decisions to resolve:

1. Confirm the reference count for each property. In particular, distinguish the requested output count from the available input population and the configured population size from the current population size.
2. Choose rounding and minimum counts. The initial recommendation is to round positive proportions upward and handle zero explicitly. Tournament sampling needs a valid participant count when selection is requested.
3. Set allowed ranges per setting. Elite proportions normally lie in `[0, 1]`, while offspring ratios above one are useful. Do not impose one global range on every floating point case.
4. Decide how fractional counts interact with existing capping, small populations, empty inputs, nonpositive integers and nonfinite floating point values.
5. Make `1` versus `1.0` explicit in documentation and usage specs. One is an absolute count and the other is the full reference count. Experiment grids must retain that distinction rather than widen every entry to `double`.
6. Decide whether one named union is coherent across these settings or whether some need a narrower contract. Avoid a general parameter framework without a demonstrated need.

Recommendation: retain configuration values as supplied and interpret them where the reference count is known. Keep operation executor arguments such as selection and replacement counts as integers unless a separate need justifies changing those contracts. Resolve population dependent values at the appropriate invocation rather than assuming the population size can never change.

`HillClimber.BatchSize` relative to `MaxNeighbors` is a possible later extension. It is lower priority because that denominator is a search allowance rather than an actual batch or population. A floating point `PopulationSize` has no useful reference count established by this discussion.

### U2. Exact application count or application probability

Priority: evaluate after U1. Proposed alternatives: an integer count and a floating point probability.

The [genetic algorithm](../src/HeuristicLib/Algorithms/Evolutionary/GeneticAlgorithm.cs) currently uses `MutationRate` as a probability. The [rate helpers](../src/HeuristicLib/Operators/Composition/ChoosePerItem.cs) apply mutation, crossover or refinement probabilistically.

Proposed fragments:

```csharp
MutationApplications = 5       // Select exactly five offspring for mutation
MutationApplications = 0.05    // Select each offspring with probability 0.05
```

An analogous composition could expose:

```csharp
mutator.AppliedTo(5)
mutator.AppliedTo(0.05)
```

Benefit: users can request a fixed amount of variation or independent probabilistic application through one setting. A probability produces a varying count and is not equivalent to choosing an exact fraction of the batch.

Decisions to resolve:

1. Confirm that the integer case means exactly the requested number of applications, not an expected number. Select distinct batch positions uniformly when an exact count is requested.
2. Define capping or rejection when the requested count exceeds the batch size, and define zero and negative counts.
3. State the unit per operation. Crossover operates on parent groups while mutation and refinement operate on candidates.
4. Define RNG use and result ordering so execution is reproducible and configuration composition remains observable in the usual way.
5. Choose names that honestly cover both cases. `MutationRate` and `AppliedAtRate` currently communicate probability, so accepting an integer there should not silently change the meaning of existing calls.

This is additional search behavior, not only shorter syntax. Keep the existing distinction between [algorithm owned rate logic and explicitly configured compositions](developer-backlog.md#internally-derived-configurations-as-selectable-sources). Do not turn a scalar branch into hidden operator configurations.

### U3. A stopping value or a terminator operator

Priority: evaluate with algorithm configuration. Proposed alternatives: an iteration count, a duration and a terminator operator, with the duration case considered separately if it makes the contract unclear.

Current [termination composition](../src/HeuristicLib/Algorithms/Control/StateTerminatedAlgorithm.cs) provides `TerminatedBy(operator)` and `TerminatedAfterIterations(count)`. The genetic algorithm also has both `MaximumGenerations` and `Terminator`.

Proposed examples:

```csharp
algorithm.TerminatedBy(1_000)
algorithm.TerminatedBy(TimeSpan.FromSeconds(30))
algorithm.TerminatedBy(customTerminator)
```

A single algorithm stopping setting could similarly accept a simple value or an operator.

Benefit: ordinary stopping limits remain concise while custom termination uses the same configuration input.

Decisions to resolve:

1. Preserve an explicit way to combine a generation limit with another stopping condition. A union represents alternatives and does not itself represent both conditions together.
2. Distinguish a limit checked before producing a state from a terminator checked after a produced state. Currently a zero generation limit can yield no states while an iteration terminator stops on the first checked state.
3. Define what a duration measures. Wall time and time spent advancing an algorithm stream are different, especially when a consumer pauses streaming.
4. Define unlimited execution without conflating it with an uninitialized union value.
5. Preserve the intended RNG placement and lifetime of custom terminators. Moving a terminator between an algorithm setting and an outer composition is not automatically behaviorally equivalent.

Recommendation: start by comparing the existing two convenience methods with a stored union setting. Method overloads may be sufficient if no configuration needs to retain the alternatives. Do not merge all stopping mechanisms solely to use unions.

### U4. A concrete configuration reference or a broader selection

Priority: low; current budgets need no union. Proposed alternatives: a configuration reference and a selection of several configurations.

Operator budgets take a reference for the common case, such as `algorithm.LimitedToInvocations(evaluator, 1_000)` on [operator count budgets](../src/HeuristicLib/Algorithms/Control/OperatorCountBudgetAlgorithm.cs) and `LimitedToDuration(evaluator, …)` on [operator duration budgets](../src/HeuristicLib/Algorithms/Control/OperatorDurationBudgetAlgorithm.cs). A broader selection is a placement: `LimitedToCount(maximum, (algorithm, counter) => algorithm.Substitute(…))` counts whatever the placement wraps, selected by reference, type or predicate, and states each wrapper's metric. Pointcuts and aspects no longer exist.

Reconsider a union only if a stored configuration property must hold either a reference or a selection. The placement is not stored, so today no property needs the alternatives. An untyped operator list as the target stays rejected (see the backlog's "Budget targets as an untyped operator list, and a metric-bound counter handle").

### U5. A mutator or a refiner for candidate postprocessing

Priority: evaluate after the numeric and targeting cases. Proposed alternatives: `IOperator<Mutation, TCandidate>` and `IOperator<Refinement, TCandidate>`.

[TransformedCreator](../src/HeuristicLib/Operators/Creators/TransformedCreator.cs) and [TransformedCrossover](../src/HeuristicLib/Operators/Crossovers/TransformedCrossover.cs) currently accept only a mutator. Their postprocessing slot could deliberately support either role.

Proposed examples:

```csharp
creator.TransformWith(mutator)
creator.TransformWith(refiner)
crossover.TransformWith(refiner)
```

Benefit: create or cross candidates and then transform them through either supported operation, without inventing another shared operator hierarchy.

Decisions to resolve:

1. Keep mutation and refinement as separate roles. This proposal applies only to a composition whose purpose makes both alternatives meaningful, not to every algorithm slot.
2. Define required batch size and ordering guarantees and how candidate contracts compose through each branch.
3. Preserve ordinary child resolution, wrappers, compatibility checks and role specific observations.
4. Handle an operator that supports both roles without relying on switch order to choose the operation. Callers may need an explicit role choice.
5. Compare a union property with two construction overloads that produce a clear existing composition. Matching method signatures alone do not establish a useful shared responsibility.

### U6. A built in strategy or a custom operator

Priority: secondary convenience. Proposed alternatives: `EvolutionStrategyType` and `IOperator<Replacement, TCandidate>`.

[EvolutionStrategy.Strategy](../src/HeuristicLib/Algorithms/Evolutionary/EvolutionStrategy.cs) currently chooses between `Comma` and `Plus` internally. A replacement setting could also accept a custom replacer.

Proposed fragments:

```csharp
Replacement = EvolutionStrategyType.Comma
Replacement = EvolutionStrategyType.Plus
Replacement = customReplacer
```

Benefit: standard strategies have a short configuration path while custom survivor selection uses the same setting.

Decisions to resolve:

1. Compare the union with a replacer interface alone and convenient predefined operator instances. The latter may be equally clear and require fewer concepts.
2. Specify the survivor count independently of the offspring count. Confirm the intended strategy behavior instead of assuming the current implementation defines every requirement.
3. Keep custom replacers visible as ordinary configured children. If presets remain algorithm owned behavior, state how their observation boundaries differ from explicitly configured operators.
4. Preserve custom operator support rather than enumerating only shipped replacer types.

### U7. Substitution or explicit configuration composition

Priority: coordinate with substitution, which replaced aspect attachment, before choosing an API. No particular union is recommended yet.

The original discussion considered whether a configuration input could accept an aspect or a configured wrapper. At the time of the initial code review these had different construction and graph responsibilities.

Substitution now places ordinary wrappers into the configuration, and aspects are gone. Equivalent selected boundaries, composition order and runtime dependencies must preserve execution behavior and state lifetime whether a wrapper is written into a configuration or placed by substitution. The attachment API remains open.

A conceptual input could distinguish applying a behavior directly at one boundary from applying it automatically at selected boundaries. A union might express these alternatives, but it does not itself establish scope, attachment order, data ownership or a public composition API.

Decisions to resolve:

1. First establish the explicit composition and substitution APIs.
2. Identify a concrete configuration input where callers need to store either alternative. Do not create a mixed list solely to accept unrelated objects through one method.
3. Compare a union with separate explicit composition and substitution entry points over the same execution behavior.
4. Define placement scope, ordering, duplicate attachment, internal observation boundaries and lifetime equivalence.

The earlier recommendation against combining the forms is not a rejection of substitution. Historical rejected implementations in the [backlog](developer-backlog.md#advice-expressed-as-wrapper-configurations) are evidence about their mechanisms, not a prohibition on the current contract.

## Deferred ideas from the earlier discussion

Retain these for completeness, but do not make them prerequisites for algorithm and operator configuration work.

### U8. A fixed numeric value or a distribution

Proposed alternatives: `double` and `IDistribution<double>`. Existing [numeric perturbation tests](../test/HeuristicLib.Tests/Encodings/SymbolicExpressions/NumericConfigurationTests.cs) use collapsed uniform distributions to express constants.

Example: replace `new AdditiveNumericPerturbation(new UniformDoubleDistribution(2, 2))` with `new AdditiveNumericPerturbation(2.0)`, while still accepting `new AdditiveNumericPerturbation(new NormalDoubleDistribution(0, 0.1))`.

Related candidates are multiplicative perturbations, resampling, initial values of evolvable constants and mixture components. A fixed initial value for an evolvable constant must remain distinct from a constant that cannot evolve. Compare the union with a constant distribution implementation and preserve defined random draw behavior.

### U9. A successful result or a typed failure

[NumericParameterFitter](../src/HeuristicLib/MachineLearning/Supervised/Regression/Symbolic/NumericParameterFitter.cs) returns a Boolean with nullable success and failure outputs and wraps existing failure types in another hierarchy. A named outcome union could contain `ExpressionTree`, `ExpressionCompilationFailure`, `VariableBindingFailure` or `LevenbergMarquardtFailure`.

[DifferentiableExpressionCompiler](../src/HeuristicLib/Encodings/SymbolicExpressions/AutomaticDifferentiation/DifferentiableExpressionCompiler.cs) has a similar success or failure result. Benefit: one returned outcome and exhaustive handling. Preserve the distinction between numerical failure, which can leave a candidate unchanged, and failures that indicate an invalid configuration.

### U10. A fixed normalization reference or a provider

[RelativeQualityEvaluator](../src/HeuristicLib/Operators/Evaluators/RelativeQualityEvaluator.cs) accepts an objective vector while [DynamicRelativeQualityEvaluator](../src/HeuristicLib.Experimental/Problems/Dynamic/Operators/DynamicRelativeQualityEvaluator.cs) uses a provider. A union could express either source of the reference.

This does not eliminate epoch refresh behavior or problem identity checks. Keep the core library independent of Experimental and compare the proposed input with the existing separate responsibilities before attempting consolidation.

### U11. Closed syntax alternatives

The parser's [SyntaxNode family](../src/HeuristicLib/Encodings/SymbolicExpressions/Authoring/InfixExpressionParser.cs) and [ExpressionDraft](../src/HeuristicLib/Encodings/SymbolicExpressions/Authoring/ExpressionDraft.cs) dispatch over known alternatives. A union could make handling exhaustive. Compare it with a closed class hierarchy where record equality, recursive structure or existing instance behavior make inheritance clearer.

## Checks when unions become available

1. Recheck the released [C# union reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union) and [feature specification](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-15.0/unions). Confirm SDK, target framework, interface case, conversion, generic inference and nullability behavior. Do not schedule this work against an assumed release version alone.
2. Select one candidate and present concrete user flows, alternatives and unresolved tradeoffs for acceptance. Preserve the distinction between accepting this inventory and accepting an implementation.
3. Verify configuration value equality, `with` behavior and retention of supplied values. Validate complete configurations at executor creation according to the developer guidelines.
4. Check union values inside experiment grids and construction helpers, especially generic inference and integer versus floating point case selection.
5. Integrate configured operator cases with `Fits`, ordinary scope resolution and [configuration child discovery](../src/HeuristicLib/Execution/SearchConfigurationValidation.cs). Current child discovery recognizes configuration values and enumerable elements directly, so a union containing a child is not automatically transparent to it.
6. Preserve consumer defined roles and implementations. Do not close an extensible operator contract over the list of shipped implementations.
7. Measure relevant costs before moving unions into frequently executed numeric or invocation paths. The documented generated representation boxes value cases and has default null contents. Recheck the released behavior and define how defaults are handled rather than assuming every union is initialized to a valid case.
8. Follow the [test suite guide](../test/README.md) and `AGENTS.md`. Establish API usage specs and focused behavior tests for accepted changes. Cover rounding, reference counts, exact versus probabilistic application, advice boundaries and relevant lifetime behavior. Update published examples only when the corresponding API is delivered.

## Resumption checklist

- [ ] Confirm released language and repository support.
- [ ] Evaluate U1 first.
- [ ] Decide whether U2 and U3 justify the additional behavior and configuration choices.
- [ ] Evaluate U5 and U6 against smaller overload or operator based alternatives.
- [ ] Revisit U7 against substitution and the sharing rules.
- [ ] Keep U8 through U11 deferred until the configuration work has been addressed.
- [ ] Record accepted designs and validation results here as candidates are implemented or declined.
