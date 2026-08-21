namespace HEAL.HeuristicLib.Objectives;

public class SingleObjectiveComparer(ObjectiveDirection objectiveDirection) : IComparer<ObjectiveVector>
{
    public int Compare(ObjectiveVector? x, ObjectiveVector? y)
    {
        if ((x is not null && !x.IsSingleObjective) || (y is not null && !y.IsSingleObjective))
        {
            throw new ArgumentException("Objective vector must be single-objective");
        }

        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return +1;
        }

        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        return ObjectiveValue.Compare(x[0], y[0], objectiveDirection);
    }
}
