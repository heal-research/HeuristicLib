### Architectural Deep Dive: Simplifying HeuristicLib to its Core

---

### Executive Summary & Diagnosis

HeuristicLib contains brilliant ideas—most notably the **composition-first paradigm**, the **two-tier graph separation** (pure immutable data configuration vs. high-performance execution objects), and **streaming search execution**.

However, the architecture has fallen victim to **accidental complexity and framework over-generalization**:
1. **The AOP & Proxy Trap**: To solve cross-cutting concerns (counting evaluations, timing, probabilistic routing, caching), HeuristicLib built a full custom Aspect-Oriented Programming (AOP) engine (`Aspect`, `Advice`, `Pointcut`, `JoinPoint`, `IInvocationWrapper`, `WrappingExecutor`, `OperationInfo`). This forced every single operator role to implement complex static abstract methods and invocation structs.
2. **Generic Type Explosion ("Generic Hell")**: Generic parameter lists regularly span 5–8 parameters (`TCandidate, TSearchSpace, TProblem, TSearchState, TOperation, TSelf, TWrapper, TControl, TResult`). To manage this, dynamic reflection and runtime signature checkers (`ExecutionSignature`, `Fits()`) were introduced, effectively fighting .NET's type system.
3. **The "God Scope" Problem**: `ResolutionScope` simultaneously acts as an IoC container, a lifetime cache, a dynamic proxy weaver, a shared memory bus (`RunResources`), a results/metrics registry (`RunResults`), and a tree hierarchy manager.
4. **Meta-Algorithm Over-Wrapping**: Basic flow control (e.g., stopping after $N$ evaluations or 5 iterations) wraps algorithms in 4+ layers of wrapper executors (`StateTerminatedAlgorithm`, `OperatorCountBudgetAlgorithm`, `SharingAlgorithm`, etc.).
5. **Glossary Explosion**: 90 distinct concepts in the glossary have made the library difficult to navigate and maintain even for its primary author.

Drawing inspiration from **scikit-learn**, **PyTorch**, and clean **SOLID / KISS** design principles, this document presents a radical simplification: reducing the architecture to **5 Core Pillars** and a streamlined **Two-Tier Configuration/Execution Model**.

---

### Comparison: Scikit-Learn vs. HeuristicLib

| Dimension | Scikit-Learn | Current HeuristicLib | Proposed HeuristicLib Core |
| :--- | :--- | :--- | :--- |
| **Core Abstractions** | 3 (`Estimator`, `Predictor`, `Transformer`) | 90 glossary concepts, 15+ executor types | 5 (`Problem`, `Candidate`, `Operator`, `Algorithm`, `SearchState`) |
| **Composition** | Simple `Pipeline([('step', Est())])` | `AdvisedAlgorithm`, `SharingAlgorithm`, `ChildScopePolicy` | Standard OOP Decorators & Algorithm step pipelines |
| **Cross-Cutting Concerns** | Explicit parameters, callbacks, or metrics | Full AOP system (`Aspect`, `Pointcut`, `Advice`, `JoinPoint`) | Standard **Decorator Pattern** (`CountedEvaluator`, `RateRoutedMutator`) |
| **Telemetry / Stats** | Return dictionaries / objects / event listeners | `Instrument<T>`, `Accumulator`, `Trace`, `AnalysisSnapshot`, `Clock`, `Moment` | Standard `IProgress<TState>`, `RunStatistics`, or `System.Diagnostics.Metrics` |
| **Control Flow** | Iterators / Generators / Simple loop params | Deeply nested meta-algorithm wrapper chains | Standard `IEnumerable<TState>` / `IAsyncEnumerable<TState>` with LINQ (`.TakeWhile()`) |

---

### The 5 Core Concept Pillars of HeuristicLib

Every metaheuristic, evolutionary algorithm, local search, and machine learning routine in HeuristicLib can be expressed through **five fundamental concepts**:

