# Reproducible randomness

Every HeuristicLib run receives an `IRandomNumberGenerator`. Create it from a recorded seed:

```csharp
var random = RandomNumberGenerator.Create(seed: 123);
var finalState = await algorithm.CompleteAsync(problem, random);
```

Keep the seed with the algorithm configuration, package version and problem data. The same inputs reproduce the same random sequence.

## Draw values

Methods such as `NextInt()` and `NextDouble()` advance the generator. Changing the order or number of calls changes every value that follows.

Use the provided sampling helpers instead of implementing common distributions yourself:

```csharp
double unitValue = random.NextDouble();
double boundedValue = random.NextDouble(-5.0, 5.0);
double normalValue = random.NextNormal(mu: 0.0, sigma: 1.0);
RealVector candidate = random.NextRealVectorUniform(searchSpace);
```

With `RandomNumberGenerator.Create(seed: 123)` and a four dimensional Rastrigin search space, those four calls produce:

```
unitValue     0.9054
boundedValue  -4.4638
normalValue   -0.5766
candidate     [0.812, -3.516, -0.212, -3.938]
```

Creators such as `UniformDistributedCreator` and `RandomPermutationCreator` use the same random source when an algorithm creates candidates.

## Fork independent streams

`Fork(ulong forkKey)` derives a child generator without drawing a value or changing the parent:

```csharp
var trialRandom = random.Fork(trialIndex);
var candidateRandom = trialRandom.Fork(candidateIndex);
```

The same parent and fork key always produce the same child stream:

```csharp
var root = RandomNumberGenerator.Create(seed: 123);

Console.WriteLine($"root.Fork(0) first draw: {root.Fork(0).NextDouble():F4}");
Console.WriteLine($"root.Fork(1) first draw: {root.Fork(1).NextDouble():F4}");
Console.WriteLine($"root.Fork(0) again:      {root.Fork(0).NextDouble():F4}");
```

```
root.Fork(0) first draw: 0.8665
root.Fork(1) first draw: 0.6622
root.Fork(0) again:      0.8665
```

Different keys give unrelated streams, and the same key gives the same stream every time. Note that the third line repeats the first even though a fork happened in between: forking does not advance the parent, so the order in which you fork does not matter.

Use stable keys such as a trial index, iteration number, operator index or candidate index.

Do not derive fork keys from task completion order, thread IDs or timestamps. Those values change when scheduling changes.

## Parallel work

Give each independent unit of work its own child generator. Results then stay stable when the degree of parallelism changes:

```csharp
Parallel.For(0, candidates.Count, index =>
{
    var candidateRandom = random.Fork((ulong)index);
    Mutate(candidates[index], candidateRandom);
});
```

Sharing one generator across concurrent workers makes results depend on draw order. Fork before starting parallel work.

## Repeated runs

Algorithms do not own a seed. Derive one generator per run:

```csharp
var rootRandom = RandomNumberGenerator.Create(seed: 123);

for (ulong runIndex = 0; runIndex < 20; runIndex++)
{
    var runRandom = rootRandom.Fork(runIndex);
    var state = await algorithm.CompleteAsync(problem, runRandom);
    Save(runIndex, state);
}
```

The [experiments API](/guide/execution/experiments) handles trial keys, independent runs and concurrency when you need repetitions or parameter grids.

## Rules to keep

- Create the root generator at the application or experiment boundary.
- Pass generators into algorithms and operators.
- Use `Next...` methods to draw values.
- Use `Fork` with stable keys for independent work.
- Record the root seed with every reported result.

See [Running algorithms](/guide/execution/running-algorithms) for run creation and cancellation.
