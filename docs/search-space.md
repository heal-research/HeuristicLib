# Search spaces

A **search space** defines which genotypes are valid candidates.

It is intentionally minimal and declarative:

- It does not explain how to search.
- It defines what is allowed.

Generation and variation are handled by operators (`ICreator`, `ICrossover`, `IMutator`).

> [!IMPORTANT]
> Search-space dependent operators are responsible for enforcing the search space.
>
> - Operators generally assume they receive genotypes that already satisfy `searchSpace.Contains(genotype)` and may throw if that contract is violated.
> - Operators that produce genotypes (creators, mutators, crossovers, repair operators) must guarantee that their outputs also satisfy the provided search space.

## Contract

`ISearchSpace<TGenotype>` is a single method:

- `bool Contains(TGenotype genotype)`

There is also a non-generic marker interface `ISearchSpace`.

## Search spaces in this repository

The repository includes ready-to-use search spaces for common genotype families:

- `RealVectorSearchSpace` (length + per-dimension min/max)
- `PermutationSearchSpace` (length)
- `IntegerVectorSearchSpace`
- `BoolVectorSearchSpace`

Search spaces are often records, which makes them lightweight value objects.

## Example

```csharp
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

var space = new RealVectorSearchSpace(
	length: 3,
	minimum: -5.0,
	maximum: +5.0
);

Console.WriteLine(space.Contains(new double[] { 0.0, 1.0, -2.0 })); // True
```

## Related pages

- [Problem](problem.md)
- [Operators](operators.md)

