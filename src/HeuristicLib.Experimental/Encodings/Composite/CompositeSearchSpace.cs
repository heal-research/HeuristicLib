using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.MetaOptimization;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.Composite;

public static class CompositeSearchSpace
{
    public static CompositeSearchSpace<T1, TS1, T2, TS2> CombinedWith<T1, TS1, T2, TS2>(this TS1 enc1, TS2 enc2)
        where T1 : class
        where TS1 : class, ISearchSpace<T1>
        where T2 : class
        where TS2 : class, ISearchSpace<T2> =>
        new(enc1, enc2);
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

    public Creator CombineCreators(IOperator<Creation, T1> operator1, IOperator<Creation, T2> operator2) => new(operator1, operator2);
    public Mutator CombineMutator(IOperator<Mutation, T1> operator1, IOperator<Mutation, T2> operator2) => new(operator1, operator2);
    public Crossover CombineCrossover(IOperator<HEAL.HeuristicLib.Operators.Crossover, T1> operator1, IOperator<HEAL.HeuristicLib.Operators.Crossover, T2> operator2) => new(operator1, operator2);

    public sealed class CreationExecutorPair(ICreationExecutor<T1, TS1, IProblem<T1, TS1>> operatorExecutor1, ICreationExecutor<T2, TS2, IProblem<T2, TS2>> operatorExecutor2)
    {
        public ICreationExecutor<T1, TS1, IProblem<T1, TS1>> OperatorExecution1 { get; } = operatorExecutor1;
        public ICreationExecutor<T2, TS2, IProblem<T2, TS2>> OperatorExecution2 { get; } = operatorExecutor2;
    }

    public record Creator(IOperator<Creation, T1> Operator1, IOperator<Creation, T2> Operator2)
        : Operator<Creation, CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>, ICreationExecutor<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>>>
    {
        protected override ICreationExecutor<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>> CreateExecutor(ResolutionScope scope)
        {
            var creator1 = scope.Resolve<T1, TS1, IProblem<T1, TS1>>(Operator1);
            var creator2 = scope.Resolve<T2, TS2, IProblem<T2, TS2>>(Operator2);
            return new Executor(creator1, creator2);
        }

        private sealed class Executor(ICreationExecutor<T1, TS1, IProblem<T1, TS1>> operatorExecutor1, ICreationExecutor<T2, TS2, IProblem<T2, TS2>> operatorExecutor2)
            : ICreationExecutor<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>>
        {
            public IReadOnlyList<CompositeGenotype<T1, T2>> Create(int count, IRandomNumberGenerator random, CompositeSearchSpace<T1, TS1, T2, TS2> searchSpace, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>> problem)
            {
                var parts1 = operatorExecutor1.Create(count, random, searchSpace.SearchSpace, searchSpace.NoProblem1);
                var parts2 = operatorExecutor2.Create(count, random, searchSpace.SearchSpace2, searchSpace.NoProblem2);
                return parts1.Zip(parts2, (a, b) => new CompositeGenotype<T1, T2>(a, b)).ToList();
            }
        }
    }

    public sealed class CrossoverExecutionPair(ICrossoverExecutor<T1, TS1, IProblem<T1, TS1>> operatorExecutor1, ICrossoverExecutor<T2, TS2, IProblem<T2, TS2>> operatorExecutor2)
    {
        public ICrossoverExecutor<T1, TS1, IProblem<T1, TS1>> OperatorExecution1 { get; } = operatorExecutor1;
        public ICrossoverExecutor<T2, TS2, IProblem<T2, TS2>> OperatorExecution2 { get; } = operatorExecutor2;
    }