```
                       ┌─────────────────────────┐
                       │         Problem         │
                       │ (SearchSpace, Evaluate) │
                       └────────────┬────────────┘
                                    │ Evaluates
                                    ▼
┌─────────────────────────┐   ┌───────────┐   ┌─────────────────────────┐
│        Candidate        │◄──┤ Algorithm │──►│       SearchState       │
│ (Genotype / Solution)   │   │  (Engine) │   │ (Population / Best / t) │
└─────────────────────────┘   └─────┬─────┘   └─────────────────────────┘
                                    │ Coordinates
                                    ▼
                       ┌─────────────────────────┐
                       │        Operators        │
                       │ Create, Mutate, Cross,  │
                       │     Select, Evaluate    │
                       └─────────────────────────┘
```

#### 1. `Problem<TCandidate, TFitness>`
Defines **what** is being optimized:
- `Evaluate(TCandidate candidate) -> TFitness` (and batch `Evaluate(ReadOnlySpan<TCandidate>)`)
- `Objective`: Minimization or Maximization (or Pareto multi-objective)
- `SearchSpace`: Defines boundaries, constraints, and valid domains

#### 2. `Candidate` & `EvaluatedCandidate<TCandidate, TFitness>`
The domain value being searched:
- Unevaluated: `TCandidate` (e.g., `RealVector`, `Permutation`, `SyntaxTree`)
- Evaluated: `EvaluatedCandidate<TCandidate, TFitness>(TCandidate Candidate, TFitness Fitness)`

#### 3. `Operator` Contracts (Pure OOP Functional Interfaces)
Defines **how** candidates are generated and manipulated. Clean, batch-oriented interfaces that natively support SIMD, multithreading, and GPU offloading without framework wrappers:
- `ICreator<TCandidate>`: `IReadOnlyList<TCandidate> Create(int count, IRandomNumberGenerator rng)`
- `IMutator<TCandidate>`: `IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator rng)`
- `ICrossover<TCandidate>`: `IReadOnlyList<TCandidate> Cross(IReadOnlyList<ParentPair<TCandidate>> pairs, IRandomNumberGenerator rng)`
- `ISelector<TCandidate, TFitness>`: `IReadOnlyList<EvaluatedCandidate<TCandidate, TFitness>> Select(IReadOnlyList<EvaluatedCandidate<TCandidate, TFitness>> population, int count, IRandomNumberGenerator rng)`

#### 4. `SearchState`
An immutable snapshot of the search at step $t$:
- e.g., `PopulationState<TCandidate, TFitness>` (current population, generation, best solution found, elapsed evaluations).

#### 5. `Algorithm` / `Optimizer`
Defines **how** the search coordinates operators across time:
- Step-based core loop:
  - `Initialize(problem, rng) -> TState`
  - `Step(previousState, problem, rng) -> TState`
- Execution API:
  - `Run(problem, ...)` $\rightarrow$ `OptimizationResult<TCandidate, TFitness>` (final answer + run stats)
  - `Stream(problem, ...)` $\rightarrow$ `IEnumerable<TState>` / `IAsyncEnumerable<TState>` (step-by-step progress, pause, resume, early stop)

---

### The Two-Tier Architecture: Configuration Graph vs. Execution Graph

You correctly noted that separating **Configuration** (pure immutable data records) and **Execution** (compiled, zero-overhead .NET objects) is a major architectural strength. The problem in the current codebase was how they were bridged.

Here is the clean, simplified pattern without `ResolutionScope` overhead, without dynamic signatures, and without reflection in hot loops.

```
┌────────────────────────────────────────────────────────┐
│                   CONFIGURATION GRAPH                  │
│       Pure C# Records (Immutable, JSON-Serializable)   │
│                                                        │
│  GeneticAlgorithmConfig {                              │
│      PopulationSize = 100,                             │
│      Crossover = new UniformCrossoverConfig(),         │
│      Mutator = new GaussianMutatorConfig { Rate=0.1 }, │
│      Selector = new TournamentSelectorConfig { K=2 }   │
│  }                                                     │
└───────────────────────────┬────────────────────────────┘
                            │
               .Compile(problem) / .Build()
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│                    EXECUTION GRAPH                     │
│       High-Performance Object Graph (No Lookups)       │
│                                                        │
│  GeneticAlgorithmExecutor {                            │
│      private readonly ICrossover crossover;            │
│      private readonly IMutator mutator;                │
│      private readonly ISelector selector;              │
│      private readonly IEvaluator evaluator;            │
│  }                                                     │
└────────────────────────────────────────────────────────┘
```

