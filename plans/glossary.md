# Glossary Plan

This plan defines the intended scope and working approach for a future HeuristicLib glossary.

The durable glossary should eventually live at `docs/glossary.md`, be linked from the documentation table of contents, and be referenced from `AGENTS.md` so AI agents are told up front to use the repository's canonical terminology. This plan exists first so we can agree on what the glossary is supposed to achieve before writing the glossary itself.

## Purpose

The glossary should define common terms that need a clear and consistent meaning across HeuristicLib.

It should be a user-facing reference for library users, advanced users, contributors, and AI agents. Its purpose is to make the language in the public API, documentation, examples, tests, and design discussions more precise.

The intended model is similar in spirit to the scikit-learn glossary: a central place for tacit and explicit terminology conventions, with links to deeper documentation rather than duplicated explanations.

## Goals

- Define important domain and API terms in one place.
- Clarify overloaded terms that are easy to misunderstand.
- Record canonical terms, accepted aliases, discouraged terms, and provisional names.
- Link to deeper documentation instead of duplicating full explanations.
- Help contributors and AI agents use the same language as the project.
- Surface naming problems when the current name does not clearly communicate the underlying concept.

## Non-goals

- The glossary is not an architecture decision record.
- The glossary is not a replacement for the user guide or API reference.
- The glossary is not a dump of every type, namespace, or implementation detail in the codebase.
- The glossary should not freeze weak terminology just because it exists in the current implementation.
- The glossary should not duplicate detailed explanations from topic pages such as algorithms, operators, execution, observability, or randomness.

## Audience

The glossary should be written for:

- users who need a short definition of an unfamiliar term
- advanced users who want to understand extensibility and architecture vocabulary
- contributors who need consistent naming when designing APIs, docs, examples, and tests
- AI agents that need a repository-local language contract before making changes

## Working Approach

The glossary should be built through an active terminology review, not through blind extraction from existing docs and code.

The review should use a grilling and domain-modeling style:

- challenge vague, overloaded, or inconsistent terms
- compare proposed definitions against existing docs, API usage specs, tests, and code
- discuss concrete examples and edge cases before accepting a term as canonical
- prefer names that communicate the underlying concept clearly to users
- mark unresolved names as provisional instead of hiding uncertainty
- create separate design notes or plans only when a naming question exposes a larger architectural decision

Unlike generic domain-modeling workflows, the durable outcome for this repository should be the user-facing `docs/glossary.md` page. HeuristicLib should not adopt a separate `CONTEXT.md` glossary convention for this work.

## Entry Status

Glossary entries may use these status labels when useful:

- `Canonical`: the preferred project term
- `Alias`: an accepted alternate term that points to a canonical term
- `Discouraged`: a term that should usually be avoided
- `Provisional`: a current term or candidate term that may change after further design discussion

Status labels should be optional for obvious entries. They are most useful when a term has aliases, naming risk, or unresolved design pressure.

## Entry Style

Each entry should be concise and opinionated:

- start with the preferred term as a heading
- define what the term means in HeuristicLib
- explain important boundaries or common confusions
- mention aliases or discouraged alternatives when relevant
- link to deeper docs when a full explanation belongs elsewhere

Example:

```md
## Search state

A value produced by an algorithm during execution that represents the observable optimization state at one point in a run.

A search state is not the same as an algorithm's private execution state. Search states are part of the public execution stream; private execution state exists to support the algorithm implementation.

See also: Algorithm, Run, Execution state
```

## Initial Term Areas

The first glossary pass should focus on terms that are central, overloaded, or important for API design:

- algorithms and runs
- operators and operator roles
- problems, objectives, objective values, and objective vectors
- genotypes, search spaces, solutions, and populations
- search states and private execution state
- reusable configuration objects and run-scoped runtime objects
- randomness and reproducibility
- observability, analyzers, and observations
- experimental APIs, scenarios, and API usage specs

## Source Material

The first audit should prioritize:

- `docs/core-concepts.md`
- `docs/getting-started.md`
- `docs/overview.md`
- `docs/algorithm.md`
- `docs/operators.md`
- `docs/problem.md`
- `docs/search-space.md`
- `docs/objectives-and-solutions.md`
- `docs/execution-model.md`
- `docs/execution-instances.md`
- `docs/observability-and-analysis.md`
- `docs/randomness.md`
- `test/HeuristicLib.Tests.ApiUsageSpecs`
- representative examples under `examples`

The glossary should also be checked against public type names in `src`, but code should not be treated as automatically canonical when a name is unclear or provisional.

## Naming Questions

When a term exposes an unclear concept boundary or a weak name, record it as an open naming question before changing docs or APIs.

Likely early naming questions include:

- whether `definition` should remain the preferred term or be replaced by `configuration`
- whether `execution instance` should remain the preferred term or be replaced by `runtime` or another term
- how to distinguish public `SearchState` values from private execution state
- whether `solution`, `solution candidate`, `individual`, and `candidate` should all remain in active use
- how to describe `Problem`, `Objective`, `Evaluator`, and `Solution` boundaries without circular definitions

These questions may be resolved inside the glossary when the answer is mainly terminological. If they imply API or architecture changes, they should move into a separate plan or design note.

## Candidate Term Inventory

This inventory is the starting point for discussion. It is not yet the glossary.

### Core Optimization Model

- algorithm
- problem
- objective
- objective direction
- objective value
- objective vector
- genotype
- search space
- solution
- population
- individual
- candidate
- solution candidate
- parent
- offspring

### Execution Model

- run
- execution
- algorithm definition
- configured object
- configuration
- execution instance
- execution state
- search state
- previous state
- initial state
- produced state
- streamed state
- completion
- structural completion
- early stopping
- continuation
- resume
- restart
- cancellation

### Algorithm and Operator Roles

- operator
- creator
- evaluator
- selector
- crossover
- mutator
- replacer
- terminator
- interceptor
- wrapping operator
- multi operator
- stateless operator
- single-solution operator
- meta algorithm
- budget
- budget unit
- iteration
- generation
- cycle
- step

### Observability and Analysis

- observation
- observer
- observable operator
- analyzer
- analysis
- analyzer result
- observation plan
- external sink
- counter
- duration
- observed boundary
- hook point
- instrumentation
- diagnostics

### Randomness and Reproducibility

- random number generator
- RNG
- seed
- random draw
- fork
- fork key
- random stream
- root RNG
- child RNG
- deterministic execution
- reproducibility
- stochastic evaluation

### Project and Documentation Terms

- experimental API
- scenario test
- API usage spec
- unit test
- example
- public API
- current API
- desired-state API

## Initial Naming Review Queue

The first glossary pass should explicitly discuss these term groups before writing final definitions:

1. `definition`, `configuration`, and `configured object`
   - Current docs use `definition` for reusable algorithm/operator objects, while user-facing examples often say "configure an algorithm".
   - The glossary should decide whether `configuration` is the clearer user-facing term and whether `definition` should remain an internal or transitional term.

2. `execution instance`, `runtime object`, and `execution state`
   - Current docs use `execution instance` for run-bound stateful objects and `execution state` for algorithm/operator private state containers.
   - The glossary must make this distinction clear if both terms remain active.

3. `search state`, `state`, `execution state`, and `algorithm state`
   - Public search states are streamed values.
   - Private execution state is implementation state.
   - The glossary should discourage unqualified "state" when it creates ambiguity.

4. `solution`, `candidate`, `solution candidate`, and `individual`
   - Current docs define `Solution` as genotype plus objective vector.
   - Some domain language uses candidate or individual more loosely.
   - The glossary should decide which terms are canonical and which are contextual aliases.

5. `iteration`, `generation`, `cycle`, `step`, and `yielded state`
   - Current docs warn that iteration is not a universal synonym.
   - The glossary should preserve budget-unit precision and discourage generic iteration language where it hides the actual counted unit.

6. `observable operator`, `observer`, `analyzer`, and `analysis`
   - Observable operators provide local callback hooks.
   - Analyzers are run-scoped analysis objects.
   - The glossary should make this boundary canonical because it affects API design.

7. `completion`, `early stopping`, `cancellation`, `resume`, and `continuation`
   - Current docs distinguish internal completion from external early stopping and immediate cancellation.
   - The glossary should define these terms before future lifecycle-result design work.

## Acceptance Criteria

The first glossary PR should be considered useful when it:

- adds `docs/glossary.md` with an initial curated set of important terms
- marks provisional terms honestly instead of over-stabilizing them
- links the glossary from `docs/toc.yml`
- references the glossary from `AGENTS.md`
- updates contradictory nearby docs only where necessary
- updates `docs/developer-backlog.md` to show that the glossary work has started
- leaves larger naming or API changes as explicit follow-up items instead of mixing them into the glossary PR

## Proposed First Pass

1. Audit existing docs and API usage specs for recurring terms.
2. Group candidate terms by concept area.
3. Discuss ambiguous or weak names before writing definitions.
4. Create `docs/glossary.md` with an initial curated set of stable entries.
5. Link the glossary from `docs/toc.yml`.
6. Reference the glossary from `AGENTS.md` so AI agents know to use the canonical terminology when editing code, docs, tests, examples, and plans.
7. Update nearby docs only where they contradict the chosen glossary language.
8. Update `docs/developer-backlog.md` to reflect that the glossary item has started.
