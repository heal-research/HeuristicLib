using System.Security.AccessControl;

namespace HEAL.HeuristicLib.Optimization;

public static class SingleObjective {
  public static readonly Objective Minimize = new Objective([ObjectiveDirection.Minimize], FitnessTotalOrderComparer.CreateSingleObjectiveComparer(ObjectiveDirection.Minimize));
  public static readonly Objective Maximize = new Objective([ObjectiveDirection.Maximize], FitnessTotalOrderComparer.CreateSingleObjectiveComparer(ObjectiveDirection.Maximize));

  public static Objective Create(ObjectiveDirection direction) => direction switch {
    ObjectiveDirection.Minimize => Minimize,
    ObjectiveDirection.Maximize => Maximize,
    _ => throw new NotImplementedException()
  };

  public static Objective WithSingleObjective(this Objective objectives) => Create(objectives.Directions.Single());
}
