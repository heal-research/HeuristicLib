using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Inspection;

public sealed record MutatorObservation<TCandidate, TSearchSpace, TProblem>(
    IOperator<Mutation, TCandidate> Mutator,
    IReadOnlyList<TCandidate> Offspring,
    IReadOnlyList<TCandidate> Parents,
    TSearchSpace SearchSpace,
    TProblem Problem) : Observation<TProblem>(Problem)
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>;

public static class MutatorObservations
{
    extension(Aspect)
    {
        /// <summary>Observes every call to one chosen mutator.</summary>
        /// <remarks>
        /// The mutator names only its candidate, so the observation names the search space and problem it reads. A run
        /// over types the observation was not written for is reported when the execution graph is built.
        /// </remarks>
        public static Aspect Observe<TCandidate, TSearchSpace, TProblem>(
            IOperator<Mutation, TCandidate> mutator,
            Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
            where TSearchSpace : class, ISearchSpace<TCandidate>
            where TProblem : class, IProblem<TCandidate, TSearchSpace> =>
            Aspect.Create(Pointcut.Reference(mutator), new MutatorObservationAdvice<TCandidate, TSearchSpace, TProblem>(observe));

        /// <summary>Observes every call to one chosen mutator, for an observer that reads no particular search space or problem.</summary>
        /// <remarks>
        /// An implicitly typed lambda binds here, because the overload naming the search space and problem cannot infer
        /// them from it. The observation fits every run over the candidate.
        /// </remarks>
        public static Aspect Observe<TCandidate>(
            IOperator<Mutation, TCandidate> mutator,
            Action<MutatorObservation<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>> observe) =>
            Aspect.Observe<TCandidate, ISearchSpace<TCandidate>, IProblem<TCandidate, ISearchSpace<TCandidate>>>(mutator, observe);
    }
}

/// <summary>Reports every call to each mutator its pointcut selects.</summary>
/// <param name="observe">Receives each observation on the thread that called the mutator.</param>
public sealed class MutatorObservationAdvice<TCandidate, TSearchSpace, TProblem>(
    Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe) : OperatorAdvice<Mutation, TCandidate>
    where TSearchSpace : class, ISearchSpace<TCandidate>
    where TProblem : class, IProblem<TCandidate, TSearchSpace>
{
    public override OperatorAdviceWrapperFactory<Mutation, TCandidate> CreateWrapperFactory(IOperator<Mutation, TCandidate> mutator) =>
        new Factory(mutator, observe, this);

    private sealed class Factory(IOperator<Mutation, TCandidate> source,
        Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe, Advice advice)
        : OperatorAdviceWrapperFactory<Mutation, TCandidate>
    {
        public override WrapperExecutorFactory GetWrapperFactory<TRunSearchSpace, TRunProblem, TRunSearchState>(
            IOperator<Mutation, TCandidate> selected,
            OperationExecutorAdapter<Mutation, TCandidate, TRunSearchSpace, TRunProblem, TRunSearchState> adapter)
        {
            ObservationSignature.Require<TSearchSpace, TProblem, TRunSearchSpace, TRunProblem>(advice);
            return WrapperExecutorFactory.From<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>>(child =>
                new ExecutorWithControls<IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>>(
                    new Executor<TRunSearchSpace, TRunProblem>(source, child.Executor, observe), child.ControlProvider));
        }
    }

    private sealed class Executor<TRunSearchSpace, TRunProblem>(
        IOperator<Mutation, TCandidate> observedMutator,
        IMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem> childMutator,
        Action<MutatorObservation<TCandidate, TSearchSpace, TProblem>> observe)
        : WrappingMutationExecutor<TCandidate, TRunSearchSpace, TRunProblem>(childMutator)
        where TRunSearchSpace : class, ISearchSpace<TCandidate>
        where TRunProblem : class, IProblem<TCandidate, TRunSearchSpace>
    {
        public override IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
            TRunSearchSpace searchSpace, TRunProblem problem)
        {
            var offspring = ChildMutator.Mutate(parents, random, searchSpace, problem);
            observe(new MutatorObservation<TCandidate, TSearchSpace, TProblem>(
                observedMutator, offspring, parents, (TSearchSpace)(object)searchSpace, (TProblem)(object)problem));
            return offspring;
        }
    }
}
