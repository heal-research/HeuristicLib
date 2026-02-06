using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Genotypes.Vectors;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Vectors;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

#region Parameters
public class ExperimentParameters<T, TE> where TE : class, ISearchSpace<T> where T : class
{
  public string AlgorithmName = "ga";
  public ICreator<T, TE, IProblem<T, TE>>? Creator;
  public ICrossover<T, TE, IProblem<T, TE>>? Crossover;
  public int Elites = 1;
  public int Iterations = 30;
  public double MutationRate = 0.05;
  public IMutator<T, TE, IProblem<T, TE>>? Mutator;
  public int NoChildren = -1;
  public int PopulationSize = 10;
  public int Seed;
  public ISelector<T, TE, IProblem<T, TE>>? Selector;
  public EvolutionStrategyType Strategy = EvolutionStrategyType.Plus;
  public bool TrackGenealogy;
  public bool TrackPopulations;
  public bool WithCrossover;
  public ExperimentParameters() { }

  public ExperimentParameters(ExperimentParameters<T, TE> parameters)
  {
    Seed = parameters.Seed;
    Elites = parameters.Elites;
    PopulationSize = parameters.PopulationSize;
    Iterations = parameters.Iterations;
    MutationRate = parameters.MutationRate;
    NoChildren = parameters.NoChildren;
    WithCrossover = parameters.WithCrossover;
    Strategy = parameters.Strategy;
    Selector = parameters.Selector;
    AlgorithmName = parameters.AlgorithmName;
    Creator = parameters.Creator;
    Crossover = parameters.Crossover;
    Mutator = parameters.Mutator;
    TrackGenealogy = parameters.TrackGenealogy;
    TrackPopulations = parameters.TrackPopulations;
  }
}

public class SymRegExperimentParameters : ExperimentParameters<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>
{
  public int ParameterOptimizationIterations = 10;
  public double TrainingSplit = 0.66;
  public int TreeDepth = 40;
  public int TreeLength = 40;

  public SymRegExperimentParameters() { }

  public SymRegExperimentParameters(SymRegExperimentParameters parameters) : base(parameters)
  {
    TrainingSplit = parameters.TrainingSplit;
    TreeDepth = parameters.TreeDepth;
    TreeLength = parameters.TreeLength;
    ParameterOptimizationIterations = parameters.ParameterOptimizationIterations;
  }
}

public class TravelingSalesmanExperimentParameters : ExperimentParameters<Permutation, PermutationSearchSpace>
{
  public TravelingSalesmanExperimentParameters() { }

  public TravelingSalesmanExperimentParameters(TravelingSalesmanExperimentParameters parameters) : base(parameters) { }
}

public class TestFunctionExperimentParameters : ExperimentParameters<RealVector, RealVectorSearchSpace>
{
  public int Dimension = 10;
  public int Instance = 1;
  public int Problem = 1;
  public TestFunctionExperimentParameters() { }

  public TestFunctionExperimentParameters(TestFunctionExperimentParameters parameters) : base(parameters) { }
}
#endregion
