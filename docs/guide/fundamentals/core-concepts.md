# Core concepts

HeuristicLib separates the optimization problem from the search method. Reuse the same problem when testing or comparing algorithm configurations.

## The six parts of a run

| Concept                 | Question it answers                      | Example                                             |
| ----------------------- | ---------------------------------------- | --------------------------------------------------- |
| Candidate               | What does one possible answer look like? | A vector of four real numbers                       |
| Search space            | Which candidates are valid?              | Four values between `-5.12` and `5.12`              |
| Problem                 | How is a candidate evaluated?            | Calculate the Rastrigin function                    |
| Objective               | Which result is better?                  | A smaller function value                            |
| Algorithm and operators | How do we explore?                       | Genetic algorithm with crossover and mutation       |
| Search state            | What has happened so far?                | Population and objective values after generation 12 |

<div class="flow-chart" role="img" aria-label="A problem, algorithm configuration and random source are inputs to an algorithm run. The run yields search states.">
  <div class="flow-chart__inputs">
    <section class="flow-card">
      <h3>Problem</h3>
      <p>Search space</p>
      <p>Evaluation</p>
      <p>Objective</p>
    </section>
    <section class="flow-card">
      <h3>Algorithm configuration</h3>
      <p>Operators</p>
      <p>Population and budgets</p>
    </section>
    <section class="flow-card">
      <h3>Random source</h3>
      <p>Recorded root seed</p>
    </section>
  </div>
  <div class="flow-arrow" aria-hidden="true"><span>combine into</span><strong>↓</strong></div>
  <section class="flow-card flow-card--primary">
    <h3>Algorithm run</h3>
    <p>Fresh execution state</p>
  </section>
  <div class="flow-arrow" aria-hidden="true"><span>yields</span><strong>↓</strong></div>
  <section class="flow-card flow-card--output">
    <h3>Search states</h3>
    <p>Evaluated candidates and progress counters</p>
  </section>
</div>

## Candidate and search space

A candidate is one proposed solution. Its type represents the shape of the answer, such as `RealVector`, `IntegerVector`, `BoolVector` or `Permutation`.

The search space owns validity rules for that type. A `RealVectorSearchSpace` can specify length and bounds. Operators use that information when creating or modifying candidates.

Read [Search spaces](/guide/fundamentals/search-spaces) for the built-in choices.

## Problem and objective

A problem combines three facts:

- the search space
- the evaluation of a candidate
- the direction and structure of the objective

Evaluation produces an objective vector, even for a single objective problem. The objective then defines whether lower or higher values are better and how results are compared.

Read [Problems](/guide/fundamentals/problems) and [Objective vectors](/guide/fundamentals/objectives).

## Algorithm and operators

An algorithm owns the search process and its stopping budget. Operators perform focused steps within that process. A genetic algorithm usually needs operators to create candidates, select parents, cross parents and mutate offspring.

This split lets you change the variation strategy without implementing another generation loop. Type parameters and search space checks catch incompatible combinations early.

Read [Algorithms](/guide/fundamentals/algorithms) and [Operators](/guide/fundamentals/operators).

## Search states

Algorithms yield immutable snapshots of useful progress. A population based state contains evaluated candidates. An iterative state also reports counters such as completed iterations.

You can consume every state for progress reporting and analysis or ask only for the final state. Read [Search states](/guide/execution/search-states) and [Running algorithms](/guide/execution/running-algorithms).

## Randomness is an input

Stochastic algorithms receive an explicit random number generator. A recorded seed is part of an experiment configuration, just like the population size or mutation rate. It makes a surprising run reproducible.

Read [Reproducible randomness](/guide/execution/randomness).

## Important guarantees

- Search spaces define candidate validity.
- Problems own evaluation and objective semantics.
- Evaluated candidates keep a candidate together with its objective vector.
- Each call to `Stream` or `CompleteAsync` starts a fresh run.
- The same configuration and seed are intended to produce the same stochastic sequence.
- One run is evidence, not a reliable comparison of stochastic algorithms.
