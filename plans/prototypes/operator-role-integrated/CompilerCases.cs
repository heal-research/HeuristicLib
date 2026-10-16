using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.OperatorRolePrototype;
using HEAL.HeuristicLib.OperatorRolePrototype.Authoring;
using HEAL.HeuristicLib.Operators;

namespace OperatorRoleCompilerConsumer;

internal static class CompilerCases
{
    private static void Main()
    {
#if WRONG_CANDIDATE
        IOperator<string, Mutation> source = new IdentityMutation();
#elif WRONG_OPERATION
        IOperator<RealVector, Evaluation> source = new IdentityMutation();
#elif ALGORITHM_SLOT
        IOperator<RealVector, Mutation> source = GetAlgorithm();
#elif ALGORITHM_NAMED
        var source = new Named<RealVector, Mutation>(GetAlgorithm(), "invalid");
#elif WRONG_ADVICE
        var source = Aspect.Create(Pointcut.OfType<IOperator<RealVector, Mutation>>(), GetEvaluationAdvice());
#else
        IOperator<RealVector, Mutation> source = new IdentityMutation().Named("valid");
#endif
        System.GC.KeepAlive(source);
    }

    private static IAlgorithm<RealVector> GetAlgorithm() => throw new System.NotSupportedException("Compile-only case.");
    private static OperatorAdvice<RealVector, Evaluation> GetEvaluationAdvice() => throw new System.NotSupportedException("Compile-only case.");

    private sealed record IdentityMutation : StatelessMutator<RealVector>
    {
        public override System.Collections.Generic.IReadOnlyList<RealVector> Mutate(
            System.Collections.Generic.IReadOnlyList<RealVector> parents,
            HEAL.HeuristicLib.Random.IRandomNumberGenerator random) => parents;
    }
}
