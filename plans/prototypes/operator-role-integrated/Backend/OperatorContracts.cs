using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Execution;
using HEAL.HeuristicLib.Objectives;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.Operators;

// Success/lifecycle subset; around-policy adapters remain a later checkpoint.
public interface IOperator<TCandidate, TOperation> : IOperator
    where TOperation : IOperation<TOperation>
{
    IOperatorContract<TCandidate>? CandidateContract => this as IOperatorContract<TCandidate>;

    PreparedOperator Prepare();
}

public interface IOperation<TSelf> where TSelf : IOperation<TSelf>
{
    static abstract OperationBinding<TCandidate, TSelf, TSearchSpace, TProblem, TRunSearchState>
        CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState;
}

public sealed class Mutation : IOperation<Mutation>
{
    public static OperationBinding<TCandidate, Mutation, TSearchSpace, TProblem, TRunSearchState>
        CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new Binding<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class Binding<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationBinding<TCandidate, Mutation, TSearchSpace, TProblem, TRunSearchState,
            IMutatorExecution<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override IMutatorExecution<TCandidate, TSearchSpace, TProblem> CreateSuccessExecution<THook>(
            IMutatorExecution<TCandidate, TSearchSpace, TProblem> child, THook hook) => new SuccessExecution<THook>(child, hook);

        protected override IMutatorExecution<TCandidate, TSearchSpace, TProblem> CreateHooksExecution<THooks, TToken>(
            IMutatorExecution<TCandidate, TSearchSpace, TProblem> child, THooks hooks) => new HooksExecution<THooks, TToken>(child, hooks);

        private sealed class HooksExecution<THooks, TToken>(IMutatorExecution<TCandidate, TSearchSpace, TProblem> child, THooks hooks)
            : IMutatorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ICallHooks<TToken>
        {
            public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var frame = new CallFrame<THooks, TToken>(hooks);
                try
                {
                    IReadOnlyList<TCandidate> result;
                    int work;
                    try
                    {
                        result = child.Mutate(parents, random, searchSpace, problem);
                        work = result.Count;
                    }
                    catch (Exception failure)
                    {
                        frame.Fault(failure);
                        throw;
                    }
                    frame.Success(work);
                    return result;
                }
                finally { frame.Finally(); }
            }
        }

        private sealed class SuccessExecution<THook>(IMutatorExecution<TCandidate, TSearchSpace, TProblem> child, THook hook)
            : IMutatorExecution<TCandidate, TSearchSpace, TProblem> where THook : struct, ISuccessHook
        {
            public IReadOnlyList<TCandidate> Mutate(IReadOnlyList<TCandidate> parents, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Mutate(parents, random, searchSpace, problem);
                hook.Success(result.Count);
                return result;
            }
        }
    }
}

public sealed class Evaluation : IOperation<Evaluation>
{
    public static OperationBinding<TCandidate, Evaluation, TSearchSpace, TProblem, TRunSearchState>
        CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new Binding<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class Binding<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationBinding<TCandidate, Evaluation, TSearchSpace, TProblem, TRunSearchState,
            IEvaluatorExecution<TCandidate, TSearchSpace, TProblem>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => true;

        protected override IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> CreateSuccessExecution<THook>(
            IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> child, THook hook) => new SuccessExecution<THook>(child, hook);

        protected override IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> CreateHooksExecution<THooks, TToken>(
            IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> child, THooks hooks) => new HooksExecution<THooks, TToken>(child, hooks);

        private sealed class HooksExecution<THooks, TToken>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> child, THooks hooks)
            : IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> where THooks : struct, ICallHooks<TToken>
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var frame = new CallFrame<THooks, TToken>(hooks);
                try
                {
                    IReadOnlyList<ObjectiveVector> result;
                    int work;
                    try
                    {
                        result = child.Evaluate(candidates, random, searchSpace, problem);
                        work = candidates.Count;
                    }
                    catch (Exception failure)
                    {
                        frame.Fault(failure);
                        throw;
                    }
                    frame.Success(work);
                    return result;
                }
                finally { frame.Finally(); }
            }
        }

        private sealed class SuccessExecution<THook>(IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> child, THook hook)
            : IEvaluatorExecution<TCandidate, TSearchSpace, TProblem> where THook : struct, ISuccessHook
        {
            public IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<TCandidate> candidates, IRandomNumberGenerator random,
                TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Evaluate(candidates, random, searchSpace, problem);
                hook.Success(candidates.Count);
                return result;
            }
        }
    }
}