#### Tier 1: The Configuration Tier (Pure Data Records)
```csharp
// Pure data: No logic, no scopes, no reflection, 100% serializable via System.Text.Json / REST
public record GeneticAlgorithmConfig<TCandidate> : IAlgorithmConfig<TCandidate, PopulationState<TCandidate>>
{
    public int PopulationSize { get; init; } = 100;
    public double MutationRate { get; init; } = 0.05;
    public int Elites { get; init; } = 1;
    
    public required ICreatorConfig<TCandidate> Creator { get; init; }
    public required ICrossoverConfig<TCandidate> Crossover { get; init; }
    public required IMutatorConfig<TCandidate> Mutator { get; init; }
    public ISelectorConfig<TCandidate>? Selector { get; init; }
    
    public IAlgorithmExecutor<TCandidate, PopulationState<TCandidate>> Build(IProblem<TCandidate> problem)
    {
        var creator = Creator.Build(problem);
        var crossover = Crossover.Build(problem);
        var mutator = Mutator.Build(problem);
        var selector = Selector?.Build(problem) ?? new TournamentSelector<TCandidate>(tournamentSize: 2);
        
        // Wrap with RateRoutedMutator decorator if MutationRate < 1.0 (Plain OOP!)
        IMutator<TCandidate> effectiveMutator = MutationRate < 1.0 
            ? new RateRoutedMutator<TCandidate>(mutator, MutationRate)
            : mutator;

        return new GeneticAlgorithmExecutor<TCandidate>(
            creator, crossover, effectiveMutator, selector, PopulationSize, Elites);
    }
}
```

#### Tier 2: The Execution Tier (Direct, Zero-Overhead .NET Classes)
```csharp
public sealed class GeneticAlgorithmExecutor<TCandidate>(
    ICreator<TCandidate> creator,
    ICrossover<TCandidate> crossover,
    IMutator<TCandidate> mutator,
    ISelector<TCandidate> selector,
    int populationSize,
    int elites) : IAlgorithmExecutor<TCandidate, PopulationState<TCandidate>>
{
    public PopulationState<TCandidate> Initialize(IProblem<TCandidate> problem, IRandomNumberGenerator rng)
    {
        var candidates = creator.Create(populationSize, rng);
        var evaluated = problem.Evaluate(candidates);
        return new PopulationState<TCandidate>(evaluated, generation: 0);
    }

    public PopulationState<TCandidate> Step(PopulationState<TCandidate> previous, IProblem<TCandidate> problem, IRandomNumberGenerator rng)
    {
        // Direct method dispatch - ZERO dictionary lookups, ZERO boxing, ZERO reflection
        var parents = selector.Select(previous.Population, populationSize * 2, rng);
        var offspring = crossover.Cross(parents.ToPairs(), rng);
        var mutatedOffspring = mutator.Mutate(offspring, rng);
        var evaluatedOffspring = problem.Evaluate(mutatedOffspring);
        
        var nextPopulation = ElitismReplacer.Replace(
            previous.Population, evaluatedOffspring, problem.Objective, populationSize, elites);
            
        return new PopulationState<TCandidate>(nextPopulation, previous.Generation + 1);
    }
}
```

---

### Systems Assessment: What to Throw Out, Replace, or Keep

#### 1. What to Throw Out (Accidental Complexity)
- ❌ **Custom AOP Framework (`Aspect`, `Advice`, `Pointcut`, `JoinPoint`, `IInvocationWrapper`, `OperationInfo`)**:
  - *Why*: Over-engineered solution for what the standard **Decorator pattern** does in 5 lines of code. It imposed massive static abstract generic requirements on every operator in the codebase.
