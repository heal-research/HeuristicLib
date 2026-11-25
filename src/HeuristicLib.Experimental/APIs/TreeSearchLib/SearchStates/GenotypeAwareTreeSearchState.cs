using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems.Partial;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.TreeSearchLib.SearchStates;

public abstract class GenotypeAwareTreeSearchState<T, TS, TP, TM> : TreeSearchState<TM>
    where TP : class, IPartialSolutionProblem<T, TS>
    where TS : class, ISearchSpace<T>
{
    public record TreeSearchContext(
        TP Problem,
        IMoveCreationExecutor<T, TM> Creator,
        IMoveApplicationExecutor<T, TM> Applier,
        IMoveEvaluationExecutor<T, TM> Evaluator,
        IMoveEvaluationExecutor<T, TM> BoundEvaluator);


    protected GenotypeAwareTreeSearchState(TreeSearchContext context)
    {
        Context = context;
    }

    protected GenotypeAwareTreeSearchState(GenotypeAwareTreeSearchState<T, TS, TP, TM> other) : this(other.Context) { }


    protected abstract T Genotype { get; }

    protected TreeSearchContext Context { get; }

    protected TP Problem => Context.Problem;
    protected IMoveCreationExecutor<T, TM> Creator => Context.Creator;
    protected IMoveApplicationExecutor<T, TM> Applier => Context.Applier;
    protected IMoveEvaluationExecutor<T, TM> Evaluator => Context.Evaluator;
    protected IMoveEvaluationExecutor<T, TM> BoundsEvaluator => Context.BoundEvaluator;

    protected abstract override GenotypeAwareTreeSearchState<T, TS, TP, TM> Copy();
    protected abstract override GenotypeAwareTreeSearchState<T, TS, TP, TM> Branch(TM move);
}
