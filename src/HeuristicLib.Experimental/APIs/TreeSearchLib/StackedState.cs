using TreesearchLib;

namespace HEAL.HeuristicLib.APIs.TreeSearchLib;

public abstract class StackedState<T> : IMutableState<StackedState<T>, T, ObjectiveVectorQuality>
{
    private Stack<T> Decisions { get; set; }

    protected StackedState(StackedState<T> stackedState)
    {
        Decisions = new Stack<T>(stackedState.Decisions.Reverse());
    }

    protected StackedState()
    {
        Decisions = [];
    }

    public abstract object Clone();

    public bool IsTerminal { get; protected set; }

    private bool hasBound;
    public ObjectiveVectorQuality Bound
    {
        get
        {
            if (hasBound)
                return field;

            hasBound = true;
            field = CalculateBound();
            return field;
        }
        protected set
        {
            hasBound = true;
            field = value;
        }
    }
    protected abstract ObjectiveVectorQuality CalculateBound();

    private bool hasQuality;
    public ObjectiveVectorQuality? Quality
    {
        get
        {
            if (hasQuality)
                return field;

            hasQuality = true;
            field = CalculateQuality();
            return field;
        }
        protected set
        {
            hasQuality = true;
            field = value;
        }
    }
    protected abstract ObjectiveVectorQuality? CalculateQuality();

    public abstract IEnumerable<T> GetChoices();

    public void Apply(T choice)
    {
        hasBound = false;
        hasQuality = false;
        Decisions.Push(choice);
        ApplyChoice(choice);
    }

    protected abstract void ApplyChoice(T choice);

    public void UndoLast()
    {
        hasBound = false;
        hasQuality = false;
        UndoLastChoice(Decisions.Pop());
    }

    protected abstract void UndoLastChoice(T choice);
}
