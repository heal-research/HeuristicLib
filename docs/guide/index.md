# Overview

HeuristicLib is a .NET library for building, running and studying heuristic optimization algorithms. It provides ready to use algorithms for common searches and typed building blocks for problems that need a custom approach.

Use it when an answer must be found among many possible candidates and checking a candidate is easier than deriving the best answer directly. Typical examples include scheduling, routing, parameter tuning and symbolic regression.

## The model in one sentence

An **algorithm** uses **operators** to explore the **search space** of a **problem** and yields **search states** that show its progress.

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

The parts stay separate. Compare algorithms on the same problem, swap a mutation operator or repeat a configuration with different random seeds.

## What is included

- Population based algorithms including genetic algorithms, evolution strategies and NSGA-II
- Local search algorithms including hill climbing
- Search spaces for real, integer, Boolean and permutation vectors
- Reusable creation, crossover, mutation and selection operators
- Single objective and multiobjective evaluation
- Repeatable experiments, parameter grids and analysis helpers
- Specialized support for machine learning, symbolic regression and symbolic expressions
- Source based Python integration through pythonnet

HeuristicLib targets .NET 10. The package is currently prerelease software, so expect APIs to evolve between versions.

## A typical workflow

1. Describe valid candidates with a search space.
2. Define how the problem evaluates a candidate.
3. Choose an algorithm and compatible operators.
4. Run with an explicit random seed.
5. Inspect the final result or stream intermediate states.
6. Repeat runs to compare configurations rather than trusting one stochastic result.

The [getting started guide](/guide/getting-started) builds a complete run. Read [core concepts](/guide/fundamentals/core-concepts) for the parts used by that example.

## Where to go next

- [Build your first optimizer](/guide/getting-started)
- [Learn the core concepts](/guide/fundamentals/core-concepts)
- [Define your own problem](/guide/fundamentals/problems)
- [Run repeatable experiments](/guide/execution/experiments)
