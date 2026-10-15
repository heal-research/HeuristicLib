using HEAL.HeuristicLib.Operators.MoveAppliers;
using HEAL.HeuristicLib.Operators.MoveCreators;
using HEAL.HeuristicLib.Operators.MoveEvaluators;
using HEAL.HeuristicLib.Problems.Partial;
using HEAL.HeuristicLib.SearchSpaces;

namespace HEAL.HeuristicLib.APIs.TreeSearchLib.SearchStates;

public abstract class GenotypeAwareTreeSearchState<T, TS, TP, TM> : TreeSearchState<TM>
    where TP : class, IPartialSolutionProblem<T, TS>
    where TS : class, ISearchSpace<T>
{
    public record TreeSearchContext(
        TP Problem,
        IMoveCreatorExecution<T, TS, TP, TM> Creator,
        IMoveApplierExecution<T, TS, TP, TM> Applier,
        IMoveEvaluatorExecution<T, TS, TP, TM> Evaluator,
        IMoveEvaluatorExecution<T, TS, TP, TM> BoundEvaluator);


    protected GenotypeAwareTreeSearchState(TreeSearchContext context)
    {
        Context = context;
    }

    protected GenotypeAwareTreeSearchState(GenotypeAwareTreeSearchState<T, TS, TP, TM> other) : this(other.Context) { }


    protected abstract T Genotype { get; }

    protected TreeSearchContext Context { get; }

    protected TP Problem => Context.Problem;
    protected IMoveCreatorExecution<T, TS, TP, TM> Creator => Context.Creator;
    protected IMoveApplierExecution<T, TS, TP, TM> Applier => Context.Applier;
    protected IMoveEvaluatorExecution<T, TS, TP, TM> Evaluator => Context.Evaluator;
    protected IMoveEvaluatorExecution<T, TS, TP, TM> BoundsEvaluator => Context.BoundEvaluator;

    protected abstract override GenotypeAwareTreeSearchState<T, TS, TP, TM> Copy();
    protected abstract override GenotypeAwareTreeSearchState<T, TS, TP, TM> Branch(TM move);
}