- ❌ **Dynamic Type Signatures (`ExecutionSignature`, `Fits()`, Signature Reconciliation)**:
  - *Why*: Runtime reflection and signature checking were only needed because configuration records had too many generic parameters. With simple typed `Build(problem)` methods, standard C# type safety works at compile time.
- ❌ **The Multi-Role Resolution Scope (`ResolutionScope`, `Node`, `Tree`, `ChildScopePolicy`)**:
  - *Why*: A single object was trying to be an IoC container, a cache, an AOP proxy weaver, and a results registry.
- ❌ **The Parallel Telemetry Metasystem (`Instrument<T>`, `Accumulator`, `AnalysisSnapshot`, `Clock`, `Moment`, `Time`, `ObservationPoint`)**:
  - *Why*: Created a parallel shadow domain for recording numbers. Replace with standard .NET telemetry (`IProgress<T>`, `System.Diagnostics.Metrics`, or return values).
- ❌ **Meta-Algorithm Wrapper Chains (`Turn`, `CycleAlgorithm`, `OperatorCountBudgetAlgorithm`, `OperatorDurationBudgetAlgorithm`, `StateTerminatedAlgorithm`)**:
  - *Why*: Creating wrapper algorithm records for basic loop termination or counting creates deep tree nesting and lifetime bugs.

#### 2. What to Replace and Simplify (Clean OOP Alternatives)

##### A. Replacing AOP Advice with the Decorator Pattern
If a user wants to count evaluations or route mutations probabilistically:
```csharp
// Standard Decorator Pattern (KISS & SOLID)
public sealed class CountingEvaluator<TCandidate>(IEvaluator<TCandidate> inner) : IEvaluator<TCandidate>
{
    private long evaluationCount;
    public long EvaluationCount => Interlocked.Read(ref evaluationCount);

    public IReadOnlyList<EvaluatedCandidate<TCandidate>> Evaluate(IReadOnlyList<TCandidate> candidates, IProblem<TCandidate> problem)
    {
        Interlocked.Add(ref evaluationCount, candidates.Count);
        return inner.Evaluate(candidates, problem);
    }
}
```

##### B. Replacing Meta-Algorithm Budgets with LINQ Streaming & Stopping Criteria
Instead of `OperatorCountBudgetAlgorithmExecutor`:
```csharp
// Clean LINQ consumption
var result = algorithm
    .Stream(problem, rng)
    .TakeWhile(state => state.Generation < 100)
    .TakeWhile(state => problem.EvaluationCount < 10_000)
    .TakeWhile(state => !cancellationToken.IsCancellationRequested)
    .Last();
```
Or for synchronous / one-liner execution:
```csharp
var result = algorithm.Run(problem, rng, stoppingCriteria: StoppingCriteria.Evaluations(10_000) | StoppingCriteria.Generations(100));
```

##### C. Replacing Scopes with Simple `ExecutionContext`
During execution, only pass what operators actually need:
```csharp
public readonly struct ExecutionContext(IRandomNumberGenerator rng, CancellationToken cancellationToken = default)
{
    public IRandomNumberGenerator Random => rng;
    public CancellationToken CancellationToken => cancellationToken;
}
```

#### 3. What to Keep & Strengthen (The Core Gems)
-  **Configuration Records as Pure Data**: Keep immutable C# records as the single source of truth for algorithm setup and serialization.
-  **High-Performance Execution Graph**: Keep compiled, linked execution classes with direct delegate/method calls.
-  **Batch-First Operator Signatures**: Keep batch processing (`IReadOnlyList<TCandidate>`) to allow vectorization and parallel evaluation.
-  **Rich Encodings & Problem Library**: Keep the real vector, permutation, integer, and symbolic regression domains.
-  **Scikit-Learn ML Alignment**: Keep the `IEstimator`, `IPredictor`, `IRegressor` interfaces aligned with scikit-learn conventions.

---

### The Simplified Architecture Blueprint

#### Proposed Simplified Class Hierarchy

