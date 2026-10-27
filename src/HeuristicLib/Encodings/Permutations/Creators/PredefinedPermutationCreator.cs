using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Creators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Encodings.Permutations;

/// <summary>Cycles through predefined permutations, or samples them uniformly with replacement.</summary>
/// <remarks>The cursor belongs to the execution. Independent runs start at the first permutation.</remarks>
public sealed record PredefinedPermutationCreator : ICreator<Permutation>
{
    public ValueArray<Permutation> PredefinedPermutations { get; init; }

    /// <summary>Samples uniformly with replacement when true; otherwise cycles in the configured order.</summary>
    public bool WithRandom { get; init; }

    public PredefinedPermutationCreator(IReadOnlyList<Permutation> predefinedPermutations)
    {
        PredefinedPermutations = predefinedPermutations.ToValueArray();
    }

    public ExecutionFactory<ICreatorExecution<Permutation, TRunSearchSpace, TRunProblem>> CreateExecutionFactory<TRunSearchSpace, TRunProblem>()
        where TRunSearchSpace : class, ISearchSpace<Permutation>
        where TRunProblem : class, IProblem<Permutation, TRunSearchSpace>
    {
        if (PredefinedPermutations.Count == 0)
            throw new InvalidOperationException("At least one predefined permutation must be provided.");

        var state = new ExecutionState();
        return _ => new Execution<TRunSearchSpace, TRunProblem>(PredefinedPermutations, WithRandom, state);
    }

    private sealed class ExecutionState
    {
        public int Index { get; set; }
    }

    private sealed class Execution<TSearchSpace, TProblem>(ValueArray<Permutation> permutations, bool withRandom, ExecutionState state)
        : CreatorExecution<Permutation, TSearchSpace, TProblem>
        where TSearchSpace : class, ISearchSpace<Permutation>
        where TProblem : class, IProblem<Permutation, TSearchSpace>
    {
        public override IReadOnlyList<Permutation> Create(int count, IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            // A creator must not introduce candidates outside the requested space.
            for (var i = 0; i < permutations.Count; i++)
                if (!searchSpace.Contains(permutations[i]))
                    throw new ArgumentException("A predefined permutation does not belong to the search space.", nameof(searchSpace));

            var result = new Permutation[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = permutations[withRandom ? random.NextInt(permutations.Count) : state.Index];
                if (!withRandom)
                    state.Index = (state.Index + 1) % permutations.Count;
            }

            return result;
        }
    }
}
