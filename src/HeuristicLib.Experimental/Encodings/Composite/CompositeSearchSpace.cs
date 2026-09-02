using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MetaOptimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.Composite;

public static class CompositeSearchSpace
{
    public static CompositeSearchSpace<T1, TS1, T2, TS2> WithSearchSpace<T1, TS1, T2, TS2>(this TS1 enc1, TS2 enc2)
        where T1 : class
        where TS1 : class, ISearchSpace<T1>
        where T2 : class
        where TS2 : class, ISearchSpace<T2>
        => new(enc1, enc2);
}

public record CompositeSearchSpace<T1, TS1, T2, TS2>(TS1 SearchSpace, TS2 SearchSpace2) : ISearchSpace<CompositeGenotype<T1, T2>>
    where T1 : class
    where T2 : class
    where TS1 : class, ISearchSpace<T1>
    where TS2 : class, ISearchSpace<T2>
{
    public readonly NoProblem<T1, TS1> NoProblem1 = new(SearchSpace);
    public readonly NoProblem<T2, TS2> NoProblem2 = new(SearchSpace2);
    public bool Contains(CompositeGenotype<T1, T2> candidate) => SearchSpace.Contains(candidate.Part1) && SearchSpace2.Contains(candidate.Part2);

    public Creator CombineCreators(ICreator<T1> operator1, ICreator<T2> operator2) => new(operator1, operator2);
    public Mutator CombineMutator(IMutator<T1> operator1, IMutator<T2> operator2) => new(operator1, operator2);
    public Crossover CombineCrossover(ICrossover<T1> operator1, ICrossover<T2> operator2) => new(operator1, operator2);

    public sealed class CreatorInstancePair(ICreatorInstance<T1, TS1, IProblem<T1, TS1>> operatorInstance1, ICreatorInstance<T2, TS2, IProblem<T2, TS2>> operatorInstance2)
    {
        public ICreatorInstance<T1, TS1, IProblem<T1, TS1>> OperatorInstance1 { get; } = operatorInstance1;
        public ICreatorInstance<T2, TS2, IProblem<T2, TS2>> OperatorInstance2 { get; } = operatorInstance2;
    }

    public record Creator(ICreator<T1> Operator1, ICreator<T2> Operator2)
        : ICreator<CompositeGenotype<T1, T2>>
    {
        public ICreatorInstance<CompositeGenotype<T1, T2>, TSearchSpace, TProblem> CreateExecutionInstance<TSearchSpace, TProblem>(ExecutionInstanceRegistry instanceRegistry)
            where TSearchSpace : class, ISearchSpace<CompositeGenotype<T1, T2>>
            where TProblem : class, IProblem<CompositeGenotype<T1, T2>, TSearchSpace>
        {
            var instance = new Instance(
                instanceRegistry.Resolve<T1, TS1, IProblem<T1, TS1>>(Operator1),
                instanceRegistry.Resolve<T2, TS2, IProblem<T2, TS2>>(Operator2));

            if (instance is not ICreatorInstance<CompositeGenotype<T1, T2>, TSearchSpace, TProblem> typed)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} is written for {typeof(CompositeSearchSpace<T1, TS1, T2, TS2>).Name} and cannot run over {typeof(TSearchSpace).Name}.");
            }

            return typed;
        }

        private sealed class Instance(ICreatorInstance<T1, TS1, IProblem<T1, TS1>> operatorInstance1, ICreatorInstance<T2, TS2, IProblem<T2, TS2>> operatorInstance2)
            : ICreatorInstance<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>>
        {
            public IReadOnlyList<CompositeGenotype<T1, T2>> Create(int count, IRandomNumberGenerator random, CompositeSearchSpace<T1, TS1, T2, TS2> searchSpace, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>> problem)
            {
                var parts1 = operatorInstance1.Create(count, random, searchSpace.SearchSpace, searchSpace.NoProblem1);
                var parts2 = operatorInstance2.Create(count, random, searchSpace.SearchSpace2, searchSpace.NoProblem2);
                return parts1.Zip(parts2, (a, b) => new CompositeGenotype<T1, T2>(a, b)).ToList();
            }
        }
    }

    public sealed class CrossoverInstancePair(ICrossoverInstance<T1, TS1, IProblem<T1, TS1>> operatorInstance1, ICrossoverInstance<T2, TS2, IProblem<T2, TS2>> operatorInstance2)
    {
        public ICrossoverInstance<T1, TS1, IProblem<T1, TS1>> OperatorInstance1 { get; } = operatorInstance1;
        public ICrossoverInstance<T2, TS2, IProblem<T2, TS2>> OperatorInstance2 { get; } = operatorInstance2;
    }

    public record Crossover(ICrossover<T1> Operator1, ICrossover<T2> Operator2)
        : ICrossover<CompositeGenotype<T1, T2>>
    {
        public ICrossoverInstance<CompositeGenotype<T1, T2>, TSearchSpace, TProblem> CreateExecutionInstance<TSearchSpace, TProblem>(ExecutionInstanceRegistry instanceRegistry)
            where TSearchSpace : class, ISearchSpace<CompositeGenotype<T1, T2>>
            where TProblem : class, IProblem<CompositeGenotype<T1, T2>, TSearchSpace>
        {
            var instance = new Instance(
                instanceRegistry.Resolve<T1, TS1, IProblem<T1, TS1>>(Operator1),
                instanceRegistry.Resolve<T2, TS2, IProblem<T2, TS2>>(Operator2));

            if (instance is not ICrossoverInstance<CompositeGenotype<T1, T2>, TSearchSpace, TProblem> typed)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} is written for {typeof(CompositeSearchSpace<T1, TS1, T2, TS2>).Name} and cannot run over {typeof(TSearchSpace).Name}.");
            }

            return typed;
        }

        private sealed class Instance(ICrossoverInstance<T1, TS1, IProblem<T1, TS1>> operatorInstance1, ICrossoverInstance<T2, TS2, IProblem<T2, TS2>> operatorInstance2)
            : ICrossoverInstance<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>>
        {
            public IReadOnlyList<CompositeGenotype<T1, T2>> Cross(IReadOnlyList<Parents<CompositeGenotype<T1, T2>>> parents, IRandomNumberGenerator random, CompositeSearchSpace<T1, TS1, T2, TS2> searchSpace, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>> problem)
            {
                var res1 = operatorInstance1.Cross(parents.Select(Selector1).ToArray(), random, searchSpace.SearchSpace, searchSpace.NoProblem1);
                var res2 = operatorInstance2.Cross(parents.Select(Selector2).ToArray(), random, searchSpace.SearchSpace2, searchSpace.NoProblem2);
                return res1.Zip(res2, ((a, b) => new CompositeGenotype<T1, T2>(a, b))).ToArray();

                static Parents<T2> Selector2(Parents<CompositeGenotype<T1, T2>> x) => Parents.From(x.Parent1.Part2, x.Parent2.Part2);
                static Parents<T1> Selector1(Parents<CompositeGenotype<T1, T2>> x) => Parents.From(x.Parent1.Part1, x.Parent2.Part1);
            }
        }
    }

    public sealed class MutatorInstancePair(IMutatorInstance<T1, TS1, IProblem<T1, TS1>> operatorInstance1, IMutatorInstance<T2, TS2, IProblem<T2, TS2>> operatorInstance2)
    {
        public IMutatorInstance<T1, TS1, IProblem<T1, TS1>> OperatorInstance1 { get; } = operatorInstance1;
        public IMutatorInstance<T2, TS2, IProblem<T2, TS2>> OperatorInstance2 { get; } = operatorInstance2;
    }

    public record Mutator(IMutator<T1> Operator1, IMutator<T2> Operator2)
        : IMutator<CompositeGenotype<T1, T2>>
    {
        public bool All { get; init; } = true;

        public IMutatorInstance<CompositeGenotype<T1, T2>, TSearchSpace, TProblem> CreateExecutionInstance<TSearchSpace, TProblem>(ExecutionInstanceRegistry instanceRegistry)
            where TSearchSpace : class, ISearchSpace<CompositeGenotype<T1, T2>>
            where TProblem : class, IProblem<CompositeGenotype<T1, T2>, TSearchSpace>
        {
            var registry = instanceRegistry;
            var instance = new Instance(
                registry.Resolve<T1, TS1, IProblem<T1, TS1>>(Operator1),
                registry.Resolve<T2, TS2, IProblem<T2, TS2>>(Operator2),
                All);

            if (instance is not IMutatorInstance<CompositeGenotype<T1, T2>, TSearchSpace, TProblem> typed)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} is written for {typeof(CompositeSearchSpace<T1, TS1, T2, TS2>).Name} and cannot run over {typeof(TSearchSpace).Name}.");
            }

            return typed;
        }

        private sealed class Instance(IMutatorInstance<T1, TS1, IProblem<T1, TS1>> operatorInstance1, IMutatorInstance<T2, TS2, IProblem<T2, TS2>> operatorInstance2, bool all)
            : IMutatorInstance<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>>
        {
            public IReadOnlyList<CompositeGenotype<T1, T2>> Mutate(IReadOnlyList<CompositeGenotype<T1, T2>> parents, IRandomNumberGenerator random, CompositeSearchSpace<T1, TS1, T2, TS2> searchSpace, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>> problem)
            {
                if (all)
                {
                    var res1 = operatorInstance1.Mutate(parents.Select(x => x.Part1).ToArray(), random, searchSpace.SearchSpace, searchSpace.NoProblem1);
                    var res2 = operatorInstance2.Mutate(parents.Select(x => x.Part2).ToArray(), random, searchSpace.SearchSpace2, searchSpace.NoProblem2);
                    return res1.Zip(res2, ((a, b) => new CompositeGenotype<T1, T2>(a, b))).ToArray();
                }

                var assignments = parents.Select(_ => random.NextInt(0, 1, true)).ToArray();
                var result = parents.ToArray();

                var p1 = parents.Select((p, i) => (p, i)).Where(t => assignments[t.i] == 0).Select(t => t.i).ToArray();
                var mutants1 = operatorInstance1.Mutate(p1.Select(t => parents[t].Part1).ToArray(), random, searchSpace.SearchSpace, searchSpace.NoProblem1);
                foreach (var (i, p) in p1.Zip(mutants1))
                {
                    result[i] = result[i] with { Part1 = p };
                }

                var p2 = parents.Select((p, i) => (p, i)).Where(t => assignments[t.i] == 1).Select(t => t.i).ToArray();
                var mutants2 = operatorInstance2.Mutate(p2.Select(t => parents[t].Part2).ToArray(), random, searchSpace.SearchSpace2, searchSpace.NoProblem2);
                foreach (var (i, p) in p2.Zip(mutants2))
                {
                    result[i] = result[i] with { Part2 = p };
                }

                return result;
            }
        }
    }
}
