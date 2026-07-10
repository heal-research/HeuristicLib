namespace HEAL.HeuristicLib.APIs.TreeSearchLib.SearchStates;

using Operators.MoveAppliers;
using Operators.MoveCreators;
using Operators.MoveEvaluators;
using Problems.Partial;
using SearchSpaces;

public readonly record struct TreeSearchContext<T, TS, TP, TM>(
    TP Problem,
    IMoveCreatorInstance<T, TS, TP, TM> Creator,
    IMoveApplierInstance<T, TS, TP, TM> Applier,
    IMoveEvaluatorInstance<T, TS, TP, TM> Evaluator)
    where TP : class, IPartialSolutionProblem<T, TS>
    where TS : class, ISearchSpace<T>;

public abstract class GenotypeAwareTreeSearchState<T, TS, TP, TM> : TreeSearchState<TM>
    where TP : class, IPartialSolutionProblem<T, TS>
    where TS : class, ISearchSpace<T>
{
    protected GenotypeAwareTreeSearchState(T genotype, TreeSearchContext<T, TS, TP, TM> context)
    {
        Genotype = genotype;
        Context = context;
    }

    public T Genotype { get; }

    protected TreeSearchContext<T, TS, TP, TM> Context { get; }

    public TP Problem => Context.Problem;
    public IMoveCreatorInstance<T, TS, TP, TM> Creator => Context.Creator;
    public IMoveApplierInstance<T, TS, TP, TM> Applier => Context.Applier;
    public IMoveEvaluatorInstance<T, TS, TP, TM> Evaluator => Context.Evaluator;

    public abstract override GenotypeAwareTreeSearchState<T, TS, TP, TM> Copy();
    public abstract override GenotypeAwareTreeSearchState<T, TS, TP, TM> Branch(TM move);
}
