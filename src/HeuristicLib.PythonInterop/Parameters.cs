using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Encodings.Permutations;
using HEAL.HeuristicLib.Encodings.RealVectors;
using HEAL.HeuristicLib.Encodings.SymbolicExpressions;
using HEAL.HeuristicLib.Operators;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.SearchSpaces;

// These classes are used for cross language purposes and therefore have public properties and constructors.

namespace HEAL.HeuristicLib.PythonInterop;

#region Parameters
public class ExperimentParameters<TCandidate, TSearchSpace> where TSearchSpace : class, ISearchSpace<TCandidate>
{
    public string AlgorithmName { get; set; } = "ga";
    public ICreator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? Creator { get; set; }
    public ICrossover<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? Crossover { get; set; }
    public int Elites { get; set; } = 1;
    public int Iterations { get; set; } = 30;
    public double MutationRate { get; set; } = 0.05;
    public IMutator<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? Mutator { get; set; }
    public int NoChildren { get; set; } = -1;
    public int PopulationSize { get; set; } = 10;
    public int Seed { get; set; }
    public ISelector<TCandidate, TSearchSpace, IProblem<TCandidate, TSearchSpace>>? Selector { get; set; }
    public EvolutionStrategyType Strategy { get; set; } = EvolutionStrategyType.Plus;
    public bool TrackGenealogy { get; set; }
    public bool TrackPopulations { get; set; }
    public bool WithCrossover { get; set; }

    public ExperimentParameters() { }

    public ExperimentParameters(ExperimentParameters<TCandidate, TSearchSpace> parameters)
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

public class SymRegExperimentParameters : ExperimentParameters<ExpressionTree, ExpressionTreeSearchSpace>
{
    public int ParameterOptimizationIterations { get; set; } = 10;
    public double TrainingSplit { get; set; } = 0.66;
    public int TreeDepth { get; set; } = 40;
    public int TreeLength { get; set; } = 40;
    public bool UseLinearScaling { get; set; } = true;

    public SymRegExperimentParameters() { }

    public SymRegExperimentParameters(SymRegExperimentParameters parameters) : base(parameters)
    {
        TrainingSplit = parameters.TrainingSplit;
        TreeDepth = parameters.TreeDepth;
        TreeLength = parameters.TreeLength;
        ParameterOptimizationIterations = parameters.ParameterOptimizationIterations;
        UseLinearScaling = parameters.UseLinearScaling;
    }
}

public class TravelingSalesmanExperimentParameters : ExperimentParameters<Permutation, PermutationSearchSpace>
{
    public TravelingSalesmanExperimentParameters() { }

    public TravelingSalesmanExperimentParameters(TravelingSalesmanExperimentParameters parameters) : base(parameters) { }
}

public class TestFunctionExperimentParameters : ExperimentParameters<RealVector, RealVectorSearchSpace>
{
    public int Dimension { get; set; } = 10;
    public int Instance { get; set; } = 1;
    public int Problem { get; set; } = 1;

    public TestFunctionExperimentParameters() { }

    public TestFunctionExperimentParameters(TestFunctionExperimentParameters parameters) : base(parameters) { }
}
#endregion