```
HeuristicLib.Core
│
├── Problems/
│   ├── IProblem<TCandidate, TFitness>
│   ├── ISearchSpace<TCandidate>
│   └── IObjective<TFitness>
│
├── Operators/
│   ├── ICreator<TCandidate>
│   ├── IMutator<TCandidate>
│   ├── ICrossover<TCandidate>
│   ├── ISelector<TCandidate, TFitness>
│   ├── IReplacer<TCandidate, TFitness>
│   └── Decorators/ (Counting, RateRouting, Caching, Retrying)
│
├── Algorithms/
│   ├── IAlgorithm<TCandidate, TState>
│   ├── IAlgorithmExecutor<TCandidate, TState>
│   ├── GeneticAlgorithm<TCandidate>
│   ├── EvolutionStrategy<TCandidate>
│   ├── HillClimber<TCandidate>
│   └── NSGA2<TCandidate>
│
├── Execution/
│   ├── OptimizationResult<TCandidate, TFitness>
│   ├── StoppingCriteria
│   └── Random/ (IRandomNumberGenerator)
│
└── MachineLearning/ (Scikit-Learn style)
    ├── IEstimator<TData, TModel> (Fit)
    ├── IPredictor<TInput, TOutput> (Predict)
    └── IRegressor (Fit, Predict, Score)
```

#### Target API Experience (The "Scikit-Learn for Heuristics" Feel)

```csharp
// 1. Define the Problem
var problem = new TravelingSalesmanProblem(distanceMatrix);

// 2. Define the Algorithm via clean Data Records
var ga = new GeneticAlgorithmConfig<Permutation>
{
    PopulationSize = 100,
    Creator = new RandomPermutationCreatorConfig(),
    Crossover = new OrderCrossoverConfig(),
    Mutator = new InversionMutatorConfig(),
    Selector = new TournamentSelectorConfig { TournamentSize = 2 }
};

// 3. Simple execution: Run to completion
OptimizationResult<Permutation, double> result = ga.Run(
    problem, 
    rng: RandomNumberGenerator.Create(42),
    stopping: StoppingCriteria.Evaluations(50_000) | StoppingCriteria.Time(TimeSpan.FromSeconds(5))
);

Console.WriteLine($"Best tour distance: {result.BestFitness}");

// 4. Advanced execution: Stream iterations with LINQ
await foreach (var state in ga.StreamAsync(problem, rng))
{
    Console.WriteLine($"Gen {state.Generation}: Best = {state.BestCandidate.Fitness}");
    if (state.BestCandidate.Fitness < 100.0) break; // Early exit
}
```

---

### Migration & Implementation Roadmap

1. **Phase 1: Operator & Decorator Simplification**
   - Replace `IOperation<TSelf>`, `IInvocationWrapper`, and static abstract wrapping with direct OOP interfaces (`IMutator<T>`, `ICrossover<T>`, `IEvaluator<T>`).
   - Re-implement counting, rate routing, and caching as clean standard Decorator classes (`CountedEvaluator`, `RateRoutedMutator`).
2. **Phase 2: Configuration & Compilation Streamlining**
   - Eliminate `ResolutionScope`, `ExecutionSignature`, and signature reconciliation.
   - Implement `Build(IProblem)` on configuration records to directly instantiate the execution object graph.
3. **Phase 3: Control Flow & Stopping Criteria**
   - Replace `*BudgetAlgorithm` wrappers with a unified `StoppingCriteria` struct and native `IAsyncEnumerable<TState>` / `IEnumerable<TState>` streaming.
4. **Phase 4: Telemetry & Result Unification**
   - Remove `RunResults`, `Instrument<T>`, `Accumulator`, and `AnalysisSnapshot`.
   - Provide a unified `OptimizationResult<TCandidate, TFitness>` containing the best solution, final state, and a clean `RunStatistics` summary (time elapsed, iterations, evaluations).
5. **Phase 5: Glossary & Documentation Pruning**
   - Reduce the glossary from 90 terms down to ~20 fundamental, clear terms.

---

### Conclusion

By pruning accidental complexity—specifically custom AOP, proxy weaving, and generic signature reflection—HeuristicLib can achieve its original vision: **a high-performance, modular, composable, and REST-friendly optimization framework that is as simple to understand and use as scikit-learn.**

