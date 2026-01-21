namespace HEAL.HeuristicLib.Algorithms;

public interface IAlgorithmBuilderRewriter<in TBuildSpec>
  where TBuildSpec : IBuildSpec
{
  void Rewrite(TBuildSpec buildSpec);
}
