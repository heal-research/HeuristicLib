using HEAL.HeuristicLib.Algorithms;
using HEAL.HeuristicLib.Algorithms.Evolutionary;
using HEAL.HeuristicLib.Algorithms.MetaAlgorithms;
using HEAL.HeuristicLib.Genotypes.Trees;
using HEAL.HeuristicLib.Operators.Creators.SymbolicExpressionTreeCreators;
using HEAL.HeuristicLib.Operators.Crossovers.SymbolicExpressionTreeCrossovers;
using HEAL.HeuristicLib.Operators.Evaluators;
using HEAL.HeuristicLib.Operators.Mutators;
using HEAL.HeuristicLib.Operators.Mutators.SymbolicExpressionTreeMutators;
using HEAL.HeuristicLib.Operators.Selectors;
using HEAL.HeuristicLib.Optimization;
using HEAL.HeuristicLib.Problems;
using HEAL.HeuristicLib.Problems.DataAnalysis;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression;
using HEAL.HeuristicLib.Problems.DataAnalysis.Regression.Evaluators;
using HEAL.HeuristicLib.Random;
using HEAL.HeuristicLib.SearchSpaces.Trees;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Grammars;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols;
using HEAL.HeuristicLib.SearchSpaces.Trees.SymbolicExpressionTree.Symbols.Math;

namespace HEAL.HeuristicLib.PythonInterOptScripts;

public record EquationScoringEvaluator(Func<SymbolicExpressionTree[], ObjectiveVector[], double[][]> PythonCallback)
  : StatelessEvaluator<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, ExtendedSymbolicRegressionProblem> //This evaluator only works with your custom Problem
{
  public override IReadOnlyList<ObjectiveVector> Evaluate(IReadOnlyList<SymbolicExpressionTree> genotypes, IRandomNumberGenerator random, SymbolicExpressionTreeSearchSpace searchSpace, ExtendedSymbolicRegressionProblem problem)
  {
    //you could also call evaluate directly but DirectEvaluator does things in parallel for us, and I am lazy
    var normalObjectives = new DirectEvaluator<SymbolicExpressionTree>().Evaluate(genotypes, random, searchSpace, problem);
    var betterObjectives = PythonCallback(genotypes.ToArray(), normalObjectives.ToArray()); //passing Arrays is not strictly needed but might be better for interopt
    return betterObjectives.Select(x => (ObjectiveVector)x).ToArray(); //make sure that Length of new Objectives matches what the problem promised
  }
}

/// <summary>
/// This is a toy problem that uses a "normal" symbolic regression problem and adds more objectives provided by a generic function
/// </summary>
public class ExtendedSymbolicRegressionProblem(Objective objective, SymbolicExpressionTreeSearchSpace searchSpace, Func<SymbolicExpressionTree,ObjectiveVector, double[]> individualPythonCallback)
  : Problem<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace>(objective, searchSpace)
{
  public required SymbolicRegressionProblem InnerProblem { get; init; }

  public override ObjectiveVector Evaluate(SymbolicExpressionTree solution, IRandomNumberGenerator random)
  {
    return individualPythonCallback(solution, InnerProblem.Evaluate(solution) ).ToArray();
  }

  #region CallTheseFromPython

  public static Population<SymbolicExpressionTree> RunDefault(
    string file, int trainingRowCount, 
    Func<SymbolicExpressionTree,ObjectiveVector, double[]> individualPythonCallback,
    Func<SymbolicExpressionTree[], ObjectiveVector[], double[][]> populationwidePythonCallback, int seed = 42)
  {

    var data = RegressionCsvInstanceProvider.ImportData(file, trainingRowCount); //rows are not shuffled
    var grammar = new SimpleSymbolicExpressionGrammar(); //Trees have 3 node min
    var root = grammar.AddLinearScaling(); //adds Keijzer scaling to top of tree (+4 Nodes)
    var symbols = new Symbol[] { //add a bunch of symbols and allow all combinations of them 
      new Addition(),
      new Subtraction(),
      new Multiplication(),
      new Division(),
      new Number(),
      new SquareRoot(),
      new Logarithm(),
      new Variable { VariableNames = data.InputVariables }
    };
    grammar.AddFullyConnectedSymbols(root, symbols);

    var symbolicExpressionTreeSearchSpace = new SymbolicExpressionTreeSearchSpace(grammar, 40, 20);
    var p = new SymbolicRegressionProblem(data,
      symbolicExpressionTreeSearchSpace,
      new PearsonR2Evaluator()

      // !! if other evaluators are added, the length of the objective vectors returned by the python 
      // !! callback needs to be increased accordingly, and the problem's objective directions need to
      // !! be updated to reflect the new objectives (e.g. more maximization objectives if you add more
      // !! evaluators that you want to maximize)

      // new RootMeanSquaredErrorEvaluator(), 
      // new TreeLengthEvaluator() //... other evaluators
    ) {
      ParameterOptimizationIterations = 5 //this is the effort spent on Constant-Optimization
    };

    //tell the objective that your func is going to append 3 maximization objectives to whatever your inner problem does
    var directions = new ObjectiveDirection[]{

      // !! number of evalutors in SymbolicRegressionProblem, these objectives, and the length 
      // !! of the objective vectors returned by the python callbacks all need to be in sync

      ObjectiveDirection.Maximize, // newly added python objectives, as weigthed sum
      ObjectiveDirection.Maximize, // just r2, for final comparison
      ObjectiveDirection.Maximize, // Dimensional Consistency
      ObjectiveDirection.Maximize, // Limits & Trends
      ObjectiveDirection.Maximize  // Symmetry
    };
    var objective = new Objective(directions, new LexicographicComparer(directions));

    if(individualPythonCallback == null) {

      // !! number of evalutors in SymbolicRegressionProblem, objectives, and this length
      // !! of the objective vectors returned by the python callbacks all need to be in sync

      individualPythonCallback = (SymbolicExpressionTree tree, ObjectiveVector objectiveVector) => new double[] {objectiveVector[0], 0, 0, 0, 0 }; //dummy values to keep the objective vectors at expected length
    }

    var problem =  new ExtendedSymbolicRegressionProblem(objective, symbolicExpressionTreeSearchSpace, individualPythonCallback) { InnerProblem = p };

    var symRegAllMutator = MultiMutator.Create(
      new ChangeNodeTypeManipulation(),
      new FullTreeShaker(),
      new OnePointShaker(),
      new RemoveBranchManipulation(),
      new ReplaceBranchManipulation()
    );

    //we need to specify the types of the alg explicitly because much of the convenience is still missing
    //var ga = GeneticAlgorithm.GetBuilder(new ProbabilisticTreeCreator(), new SubtreeCrossover(), symRegAllMutator);
    var ga = new GeneticAlgorithmBuilder<SymbolicExpressionTree, SymbolicExpressionTreeSearchSpace, ExtendedSymbolicRegressionProblem> {
      Creator = new ProbabilisticTreeCreator(),
      Crossover = new SubtreeCrossover(), 
      Mutator = symRegAllMutator,
      MutationRate = 0.1,
      Selector = new TournamentSelector<SymbolicExpressionTree>(4),
      PopulationSize = 300,
      Evaluator = new EquationScoringEvaluator(populationwidePythonCallback)
    };

    var res = ga.Build()
                .WithMaxIterations(200)
                .RunToCompletion(problem, RandomNumberGenerator.Create(seed), null, CancellationToken.None);
    return res.Population;
  }
  #endregion
}
 