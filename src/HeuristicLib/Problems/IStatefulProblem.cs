using HEAL.HeuristicLib.Optimization;

namespace HEAL.HeuristicLib.Problems;

public interface IStatefulProblem<in TGenotype, out TEncoding, in TUpdateInfo, out TProblemData> : IProblem<TGenotype, TEncoding>
  where TProblemData : IProblemData
  where TEncoding : class, IEncoding<TGenotype> {
  TProblemData CurrentState { get; }

  void UpdateState(TUpdateInfo update);
}