    public record Crossover(IOperator<HEAL.HeuristicLib.Operators.Crossover, T1> Operator1, IOperator<HEAL.HeuristicLib.Operators.Crossover, T2> Operator2)
        : Operator<HEAL.HeuristicLib.Operators.Crossover, CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>, ICrossoverExecutor<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>>>
    {
        protected override ICrossoverExecutor<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>> CreateExecutor(ResolutionScope scope)
        {
            var crossover1 = scope.Resolve<T1, TS1, IProblem<T1, TS1>>(Operator1);
            var crossover2 = scope.Resolve<T2, TS2, IProblem<T2, TS2>>(Operator2);
            return new Executor(crossover1, crossover2);
        }

        private sealed class Executor(ICrossoverExecutor<T1, TS1, IProblem<T1, TS1>> operatorExecutor1, ICrossoverExecutor<T2, TS2, IProblem<T2, TS2>> operatorExecutor2)
            : ICrossoverExecutor<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>>
        {
            public IReadOnlyList<CompositeGenotype<T1, T2>> Cross(IReadOnlyList<Parents<CompositeGenotype<T1, T2>>> parents, IRandomNumberGenerator random, CompositeSearchSpace<T1, TS1, T2, TS2> searchSpace, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>> problem)
            {
                var res1 = operatorExecutor1.Cross(parents.Select(Selector1).ToArray(), random, searchSpace.SearchSpace, searchSpace.NoProblem1);
                var res2 = operatorExecutor2.Cross(parents.Select(Selector2).ToArray(), random, searchSpace.SearchSpace2, searchSpace.NoProblem2);
                return res1.Zip(res2, ((a, b) => new CompositeGenotype<T1, T2>(a, b))).ToArray();

                static Parents<T2> Selector2(Parents<CompositeGenotype<T1, T2>> x) => Parents.From(x.Parent1.Part2, x.Parent2.Part2);
                static Parents<T1> Selector1(Parents<CompositeGenotype<T1, T2>> x) => Parents.From(x.Parent1.Part1, x.Parent2.Part1);
            }
        }
    }

    public sealed class MutationExecutorPair(IMutationExecutor<T1, TS1, IProblem<T1, TS1>> operatorExecutor1, IMutationExecutor<T2, TS2, IProblem<T2, TS2>> operatorExecutor2)
    {
        public IMutationExecutor<T1, TS1, IProblem<T1, TS1>> OperatorExecution1 { get; } = operatorExecutor1;
        public IMutationExecutor<T2, TS2, IProblem<T2, TS2>> OperatorExecution2 { get; } = operatorExecutor2;
    }

    public record Mutator(IOperator<Mutation, T1> Operator1, IOperator<Mutation, T2> Operator2)
        : Operator<Mutation, CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>, IMutationExecutor<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>>>
    {
        public bool All { get; init; } = true;

        protected override IMutationExecutor<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>> CreateExecutor(ResolutionScope scope)
        {
            var mutator1 = scope.Resolve<T1, TS1, IProblem<T1, TS1>>(Operator1);
            var mutator2 = scope.Resolve<T2, TS2, IProblem<T2, TS2>>(Operator2);
            return new Executor(mutator1, mutator2, All);
        }

        private sealed class Executor(IMutationExecutor<T1, TS1, IProblem<T1, TS1>> operatorExecutor1, IMutationExecutor<T2, TS2, IProblem<T2, TS2>> operatorExecutor2, bool all)
            : IMutationExecutor<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>>>
        {
            public IReadOnlyList<CompositeGenotype<T1, T2>> Mutate(IReadOnlyList<CompositeGenotype<T1, T2>> parents, IRandomNumberGenerator random, CompositeSearchSpace<T1, TS1, T2, TS2> searchSpace, IProblem<CompositeGenotype<T1, T2>, CompositeSearchSpace<T1, TS1, T2, TS2>> problem)
            {
                if (all)
                {
                    var res1 = operatorExecutor1.Mutate(parents.Select(x => x.Part1).ToArray(), random, searchSpace.SearchSpace, searchSpace.NoProblem1);
                    var res2 = operatorExecutor2.Mutate(parents.Select(x => x.Part2).ToArray(), random, searchSpace.SearchSpace2, searchSpace.NoProblem2);
                    return res1.Zip(res2, ((a, b) => new CompositeGenotype<T1, T2>(a, b))).ToArray();
                }

                var assignments = parents.Select(_ => random.NextInt(0, 1, true)).ToArray();
                var result = parents.ToArray();

                var p1 = parents.Select((p, i) => (p, i)).Where(t => assignments[t.i] == 0).Select(t => t.i).ToArray();
                var mutants1 = operatorExecutor1.Mutate(p1.Select(t => parents[t].Part1).ToArray(), random, searchSpace.SearchSpace, searchSpace.NoProblem1);
                foreach (var (i, p) in p1.Zip(mutants1))
                {
                    result[i] = result[i] with { Part1 = p };
                }

                var p2 = parents.Select((p, i) => (p, i)).Where(t => assignments[t.i] == 1).Select(t => t.i).ToArray();
                var mutants2 = operatorExecutor2.Mutate(p2.Select(t => parents[t].Part2).ToArray(), random, searchSpace.SearchSpace2, searchSpace.NoProblem2);
                foreach (var (i, p) in p2.Zip(mutants2))
                {
                    result[i] = result[i] with { Part2 = p };
                }

                return result;
            }
        }
    }
}