public sealed class Termination : IOperation<Termination>
{
    public static OperationBinding<TCandidate, Termination, TSearchSpace, TProblem, TRunSearchState>
        CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new Binding<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class Binding<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationBinding<TCandidate, Termination, TSearchSpace, TProblem, TRunSearchState,
            ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => false;

        protected override ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateSuccessExecution<THook>(
            ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, THook hook) => new SuccessExecution<THook>(child, hook);

        protected override ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateHooksExecution<THooks, TToken>(
            ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, THooks hooks) => new HooksExecution<THooks, TToken>(child, hooks);

        private sealed class HooksExecution<THooks, TToken>(ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, THooks hooks)
            : ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> where THooks : struct, ICallHooks<TToken>
        {
            public bool IsTerminalState(TRunSearchState state, TSearchSpace searchSpace, TProblem problem)
            {
                var frame = new CallFrame<THooks, TToken>(hooks);
                try
                {
                    bool result;
                    int work;
                    try
                    {
                        result = child.IsTerminalState(state, searchSpace, problem);
                        work = 0;
                    }
                    catch (Exception failure)
                    {
                        frame.Fault(failure);
                        throw;
                    }
                    frame.Success(work);
                    return result;
                }
                finally { frame.Finally(); }
            }
        }

        private sealed class SuccessExecution<THook>(ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, THook hook)
            : ITerminatorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> where THook : struct, ISuccessHook
        {
            public bool IsTerminalState(TRunSearchState state, TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.IsTerminalState(state, searchSpace, problem);
                hook.Success(0);
                return result;
            }
        }
    }
}

public sealed class Interception : IOperation<Interception>
{
    public static OperationBinding<TCandidate, Interception, TSearchSpace, TProblem, TRunSearchState>
        CreateBinding<TCandidate, TSearchSpace, TProblem, TRunSearchState>()
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState =>
        new Binding<TCandidate, TSearchSpace, TProblem, TRunSearchState>();

    private sealed class Binding<TCandidate, TSearchSpace, TProblem, TRunSearchState>
        : OperationBinding<TCandidate, Interception, TSearchSpace, TProblem, TRunSearchState,
            IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState>>
        where TSearchSpace : class, ISearchSpace<TCandidate>
        where TProblem : class, IProblem<TCandidate, TSearchSpace>
        where TRunSearchState : class, ISearchState
    {
        public override bool SupportsCandidateWork => false;

        protected override IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateSuccessExecution<THook>(
            IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, THook hook) => new SuccessExecution<THook>(child, hook);

        protected override IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> CreateHooksExecution<THooks, TToken>(
            IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, THooks hooks) => new HooksExecution<THooks, TToken>(child, hooks);

        private sealed class HooksExecution<THooks, TToken>(IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, THooks hooks)
            : IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> where THooks : struct, ICallHooks<TToken>
        {
            public TRunSearchState Transform(TRunSearchState currentState, TRunSearchState? previousState,
                IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var frame = new CallFrame<THooks, TToken>(hooks);
                try
                {
                    TRunSearchState result;
                    int work;
                    try
                    {
                        result = child.Transform(currentState, previousState, random, searchSpace, problem);
                        work = 0;
                    }
                    catch (Exception failure)
                    {
                        frame.Fault(failure);
                        throw;
                    }
                    frame.Success(work);
                    return result;
                }
                finally { frame.Finally(); }
            }
        }

        private sealed class SuccessExecution<THook>(IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> child, THook hook)
            : IInterceptorExecution<TCandidate, TSearchSpace, TProblem, TRunSearchState> where THook : struct, ISuccessHook
        {
            public TRunSearchState Transform(TRunSearchState currentState, TRunSearchState? previousState,
                IRandomNumberGenerator random, TSearchSpace searchSpace, TProblem problem)
            {
                var result = child.Transform(currentState, previousState, random, searchSpace, problem);
                hook.Success(0);
                return result;
            }
        }
    }
}
