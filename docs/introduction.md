# Introduction


### Immutability

Most objects are immutable.
E.g. a Permutation is immutable. To create a changed Permutation, you have to create a new one.


### Config vs Instance

`Algorithms` and `Operators` have a "Configuration" and an "Instance" part.

The configuration has parameters that a user typically would want to supply.
For example, the `GeneticAlgorithm` (configuration) has a `PopulationSize` or a `Crossover` parameter.

Algorithms and Operator "configurations" are immutable records.
They should be lightweight and should be easily serializable.
Therefore, Algorithm and Operator "configurations" should only contain primitive types or other Algorithm or Operator "configuration".

The "Instances" of an Algorithm or Operator are executable objects.
They are created from a "configuration."
E.g. from a `GeneticAlgorithm` you can create a `GeneticAlgorithmInstance`.

The "instances" are usually read-only but can have an internal state which can change when the algorithm or operator is executed.

"Instances" are generally not serializable.

Usually, when an Algorithm instance is created, it creates operator instances from the corresponding operator configurations in the algorithms configuration.
For example, the `GeneticAlgorithm` having a Crossover, a CrossoverInstance is created when a `GeneticAlgorithmInstance` is created.

Usually, a user does not need to know about the "instances."
Algorithms also offer an Execution method that simply creates an instance from itself and executes it.

### Algorithm State-Execution Model

Algorithms are "state-transformers."
I.e., State in -> state out.

If no input state is given, the algorithm does its initialization.

Iterative algorithms are executing state transformations in a loop.

States should be immutable and serializable.

States of one algorithm can be fed into another algorithm.
For example, execute 10 iterations on algorithm "a" and then feed the state into algorithm "b".
Of course, this only works if the state types themselves are compatible.

### Operator Model

Operators are operating on `Genotypes`.

They typically get some `Genotype` as input and return a `Genotype` as output.
E.g., a Crossover takes two `Genotype` as input and returns a new `Genotype` as output.

Additionally, an operator has a `SearchSpace`.

### SearchSpace Model

The `SearchSpace` describes the possible values of the `Genotype`.
I.e., this is the old HeuristicLab "Encoding."

SearchSpaces only describe immutable "Ranges."
E.g., a `PermutationSearchSpace` is defined by its `Length`.
E.g., a `RealVectorSearchSpace` is defined by `Length` and its `LowerBound` and `UpperBound`.

SearchSpaces can be related by being subspaces.
E.g., a `RealVectorSpace` with `Length=1` and `LowerBound=2` and `UpperBound=5` is a subspace of a `RealVectorSpace` with `Length=1` and `LowerBound=0` and `UpperBound=10`.

SearchSpaces can be converted.
E.g., a `PermutationSearchSpace` with `Length=4` can be converted to a `IntegerVectorSpace` with `Length=4` and `LowerBound=0` and `UpperBound=3`.
E.g., a `IntegerVectorSpace` can be converted to a `RealVectorSpace` with the same lengths and bounds.

### Genotype Model

Represent simple, immutable objects.
E.g., a `Permuation`, a `RealVector`, a `SymbolicExpressionTree`, etc.

### Algorithm State Model (Input State)

An algorithm state is immutable and holds the "current state" of an algorithm.

E.g., the `GeneticAlgorithmState` simply contains the current `Generation` and the current `Population` (of evaluated genotypes).

### Algorithm Result State (Output State)

An algorithm result can contain additional metainformation, that goes beyond what the (input) AlgorithmState required.
E.g., iteration Duration, Objective, OperatorMetrics (call count and duration of operator calls).

### Objective & Fitness Model

Everything is "Multi-Objective."
Single Objective is just a 1-dimensional multi-objective.

Each Objective has a Order defined that can be used to sort for selection.
Different strategies are available, such as `WeightedSum`, `Lexicographic`, `FixedDimension` (which can be simple be used to simulate single objective), ...

### Problem Model

Problem defines "Evaluation" and "Encoding".

A problem is `Optimizable` if it has evaluation AND an encoding.
If it has only evaluation, we need to "encode" it first.

Algorithms do not care how "hidden" or "nested" the problem is.
Only a final Genotype is required for evaluation.

### Serialization

Base types should be all serializable to JSON (REST) or to Protobuf (gPRC).
This includes:
- Genotypes
- Algorithm States (inputs)
- Algorithm Results (outputs)
- Algorithms (config)
- Operator (configs)
- SearchSpaces (definitions)

Things that are generally not serializable:
- Algorithm and Operator instances (as they can have arbitrary state)
- Phenotypes (as they are from the problem domain)

### Meta Algorithms

Meta algorithms are algorithms that take other algorithms as input.
E.g., `ConcatAlgorithm` Algorithm that takes the last result of one algorithm and feeds it into the next algorithm.
Like a pipeline.
E.g., `LoopAlgorithm`, like the `ConcatAlgorithm`, but feeds the last algorithm results into the first algorithm again.

States must be compatible or we need a state converter.

### Execution Streaming Model

Execute = Execute the whole algorithm and return an `AlgorithmResult`.
Streaming = Execute an iterative algorithm and return multiple `IterationResults`, i.e., an IEnumerable of results.

Streaming is pull-based.
Termination can be done by stop pulling.

### Solver Model

Algorithm = low-level
Solver = high-level

Algorithm returns `AlgorithmResult` or `IterationResult`, which contains alg-specific structures (e.g., a Population) of `genotypes`.

Solver returns a problem-dependent `Phenotype`.

Solver can return a single solution or multiple, "Solve Pareto."

Solve methods are built using extension methods on the `Algorithms`.
