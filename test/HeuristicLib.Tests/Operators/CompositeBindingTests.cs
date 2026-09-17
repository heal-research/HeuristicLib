using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Tests.Operators;

/// <summary>
/// Pins why an operator that owns children cannot be written on the leaf ladder, which is the reason the composite
/// bases exist and the rule any new role has to follow.
/// </summary>
public class CompositeBindingTests
{
    /// <summary>
    /// The leaf ladder fixes the search space and problem on the operator's <em>type</em>, so the authored member
    /// never sees the run's. A composite written there can only resolve its children at the triple it was fixed at,
    /// and the run is narrowed to that — breaking children the run itself supports.
    /// </summary>
    /// <remarks>
    /// Note where the failure lands: on the child, not on the composite. The composite's own instance is built at the
    /// widest triple and converts to anything, so nothing rejects the wrapper; the child is asked for an instance over
    /// <see cref="ISearchSpace{TCandidate}"/> and refuses, even though the run is over the space it was written for.
    /// </remarks>
    [Fact]
    public void ACompositeOnTheLeafLadder_NarrowsTheRunAndRefusesAChildTheRunSupports()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(3, -1.0, 1.0);
        var composite = new LeafLadderTransformedCreator(new UniformDistributedCreator(searchSpace));

        var failure = Should.Throw<InvalidOperationException>(() =>
            new ExecutionInstanceRegistry()
                .Resolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(composite));

        failure.Message.ShouldContain(nameof(UniformDistributedCreator));
    }

    /// <summary>
    /// The same composite written as one: the run's types arrive as method type arguments, so the child resolves at
    /// the run's triple and works.
    /// </summary>
    [Fact]
    public void ACompositeThatTakesTheRunsTypes_ResolvesTheSameChild()
    {
        var searchSpace = new BoundedRealVectorSearchSpace(3, -1.0, 1.0);
        var composite = new AgnosticTransformedCreator(new UniformDistributedCreator(searchSpace));

        var instance = new ExecutionInstanceRegistry()
            .Resolve<RealVector, BoundedRealVectorSearchSpace, IProblem<RealVector, BoundedRealVectorSearchSpace>>(composite);

        instance.ShouldNotBeNull();
    }

    /// <summary>How not to write it: the base fixes the triple, so the child is resolved at the wrong one.</summary>
    private sealed record LeafLadderTransformedCreator(ICreator<RealVector> Source) : Creator<RealVector>
    {
        public override ICreatorInstance<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>
            CreateExecutionInstance(ExecutionInstanceRegistry instanceRegistry) =>
            new Instance(instanceRegistry.Resolve<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>(Source));

        private sealed class Instance(ICreatorInstance<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>> source)
            : CreatorInstance<RealVector, ISearchSpace<RealVector>, IProblem<RealVector, ISearchSpace<RealVector>>>
        {
            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, ISearchSpace<RealVector> searchSpace, IProblem<RealVector, ISearchSpace<RealVector>> problem) =>
                source.Create(count, random, searchSpace, problem);
        }
    }

    /// <summary>How to write it: the run's types are method type arguments, so they reach the child.</summary>
    private sealed record AgnosticTransformedCreator(ICreator<RealVector> Source) : ICreator<RealVector>
    {
        public ICreatorInstance<RealVector, TRunSearchSpace, TRunProblem> CreateExecutionInstance<TRunSearchSpace, TRunProblem>(ExecutionInstanceRegistry instanceRegistry)
            where TRunSearchSpace : class, ISearchSpace<RealVector>
            where TRunProblem : class, IProblem<RealVector, TRunSearchSpace> =>
            new Instance<TRunSearchSpace, TRunProblem>(instanceRegistry.Resolve<RealVector, TRunSearchSpace, TRunProblem>(Source));

        private sealed class Instance<TSearchSpace, TProblem>(ICreatorInstance<RealVector, TSearchSpace, TProblem> source)
            : CreatorInstance<RealVector, TSearchSpace, TProblem>
            where TSearchSpace : class, ISearchSpace<RealVector>
            where TProblem : class, IProblem<RealVector, TSearchSpace>
        {
            public override IReadOnlyList<RealVector> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem) =>
                source.Create(count, random, searchSpace, problem);
        }
    }
}
